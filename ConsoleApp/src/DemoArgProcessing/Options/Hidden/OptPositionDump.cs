using System;
using System.Collections.Immutable;
using System.Numerics;
using DemoParser.Parser.Components.Packets;
using DemoParser.Utils;

namespace ConsoleApp.DemoArgProcessing.Options.Hidden {

	public class OptPositionDump : DemoOption {

		public static readonly ImmutableArray<string> DefaultAliases = new[] {"--position-dump"}.ToImmutableArray();

		public OptPositionDump() : base(
			DefaultAliases,
			"Dump player's 1 position and angles on every tick in the following format:" +
			"\n|tick|~|x,y,z|pitch,yaw,roll|" +
			"\nIntended use is for the demo overlay render.",
			true) {}


		public override void AfterParse(DemoParsingSetupInfo setupObj) {
			setupObj.ExecutableOptions++;
		}


		public override void Process(DemoParsingInfo infoObj) {
			infoObj.PrintOptionMessage("dumping player position");
			try {
				int prevTick = int.MinValue;
				foreach (Packet packet in infoObj.CurrentDemo.FilterForPacket<Packet>()) {
					if (packet.Tick == prevTick || packet.Tick < 0)
						continue;
					prevTick = packet.Tick;
					CmdInfo cmdInfo = packet.PacketInfo[0];
					ref Vector3 pos = ref cmdInfo.ViewOrigin;
					ref Vector3 ang = ref cmdInfo.ViewAngles;
					Console.WriteLine($"|{packet.Tick}|~|{pos.X},{pos.Y},{pos.Z}|{ang.X},{ang.Y},{ang.Z}|");
				}
			} catch (Exception) {
				Utils.Warning("Position dump failed.\n");
			}
		}


		public override void PostProcess(DemoParsingInfo infoObj) {}
	}
}
