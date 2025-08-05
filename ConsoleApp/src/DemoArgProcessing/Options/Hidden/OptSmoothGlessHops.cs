using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Numerics;
using DemoParser.Parser;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.Components.Packets;
using DemoParser.Parser.Components.Packets.StringTableEntryTypes;
using DemoParser.Parser.EntityStuff;
using DemoParser.Parser.GameState;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace ConsoleApp.DemoArgProcessing.Options.Hidden {

	public class OptSmoothGlessHops : DemoOption {

		public static readonly ImmutableArray<string> DefaultAliases = new[] {"--smooth-jumps"}.ToImmutableArray();


		public OptSmoothGlessHops() : base(
			DefaultAliases,
			"Create a new demo with smoother jumps, main use is for rendering demos with lots of standing glitchless hops. Only designed to work for portal 1 unpack.",
			true) {}


		public override void AfterParse(DemoParsingSetupInfo setupObj) {
			setupObj.ExecutableOptions++;
			setupObj.EditsDemos = true;
		}


		public override void Process(DemoParsingInfo infoObj) {
			infoObj.PrintOptionMessage("smoothing jumps");
			Stream s = infoObj.StartWritingBytes("smooth-jumps", ".dem");
			try {
				SmoothJumps(infoObj.CurrentDemo, s);
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
		public static void SmoothJumps(SourceDemo demo, Stream s) {
			BitStreamWriter bsw = new BitStreamWriter(demo.Reader.Data);

			IEnumerable<InstanceBaseline> baselines;
			StringTables? st = demo.FilterForPacket<StringTables>().SingleOrDefault();

			if (st != null)
			{
				// look through StringTables
				baselines = st.Tables
					.Where(table => table.Name == TableNames.InstanceBaseLine)
					.SelectMany(table => table.TableEntries!)
					.Select(entry => entry.EntryData)
					.OfType<InstanceBaseline>();
			} else {
				// look through SvcCreateStringTable
				baselines = demo.FilterForMessage<SvcCreateStringTable>()
					.Select(msg => msg.message?.TableUpdates)
					.Where(updates => updates != null)
					.SelectMany(updates => updates!.TableUpdates)
					.Select(update => update?.Entry?.EntryData)
					.OfType<InstanceBaseline>();
			}

			// get baseline view offset
			SingleEntProp<float>? prevViewOffsetProp = baselines
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

				// set string table view offset
				var stringTableUpdates = packet
					.FilterForMessage<SvcUpdateStringTable>()
					.Where(svcMsg => svcMsg!.TableName == TableNames.InstanceBaseLine)
					.SelectMany(svcMsg => svcMsg.TableUpdates.TableUpdates)
					.Select(update => update?.Entry?.EntryData)
					.OfType<InstanceBaseline>()
					.Where(baseline => baseline.ServerClassRef!.ClassName == "CPortal_Player")
					.SelectMany(baseline => baseline.Properties!)
					.Select(tuple => tuple.prop)
					.OfType<SingleEntProp<float>>()
					.Where(prop => prop.Name == "m_vecViewOffset[2]");

				foreach (var update in stringTableUpdates) {
					BitStreamWriter viewOffsetBytes = new BitStreamWriter();
					viewOffsetBytes.WriteFloat(0);
					bsw.EditBitsAtIndex(viewOffsetBytes, update.Offset);
				}

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


		public override void PostProcess(DemoParsingInfo infoObj) {}
	}
}
