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
				throw new ArgProcessUserException("not a valid integer");
			if (numTicks <= 0 || numTicks > 20)
				throw new ArgProcessUserException($"expected interp ticks to be between 1 and 20, got {numTicks}");
			return numTicks;
		}
		
		
		public OptSmoothGlessHops() : base(
			DefaultAliases,
			Arity.ZeroOrOne,
			"Creates a new demo with smoother jumps, main use is for rendering demos with lots of standing glitchless hops." +
			"\nmax_ground_ticks is at most how many ticks the player is on the ground before they jump again. High values might interp too much." +
			"\nOnly jumps in the middle of a hop sequence will be smoothed. Only designed to work for portal 1 unpack. " + OptOutputFolder.RequiresString,
			"max_ground_ticks",
			ValidateInterpTicks,
			5,
			true) {}


		protected override void AfterParse(DemoParsingSetupInfo setupObj, int arg, bool isDefault) {
			setupObj.ExecutableOptions++;
			setupObj.EditsDemos = true;
		}


		protected override void Process(DemoParsingInfo infoObj, int arg, bool isDefault) {
			Stream s = infoObj.StartWritingBytes("smoothing jumps", "smooth-jumps", ".dem");
			try {
				SmoothJumps(infoObj.CurrentDemo, s, arg);
			} catch (Exception) {
				Utils.Warning("Smoothing jumps failed.\n");
				infoObj.CancelOverwrite = true;
			}
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
		 * view offset across the ticks when they are on the ground. There's some spicy stuff below, this is probably
		 * something that should be improved in the future.
		 */
		public static void SmoothJumps(SourceDemo demo, Stream s, int maxGroundTicks) {
			
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

			const float interpThreshold = 15; // this many units off the ground is considered a jump

			var frames = demo.Frames;
			foreach (int jumpTick in jumpTicks) {

				int jumpTickIdx = 0;
				for (; frames[jumpTickIdx].Type != PacketType.Packet; jumpTickIdx++) ; // find first packet
				while (frames[++jumpTickIdx].Tick < jumpTick) ; // find the first frame with the matching tick
				for (; frames[jumpTickIdx].Type != PacketType.Packet; jumpTickIdx++) ; // find the packet on this tick

				Packet groundPacket = (Packet)frames[jumpTickIdx].Packet!;
				Vector3 groundPos = groundPacket.PacketInfo[0].ViewOrigin; // roughly the ground pos

				int endTick;
				Vector3 endVec;

				int idx = jumpTickIdx;
				while (frames[++idx].Type != PacketType.Packet) ; // jump to next packet

				Vector3 curViewOrigin = ((Packet)frames[idx].Packet!).PacketInfo[0].ViewOrigin;

				if (curViewOrigin.Z - groundPos.Z > interpThreshold) {
					endTick = ((Packet)frames[idx].Packet!).Tick;
					endVec = curViewOrigin;
				} else {
					Console.WriteLine($"not patching jump on tick {jumpTick}, too many ticks before air time detected");
					continue;
				}
				
				Vector3 startVec = new Vector3(float.PositiveInfinity);
				idx = jumpTickIdx;
				bool shouldInterp = true;
				
				for (int ticksBeforeJump = 0; ticksBeforeJump < maxGroundTicks; ticksBeforeJump++) {

					while (frames[--idx].Type != PacketType.Packet) ; // jump to previous packet

					Packet curPacket = (Packet)frames[idx].Packet!;
					curViewOrigin = curPacket.PacketInfo[0].ViewOrigin;

					if (curViewOrigin.Z - groundPos.Z > interpThreshold) {
						startVec = curViewOrigin;
						break;
					}
					if (ticksBeforeJump == maxGroundTicks - 1) {
						shouldInterp = false;
						Console.WriteLine($"not patching jump on tick {jumpTick}, too many ticks on ground (at least {maxGroundTicks}) before this jump");
						break;
					}
				}
				if (!shouldInterp)
					continue;
				
				int startTick = frames[idx].Tick; // idx is currently the start tick (right before the first interp tick)

				while (frames[idx].Tick < endTick) {

					while (frames[++idx].Type != PacketType.Packet); // jump to next packet

					BitStreamWriter vecBytes = new BitStreamWriter();
					Packet packet = (Packet)frames[idx].Packet!;
					float lerpAmount = (float)(frames[idx].Tick - startTick) / (endTick - startTick);
					Vector3 lerpVec = Vector3.Lerp(startVec, endVec, lerpAmount);
					vecBytes.WriteVector3(lerpVec);
					// edit the pos in the cmdinfo, we don't actually need to edit the origin in the ent data since that isn't used during demo playback
					bsw.EditBitsAtIndex(vecBytes, packet.Reader.AbsoluteStart + 32);

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
