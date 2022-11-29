using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using ConsoleApp.GenericArgProcessing;
using DemoParser.Parser;
using DemoParser.Parser.Components.Packets;
using DemoParser.Parser.Components.Packets.StringTableEntryTypes;
using DemoParser.Parser.EntityStuff;
using DemoParser.Parser.GameState;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace ConsoleApp.DemoArgProcessing.Options.Hidden {

	public class OptPgunColorPatch : DemoOption<OptPgunColorPatch.ColorOption> {

		public enum ColorOption {
			None,
			Blue,
			Orange
		}

		public static readonly ImmutableArray<string> DefaultAliases = new[] {"--portalgun-color-patch"}.ToImmutableArray();

		public OptPgunColorPatch() : base(
			DefaultAliases,
			Arity.One,
			"Create a new demo that sets the portal gun color at the start of the demo",
			"color",
			Utils.ParseEnum<ColorOption>,
			ColorOption.None,
			true) {}

		protected override void AfterParse(DemoParsingSetupInfo setupObj, ColorOption arg, bool isDefault) {
			setupObj.ExecutableOptions++;
			setupObj.EditsDemos = true;
		}

		protected override void Process(DemoParsingInfo infoObj, ColorOption arg, bool isDefault) {
			infoObj.PrintOptionMessage("changing portal gun color");
			Stream s = infoObj.StartWritingBytes("pgun-color", ".dem");
			try {
				PatchPortalGunColor(infoObj.CurrentDemo, s, arg);
			} catch (Exception) {
				Utils.Warning("Portal gun color change failed.\n");
				infoObj.CancelOverwrite = true;
			}
		}

		public static void PatchPortalGunColor(SourceDemo demo, Stream s, ColorOption color) {

			IEnumerable<(int Offset, int BitLength)> firedColorsOffs =
				from packetTup in demo.FilterForPacket<StringTables>()
				from table in packetTup.Tables
				where table.Name == TableNames.InstanceBaseLine
				from entry in table.TableEntries
				let baseline = entry.EntryData as InstanceBaseline
				where baseline is {ServerClassRef: {ClassName: "CWeaponPortalgun"}}
				from propData in baseline.Properties
				let prop = propData.prop as SingleEntProp<int>
				where prop is {Name: "m_iLastFiredPortal"}
				select (prop.Offset, prop.BitLength);

			BitStreamWriter bsw = new BitStreamWriter(demo.Reader.Data);

			foreach ((int offset, int bitLength) in firedColorsOffs)
				bsw.EditIntAtIndex((int)color, offset, bitLength);

			s.Write(bsw, 0, bsw.ByteLength);
		}

		protected override void PostProcess(DemoParsingInfo infoObj, ColorOption arg, bool isDefault) {}
	}
}
