using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Numerics;
using ConsoleApp.GenericArgProcessing;
using DemoParser.Parser;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.Components.Packets;
using DemoParser.Parser.HelperClasses.EntityStuff;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace ConsoleApp.DemoArgProcessing.Options.Hidden {
	
	public class OptSmoothGlessHops : DemoOption<int> {
		
		public static readonly ImmutableArray<string> DefaultAliases = new[] {"--smooth-jumps"}.ToImmutableArray();


		private static int ValidateInterpTicks(string s) {
			if (!int.TryParse(s, out int numTicks))
				throw new ArgProcessUserException($"\"{s}\" is not a valid integer");
			if (numTicks <= 0 || numTicks > 20)
				throw new ArgProcessUserException($"expected interp ticks to be between 1 and 20, got {numTicks}");
			return numTicks;
		}
		
		
		public OptSmoothGlessHops() : base(
			DefaultAliases,
			Arity.ZeroOrOne,
			"Creates a new demo with smoother jumps, main use is for rendering demos with lots of standing glitchless hops." +
			"\nmax_ground_ticks is at most how many ticks the player is on the ground before they jump again. High values might interp too much." +
			"\nOnly jumps in the middle of a hop sequence will be smoothed. Only designed to work for portal 1 unpack.",
			"max_ground_ticks",
			ValidateInterpTicks,
			5,
			true) {}


		protected override void AfterParse(DemoParsingSetupInfo setupObj, int arg, bool isDefault) {
			setupObj.ExecutableOptions++;
			setupObj.FolderOutputRequired = true;
		}


		protected override void Process(DemoParsingInfo infoObj, int arg, bool isDefault) {
			BinaryWriter bw = infoObj.InitBinaryWriter("smoothing jumps", "smooth-jumps", ".dem");
			SmoothJumps(infoObj.CurrentDemo, bw.BaseStream, arg);
		}

		/*
		 * Okay so this is long and wacky and honestly I don't remember exactly how this works because I threw it
		 * together for rendering glitchless v2 a while ago. Sooooooooo, the general idea is to smoothly transition the
		 * player's origin and view offset during a jump.
		 * 
		 * 
		 * Normally, a jump will look something like this (each column is a tick):
		 * 
		 * cam pos:     👁️👁️👁️👁️👁️                               👁️👁️👁️👁️👁️
		 *                        👁️👁️👁️👁️               👁️👁️👁️👁️
		 *                                👁️👁️👁️   👁️👁️👁️
		 *                                      👁️👁️
		 * 
		 * origin pos:  🦶🦶🦶🦶🦶                               🦶🦶🦶🦶🦶
		 *                        🦶🦶🦶🦶               🦶🦶🦶🦶
		 *                                🦶🦶🦶   🦶🦶🦶
		 * 
		 * 
		 * 
		 *                                      🦶🦶
		 * floor:       ------------------------------------------------------------
		 * 
		 * God bless you if you don't see emojis here. Notice that the player is in a crouched state until they get
		 * close to the floor. For the 2 (in this case) ticks that the player is on the floor, they are standing. To
		 * compensate for this, the view offset is increased and therein lies the problem. It seems like the player's
		 * view offset transitions smoothly regardless of demo_interpolateview and always happens a tick too late or
		 * something like that. This makes jumps extremely ugly without demo interp and slightly bouncy with interp.
		 * The goal of this function is to "bridge the gap" if you will, and pull the player's feet up during a jump.
		 * We find jumps by looking at "m_Local.m_flJumpTime", and then linearly interpolate the player's origin and
		 * view offset across the ticks when they are on the ground. There's some spicy stuff below, for instance I
		 * re-encode the player's view origin, but I can only write it if my encoding takes the same amount of bits as
		 * the original value. I'm sure there's plenty of memery that could cut out here, this is probably something
		 * that should be improved in the future.
		 */
		public static void SmoothJumps(SourceDemo demo, Stream s, int maxTicksBeforeJump) {
			
			if ((demo.DemoInfo.DemoParseResult & DemoParseResult.EntParsingEnabled) == 0) {
				Console.WriteLine("Entity parsing not enabled for this demo, can't apply smoothing.");
				return;
			}
			
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			var jumpTicks =
				(from msgTup in demo.FilterForMessage<SvcPacketEntities>() 
					from update in msgTup.message.Updates
					where update.ServerClass.ClassName == "CPortal_Player" && update is Delta
					from deltaProp in ((Delta)update).Props
					where deltaProp.prop.Name == "m_Local.m_flJumpTime" &&
						  ((SingleEntProp<float>)deltaProp.prop).Value == 510.0
					select msgTup.tick).ToList();
			
			
			Console.WriteLine($"found jump ticks: {jumpTicks.SequenceToString()}");
			BitStreamWriter bsw = new BitStreamWriter(demo.Reader.Data);

			const int maxTicksBeyondJump = 1;
			const float interpThreshold = 15; // this many units off the ground is considered a jump

			var frames = demo.Frames;
			foreach (int jumpTick in jumpTicks) {

				int jumpTickIdx = 0;
				for (; frames[jumpTickIdx].Type != PacketType.Packet; jumpTickIdx++) ; // find first packet
				while (frames[++jumpTickIdx].Tick < jumpTick) ; // find the first frame with the matching tick
				for (; frames[jumpTickIdx].Type != PacketType.Packet; jumpTickIdx++) ; // find the packet on this tick

				Packet groundPacket = (Packet)frames[jumpTickIdx].Packet!;
				Vector3 groundPos = groundPacket.PacketInfo[0].ViewOrigin; // roughly the ground pos

				int endTick = -1;
				Vector3 endVec = new Vector3(float.PositiveInfinity);
				bool shouldInterp = true;

				int idx = jumpTickIdx;
				for (int ticksBeyondJump = 0; ticksBeyondJump < maxTicksBeyondJump; ticksBeyondJump++) {

					while (frames[++idx].Type != PacketType.Packet); // jump to next packet

					Packet curPacket = (Packet)frames[idx].Packet!;
					Vector3 curViewOrigin = curPacket.PacketInfo[0].ViewOrigin;

					if (curViewOrigin.Z - groundPos.Z > interpThreshold) {
						endTick = curPacket.Tick;
						endVec = curViewOrigin;
						break;
					}

					if (ticksBeyondJump == maxTicksBeyondJump - 1) {
						shouldInterp = false;
						Console.WriteLine($"not patching jump on tick {jumpTick}, too many ticks before air time detected");
						break;
					}
				}
				if (!shouldInterp)
					continue;
				
				Vector3 startVec = new Vector3(float.PositiveInfinity);

				idx = jumpTickIdx;
				for (int ticksBeforeJump = 0; ticksBeforeJump < maxTicksBeforeJump; ticksBeforeJump++) {

					while (frames[--idx].Type != PacketType.Packet) ; // jump to previous packet

					Packet curPacket = (Packet)frames[idx].Packet!;
					Vector3 curViewOrigin = curPacket.PacketInfo[0].ViewOrigin;

					if (curViewOrigin.Z - groundPos.Z > interpThreshold) {
						startVec = curViewOrigin;
						break;
					}
					if (ticksBeforeJump == maxTicksBeforeJump - 1) {
						shouldInterp = false;
						Console.WriteLine($"not patching jump on tick {jumpTick}, too many ticks on ground (at least {maxTicksBeforeJump}) before this jump");
						break;
					}
				}
				if (!shouldInterp)
					continue;
				int startTick = frames[idx].Tick; // idx is currently the start tick (right before the first interp tick)

				while (frames[idx].Tick < endTick) {

					while (frames[++idx].Type != PacketType.Packet); // jump to next packet

					BitStreamWriter simpleVecBytes = new BitStreamWriter();
					Packet packet = (Packet)frames[idx].Packet!;
					float lerpAmount = (float)(frames[idx].Tick - startTick) / (endTick - startTick);
					Vector3 lerpVec = Vector3.Lerp(startVec, endVec, lerpAmount);
					simpleVecBytes.WriteVector3(lerpVec);
					// edit the pos in the cmdinfo
					bsw.EditBitsAtIndex(simpleVecBytes, packet.Reader.AbsoluteStart + 32);
					// edit the pos in the entity delta
					var originProp = (SingleEntProp<Vector3>?)(
							(Delta?)packet.FilterForMessage<SvcPacketEntities>().Single().Updates!
								.SingleOrDefault(update => update.ServerClass.ClassName == "CPortal_Player"))
						?.Props.SingleOrDefault(tuple => tuple.prop.Name == "m_vecOrigin").prop;
					if (originProp != null) {
						bool tryWrite = true;
						while (tryWrite) {
							tryWrite = false;
							BitStreamWriter newOriginBytes = new BitStreamWriter();
							newOriginBytes.WriteBitCoord(originProp.Value.X);
							newOriginBytes.WriteBitCoord(originProp.Value.Y);
							newOriginBytes.WriteBitCoord(lerpVec.Z);
							// only edit the pos if the new value is encoded with the same number of bits
							switch (newOriginBytes.BitLength - originProp.BitLength) {
								case 0:
									bsw.EditBitsAtIndex(newOriginBytes, originProp.Offset, originProp.BitLength);
									break;
								case 5: // too many bits, but we can shave some off by rounding the z val
									lerpVec.Z = (float)Math.Round(lerpVec.Z);
									tryWrite = true;
									break;
								case -5: // too few bits, but we can increase the encoding size by giving z a fractional part
									lerpVec.Z += 0.04f;
									tryWrite = true;
									break;
								default:
									Console.WriteLine(
										$"not patching jump on tick {jumpTick}, " +
										$"bit length doesn't match: new value uses {newOriginBytes.BitLength} bits but old value has {originProp.BitLength} bits");
									break;
							}
						}
					}

					var viewOffsetProp = (SingleEntProp<float>?)(
							(Delta?)packet.FilterForMessage<SvcPacketEntities>().Single().Updates!
								.SingleOrDefault(update => update.ServerClass.ClassName == "CPortal_Player"))
						?.Props.SingleOrDefault(tuple => tuple.prop.Name == "m_vecViewOffset[2]").prop;

					if (viewOffsetProp != null) {
						BitStreamWriter viewOffsetBytes = new BitStreamWriter();
						viewOffsetBytes.WriteFloat(28);
						bsw.EditBitsAtIndex(viewOffsetBytes, viewOffsetProp.Offset);
					}
				}
			}
			s.Write(bsw, 0, bsw.ByteLength);
		}


		protected override void PostProcess(DemoParsingInfo infoObj, int arg, bool isDefault) {}
	}
}
