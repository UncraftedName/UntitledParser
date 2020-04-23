using System;
using System.Diagnostics;
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
		public MessageStream MessageStream;
		
		
		public Packet(SourceDemo demoRef, BitStreamReader reader, int tick) : base(demoRef, reader, tick) {}


		internal override void ParseStream(BitStreamReader bsr) {
			PacketInfo = new CmdInfo[DemoRef.DemoSettings.MaxSplitscreenPlayers];
			for (int i = 0; i < PacketInfo.Length; i++) {
				PacketInfo[i] = new CmdInfo(DemoRef, bsr);
				PacketInfo[i].ParseStream(bsr);
			}
			InSequence = bsr.ReadUInt();
			OutSequence = bsr.ReadUInt();
			MessageStream = new MessageStream(DemoRef, bsr);
			MessageStream.ParseStream(bsr);
			SetLocalStreamEnd(bsr);
			
			// After we're doing with the packet, we can process all the messages.
			// Most things should be processed during parsing, but any additional checks should be done here.

			var netTickMessages = MessageStream.Where(tuple => tuple.messageType == MessageType.NetTick).ToList();
			Debug.Assert(netTickMessages.Count < 2, "there's more than 2 net tick messages in this packet");
			NetTick tickInfo = (NetTick)netTickMessages.FirstOrDefault().message;
			if (tickInfo != null) {
				if (DemoRef.CEntitySnapshot != null)
					DemoRef.CEntitySnapshot.EngineTick = tickInfo.EngineTick;
			}
			// todo fill prop handles with data here
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			foreach (CmdInfo cmdInfo in PacketInfo) 
				cmdInfo.AppendToWriter(iw);
			iw.AppendLine($"in sequence: {InSequence}");
			iw.AppendLine($"out sequence: {OutSequence}");
			MessageStream.AppendToWriter(iw);
		}
	}
	
	
	public class CmdInfo : DemoComponent {

		public InterpFlags Flags;
		private Vector3[] _floats;
		public Vector3 ViewOrigin => _floats[0];
		public Vector3 ViewAngles => _floats[1];
		public Vector3 LocalViewAngles => _floats[2];
		public Vector3 ViewOrigin2 => _floats[3];
		public Vector3 ViewAngles2 => _floats[4];
		public Vector3 LocalViewAngles2 => _floats[5];
		private static readonly string[] Names = {
			"view origin", "view angles", "local view angles", "view origin 2", "view angles 2", "local view angles 2"};
		private static readonly bool[] UseDegreeSymbol = {false, true, true, false, true, true};


		public CmdInfo(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}


		internal override void ParseStream(BitStreamReader bsr) {
			Flags = (InterpFlags)bsr.ReadUInt();
			_floats = new Vector3[6];
			for (int i = 0; i < _floats.Length; i++)
				_floats[i] = bsr.ReadVector3();
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"flags: {Flags}");
			for (int i = 0; i < _floats.Length; i++) {
				string dSym = UseDegreeSymbol[i] ? "°" : " ";
				iw.AppendFormat("{0,-20} {1,11}, {2,11}, {3,11}\n",
					$"{Names[i]}:", $"{_floats[i].X:F2}{dSym}", $"{_floats[i].Y:F2}{dSym}", $"{_floats[i].Z:F2}{dSym}");
			}
		}
	}


	[Flags]
	public enum InterpFlags: uint {
		None        = 0,
		UseOrigin2  = 1,
		UserAngles2 = 1 << 1,
		NoInterp    = 1 << 2  // don't interpolate between this and last view
	}
}