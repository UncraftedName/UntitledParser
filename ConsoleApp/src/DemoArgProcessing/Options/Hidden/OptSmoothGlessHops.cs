using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Numerics;
using ConsoleApp.GenericArgProcessing;
using DemoParser.Parser;
using DemoParser.Parser.Components;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.Components.Packets;
using DemoParser.Parser.Components.Packets.StringTableEntryTypes;
using DemoParser.Parser.EntityStuff;
using DemoParser.Parser.GameState;
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
			"Create a new demo with smoother jumps, main use is for rendering demos with lots of standing glitchless hops." +
			"\nmax_ground_ticks is at most how many ticks the player is on the ground before they jump again. High values might interp too much." +
			"\nOnly jumps in the middle of a hop sequence will be smoothed. Only designed to work for portal 1 unpack.",
			"max_ground_ticks",
			ValidateInterpTicks,
			5,
			true) {}


		protected override void AfterParse(DemoParsingSetupInfo setupObj, int arg, bool isDefault) {
			setupObj.ExecutableOptions++;
			setupObj.EditsDemos = true;
		}


		protected override void Process(DemoParsingInfo infoObj, int arg, bool isDefault) {
			infoObj.PrintOptionMessage("smoothing jumps");
			Stream s = infoObj.StartWritingBytes("smooth-jumps", ".dem");
			try {
				SmoothJumps(infoObj.CurrentDemo, s, arg);
			} catch (Exception e) {
				Utils.Warning($"Smoothing jumps failed: {e.Message}\n");
				infoObj.CancelOverwrite = true;
			}
		}

		/*
		 * Unfortunately even with demo interp on, for unknown reasons transitions in the player's view offset look
		 * bad during demo playback. The solution is to get rid of any places where it changes. Jumps are a common
		 * place for that, and they normally look something like this in portal 1 (each column is a tick):
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
		 * God bless you if you don't see emojis here. ThE fix is to set the player's view offset to 0 in the entire
		 * demo. For each tick we get the origin and view offset, then set the origin to origin + view offset and the
		 * view offset to 0.
		 */
		public static void SmoothJumps(SourceDemo demo, Stream s, int maxGroundTicks) {
			BitStreamWriter bsw = new BitStreamWriter(demo.Reader.Data);

			// get baseline view offset
			SingleEntProp<float>? prevViewOffsetProp =
				demo.FilterForPacket<StringTables>().Single().Tables
					.Where(table => table.Name == TableNames.InstanceBaseLine)
					.SelectMany(table => table.TableEntries!)
					.Select(entry => entry.EntryData)
					.OfType<InstanceBaseline>()
					.Where(baseline => baseline.ServerClassRef!.ClassName == "CPortal_Player")
					.SelectMany(baseline => baseline.Properties!)
					.Select(tuple => tuple.prop)
					.OfType<SingleEntProp<float>>()
					.SingleOrDefault(prop => prop.Name == "m_vecViewOffset[2]");

			// set baseline view offset to 0
			if (prevViewOffsetProp != null) {
				BitStreamWriter viewOffsetBytes = new BitStreamWriter();
				viewOffsetBytes.WriteFloat(0);
				bsw.EditBitsAtIndex(viewOffsetBytes, prevViewOffsetProp.Offset);
			}

			foreach (Packet packet in demo.FilterForPacket<Packet>()) {
				// get packet view offset
				SingleEntProp<float>? curViewOffsetProp = packet
					.FilterForMessage<SvcPacketEntities>()
					.SelectMany(svcMsg => svcMsg.Updates!)
					.OfType<Delta>()
					.Where(delta => delta.ServerClass!.ClassName == "CPortal_Player")
					.SelectMany(delta => delta.Props)
					.Select(tuple => tuple.prop)
					.OfType<SingleEntProp<float>>()
					.FirstOrDefault(prop => prop.Name == "m_vecViewOffset[2]");

				// set packet view offset to 0
				if (curViewOffsetProp != null) {
					prevViewOffsetProp = curViewOffsetProp;
					BitStreamWriter viewOffsetBytes = new BitStreamWriter();
					viewOffsetBytes.WriteFloat(0);
					bsw.EditBitsAtIndex(viewOffsetBytes, curViewOffsetProp.Offset);
				}
				// set origin
				if (prevViewOffsetProp != null) {
					BitStreamWriter vecBytes = new BitStreamWriter();
					Vector3 oldOrigin = packet.PacketInfo[0].ViewOrigin;
					vecBytes.WriteVector3(oldOrigin with {Z = oldOrigin.Z + prevViewOffsetProp.Value});
					// edit the pos in the cmdinfo - the origin in the ent data isn't used during demo playback
					bsw.EditBitsAtIndex(vecBytes, packet.Reader.AbsoluteStart + 32);
				}
			}

			s.Write(bsw, 0, bsw.ByteLength);
		}


		protected override void PostProcess(DemoParsingInfo infoObj, int arg, bool isDefault) {}
	}
}
