using System;
using UncraftedDemoParser.Parser.Components.Abstract;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.Parser.Components.Packets {
	
	public class CustomData : DemoPacket {

		public int Unknown;
		// int size    <- not stored for simplicity
		public byte[] Data;
		
		
		public CustomData(byte[] data, SourceDemo demoRef, int tick) : base(data, demoRef, tick) {}


		protected override void ParseBytes() {
			BitFieldReader bfr = new BitFieldReader(Bytes);
			Unknown = bfr.ReadInt();
			Data = bfr.ReadBytes(Bytes.Length - 4);
		}

		public override void UpdateBytes() {
			BitFieldWriter bfw = new BitFieldWriter(Data.Length + 4);
			bfw.WriteInt(Unknown);
			bfw.WriteInt(Data.Length);
			bfw.WriteBytes(Data);
			Bytes = bfw.Data;
		}


		public override string ToString() {
			return $"Unknown: {Unknown}\nData: {BitConverter.ToString(Data).Replace("-", " ").ToLower()}";
		}
	}
}