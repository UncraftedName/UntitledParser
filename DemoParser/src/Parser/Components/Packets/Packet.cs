using System;
using System.Linq;
using System.Numerics;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Messages;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets {

	/// <summary>
	/// Contains client player location and server-side messages.
	/// </summary>
	public class Packet : DemoPacket {

		public CmdInfo[] PacketInfo;
		public uint InSequence;
		public uint OutSequence;
		public MessageStream MessageStream {get;set;}


		public Packet(SourceDemo? demoRef, PacketFrame frameRef) : base(demoRef, frameRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			PacketInfo = new CmdInfo[DemoInfo.MaxSplitscreenPlayers];
			for (int i = 0; i < PacketInfo.Length; i++) {
				PacketInfo[i] = new CmdInfo(DemoRef);
				PacketInfo[i].ParseStream(ref bsr);
			}
			InSequence = bsr.ReadUInt();
			OutSequence = bsr.ReadUInt();
			MessageStream = new MessageStream(DemoRef);
			MessageStream.ParseStream(ref bsr);

			// After we're doing with the packet, we can process all the messages.
			// Most things should be processed during parsing, but any additional checks should be done here.

			NetTick? tickInfo = MessageStream.OfType<NetTick>().FirstOrDefault();
			if (tickInfo != null) {
				if (GameState.EntitySnapshot != null)
					GameState.EntitySnapshot.EngineTick = tickInfo.EngineTick;
			}
			// todo fill prop handles with data here
			TimingAdjustment.AdjustFromPacket(this);
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			foreach (CmdInfo cmdInfo in PacketInfo)
				cmdInfo.PrettyWrite(pw);
			pw.AppendLine($"in sequence: {InSequence}");
			pw.AppendLine($"out sequence: {OutSequence}");
			MessageStream.PrettyWrite(pw);
		}
	}


	public class CmdInfo : DemoComponent {

		public InterpFlags Flags;
		private Vector3[] _floats;
		public ref Vector3 ViewOrigin       => ref _floats[0];
		public ref Vector3 ViewAngles       => ref _floats[1];
		public ref Vector3 LocalViewAngles  => ref _floats[2];
		public ref Vector3 ViewOrigin2      => ref _floats[3];
		public ref Vector3 ViewAngles2      => ref _floats[4];
		public ref Vector3 LocalViewAngles2 => ref _floats[5];
		private static readonly string[] Names = {
			"view origin", "view angles", "local view angles", "view origin 2", "view angles 2", "local view angles 2"};
		private static readonly bool[] UseDegreeSymbol = {false, true, true, false, true, true};


		public CmdInfo(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Flags = (InterpFlags)bsr.ReadUInt();
			_floats = new Vector3[6];
			for (int i = 0; i < _floats.Length; i++)
				bsr.ReadVector3(out _floats[i]);
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"flags: {Flags}");
			for (int i = 0; i < _floats.Length; i++) {
				string dSym = UseDegreeSymbol[i] ? "°" : " ";
				pw.AppendFormat("{0,-20} {1,11}, {2,11}, {3,11}\n",
					$"{Names[i]}:", $"{_floats[i].X:F2}{dSym}", $"{_floats[i].Y:F2}{dSym}", $"{_floats[i].Z:F2}{dSym}");
			}
		}
	}


	[Flags]
	public enum InterpFlags : uint {
		None        = 0,
		UseOrigin2  = 1,
		UserAngles2 = 1 << 1,
		NoInterp    = 1 << 2  // don't interpolate between this and last view
	}
}
