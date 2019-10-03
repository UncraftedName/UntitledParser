using System;
using System.Linq;
using System.Text;
using UncraftedDemoParser.Parser.Components.Abstract;
using UncraftedDemoParser.Parser.Misc;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.Parser.Components.Packets {
	
	public class Packet : DemoPacket {

		
		public CmdInfo[] PacketInfo;
		public int InSequence;
		public int OutSequence;
		// int size				// not stored for simplicity
		private byte _messageType;

		public SvcMessageType MessageType {
			get => _messageType.ToSvcMessageType(DemoRef.DemoSettings.NewEngine);
			set => _messageType = value.ToByte(DemoRef.DemoSettings.NewEngine);
		}
		public byte[] SvcMessageBytes; // to be removed
		public SvcNetMessage SvcNetMessage;
		

		public Packet(byte[] data, SourceDemo demoRef, int tick) : base(data, demoRef, tick) {}


		protected override void ParseBytes() {
			BitFieldReader bfr = new BitFieldReader(Bytes);
			PacketInfo = new CmdInfo[DemoRef.DemoSettings.MaxSplitscreenPlayers];
			for (int i = 0; i < PacketInfo.Length; i++) {
				PacketInfo[i] = (CmdInfo)new CmdInfo(bfr.ReadBytes(76), DemoRef, Tick).TryParse(Tick);
			}
			InSequence = bfr.ReadInt();
			OutSequence = bfr.ReadInt();
			int svcMessageSize = bfr.ReadInt();
			_messageType = bfr.ReadBits(6)[0]; // i'm not sure if the data starts on the byte boundary
			SvcMessageBytes = bfr.ReadBytes(svcMessageSize - 1);
			// might throw exceptions
			SvcNetMessage = MessageType.ToSvcNetMessage(SvcMessageBytes, DemoRef, Tick);
			if (SvcNetMessage == null)
				Console.WriteLine($"warning: {MessageType} is not parsable yet");
			else
				SvcNetMessage.TryParse(Tick);
		}

		public override void UpdateBytes() {
			BitFieldWriter bfw = new BitFieldWriter(PacketInfo.Length * 76 + SvcMessageBytes.Length + 13);
			foreach (CmdInfo cmdInfo in PacketInfo) {
				cmdInfo.UpdateBytes();
				bfw.WriteBytes(cmdInfo.Bytes);
			}
			bfw.WriteInt(InSequence);
			bfw.WriteInt(OutSequence);
			bfw.WriteInt(SvcMessageBytes.Length);
			bfw.WriteBitsFromInt(_messageType, 6);
			bfw.WriteBytes(SvcMessageBytes);
		}


		public override string ToString() {
			StringBuilder output = new StringBuilder();
			PacketInfo.ToList().ForEach(info => output.AppendLine(info.ToString()));
			output.AppendLine($"\tin sequence: {InSequence}");
			output.AppendLine($"\tout sequence: {OutSequence}");
			output.AppendLine($"\tmessage type: {MessageType}");
			output.AppendLine($"\tmessage of length {SvcMessageBytes.Length}: {SvcMessageBytes.AsHexStr()}");
			output.Append(SvcNetMessage);
			return output.ToString();
		}
	}


	public sealed class CmdInfo : DemoPacket {

		public int Flags;
		public float[] ViewOrigin = new float[3];
		public float[] ViewAngles = new float[3];
		public float[] LocalViewAngles = new float[3];
		public float[] ViewOrigin2 = new float[3];
		public float[] ViewAngles2 = new float[3];
		public float[] LocalViewAngles2 = new float[3];

		private float[][] _floats; // just a reference to the above floats for easy iteration


		public CmdInfo(byte[] data, SourceDemo demoRef, int tick) : base(data, demoRef, tick) {}


		protected override void ParseBytes() {
			BitFieldReader bfr = new BitFieldReader(Bytes);
			Flags = bfr.ReadInt();
			_floats = new[] {ViewOrigin, ViewAngles, LocalViewAngles, ViewOrigin2, ViewAngles2, LocalViewAngles2};
			foreach (float[] f in _floats)
				for (int i = 0; i < 3; i++)
					f[i] = bfr.ReadFloat();
		}

		public override void UpdateBytes() {
			BitFieldWriter bfw = new BitFieldWriter(76);
			bfw.WriteInt(Flags);
			foreach (float[] f in _floats)
				for (int i = 0; i < 3; i++)
					bfw.WriteFloat(f[i]);
		}


		public override string ToString() {
			StringBuilder output = new StringBuilder(200);
			BitFieldWriter bfwTmp = new BitFieldWriter(4);
			bfwTmp.WriteInt(Flags);
			output.AppendLine($"\tflags: {bfwTmp.Data.AsBinStr()}");
			// locations are written as xyz, angles as written as pitch, yaw, roll
			output.AppendFormat("\tview origin:         {0,11} {1,11} {2,11}\n", $"{ViewOrigin[0]:F2} ,",       $"{ViewOrigin[1]:F2} ,",       $"{ViewOrigin[2]:F2} ");
			output.AppendFormat("\tview angles:         {0,11} {1,11} {2,11}\n", $"{ViewAngles[0]:F2}°,",       $"{ViewAngles[1]:F2}°,",       $"{ViewAngles[2]:F2}°");
			output.AppendFormat("\tlocal view angles:   {0,11} {1,11} {2,11}\n", $"{LocalViewAngles[0]:F2}°,",  $"{LocalViewAngles[1]:F2}°,",  $"{LocalViewAngles[2]:F2}°");
			output.AppendFormat("\tview origin 2:       {0,11} {1,11} {2,11}\n", $"{ViewOrigin2[0]:F2} ,",      $"{ViewOrigin2[1]:F2} ,",      $"{ViewOrigin2[2]:F2} ");
			output.AppendFormat("\tview angles 2:       {0,11} {1,11} {2,11}\n", $"{ViewAngles2[0]:F2}°,",      $"{ViewAngles2[1]:F2}°,",      $"{ViewAngles2[2]:F2}°");
			output.AppendFormat("\tlocal view angles 2: {0,11} {1,11} {2,11}",   $"{LocalViewAngles2[0]:F2}°,", $"{LocalViewAngles2[1]:F2}°,", $"{LocalViewAngles2[2]:F2}°");
			return output.ToString();
		}
	}
}