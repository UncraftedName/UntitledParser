using System;
using System.Text;
using UncraftedDemoParser.DemoStructure.Packets;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.DemoStructure {
	
	public sealed class Header : DemoComponent {
		
		private byte[] _fileStamp;
		public string FileStamp {
			get => ByteArrayAsString(_fileStamp);
			set => _fileStamp = StringAsByteArray(value, 8);
		}
		
		public int DemoProtocol;
		public int NetworkProtocol;
		
		private byte[] _serverName;
		public string ServerName {
			get => ByteArrayAsString(_serverName);
			set => _serverName = StringAsByteArray(value, 260);
		}
		
		private byte[] _clientName;
		public string ClientName {
			get => ByteArrayAsString(_clientName);
			set => _clientName = StringAsByteArray(value, 260);
		}
		
		private byte[] _mapName;
		public string MapName {
			get => ByteArrayAsString(_mapName);
			set => _mapName = StringAsByteArray(value, 260);
		}
		
		private byte[] _gameDirectory;
		public string GameDirectory {
			get => ByteArrayAsString(_gameDirectory);
			set => _gameDirectory = StringAsByteArray(value, 260);
		}
		
		public float PlaybackTime;
		public int TickCount;
		public int FrameCount;
		public int SignOnLength;


		public Header(byte[] data, SourceDemo demoRef): base(data, demoRef) {
			ParseBytes();
		}


		public override void ParseBytes() {
			BitFieldReader bfr = new BitFieldReader(Bytes);
			_fileStamp = bfr.ReadBytes(8);
			DemoProtocol = bfr.ReadInt();
			NetworkProtocol = bfr.ReadInt();
			_serverName = bfr.ReadBytes(260);
			_clientName = bfr.ReadBytes(260);
			_mapName = bfr.ReadBytes(260);
			_gameDirectory = bfr.ReadBytes(260);
			PlaybackTime = bfr.ReadFloat();
			TickCount = bfr.ReadInt();
			FrameCount = bfr.ReadInt();
			SignOnLength = bfr.ReadInt();
		}
		

		public override void UpdateBytes() {
			BitFieldWriter bfw = new BitFieldWriter(838);
			bfw.WriteBytes(_fileStamp);
			bfw.WriteInt(DemoProtocol);
			bfw.WriteInt(NetworkProtocol);
			bfw.WriteBytes(_serverName);
			bfw.WriteBytes(_clientName);
			bfw.WriteBytes(_mapName);
			bfw.WriteBytes(_gameDirectory);
			bfw.WriteFloat(PlaybackTime);
			bfw.WriteInt(TickCount);
			bfw.WriteInt(FrameCount);
			bfw.WriteInt(SignOnLength);
		}


		public override string ToString() {
			StringBuilder output = new StringBuilder();
			output.AppendLine($"File stamp: {FileStamp}");
			output.AppendLine($"Demo protocol: {DemoProtocol}");
			output.AppendLine($"Network protocol: {NetworkProtocol}");
			output.AppendLine($"Server name: {ServerName}");
			output.AppendLine($"Client name: {ClientName}");
			output.AppendLine($"Map name: {MapName}");
			output.AppendLine($"Game directory: {GameDirectory}");
			output.AppendLine($"Playback time: {PlaybackTime}");
			output.AppendLine($"Tick count: {TickCount}");
			output.AppendLine($"Frame count: {FrameCount}");
			output.AppendLine($"Sign on length: {SignOnLength}");
			return output.ToString();
		}

		private string ByteArrayAsString(byte[] strBytes) {
			unsafe {
				fixed (byte* bytePtr = strBytes) { 
					return new String((sbyte*)bytePtr);
				}
			}
		}


		private byte[] StringAsByteArray(string str, int size) {
			byte[] result = new byte[size];
			Array.Copy(Encoding.ASCII.GetBytes(str), result, str.Length);
			return result;
		}
	}
}