using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components {
	
	/// <summary>
	/// Serves as a wrapper for all packets.
	/// </summary>
	public class PacketFrame : DemoComponent {
		
		public byte? PlayerSlot; // demo protocol 4 only
		public DemoPacket? Packet;
		public int Tick;
		public PacketType Type;
		
		
		public PacketFrame(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			
			byte typeVal = bsr.ReadByte();
			Type = DemoPacket.ByteToPacketType(DemoInfo, typeVal);
			
			if (Type == PacketType.Unknown)
				throw new ArgumentException("no byte->packet mapper found for this game!");
			else if (Type == PacketType.Invalid)
				throw new ArgumentException($"Illegal packet type: {typeVal}");
			
			// stop tick is cut off in portal demos, not that it really matters
			Tick = Type == PacketType.Stop && !DemoInfo.NewDemoProtocol
				? (int)bsr.ReadBitsAsUInt(24) | (DemoRef.Frames[^2].Tick & (0xff << 24))
				: bsr.ReadSInt();

			// last player slot byte is cut off in l4d demos
			if ((DemoInfo.NewDemoProtocol || DemoInfo.IsLeft4Dead1()) && bsr.BitsRemaining > 0)
				PlayerSlot = bsr.ReadByte();
			
			Packet = PacketFactory.CreatePacket(DemoRef!, this, Type);
			Packet.ParseStream(ref bsr);
			bsr.EnsureByteAlignment(); // make sure the next frame starts on a byte boundary
		}
		
		
		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}
		
		
		public override void PrettyWrite(IPrettyWriter pw) {
			if (Packet != null) {
				pw.Append($"[{Tick}] {Type.ToString().ToUpper()} ({DemoPacket.PacketTypeToByte(DemoInfo, Type)})");
				if (DemoInfo.NewDemoProtocol && PlayerSlot.HasValue)
					pw.Append($"\nplayer slot: {PlayerSlot.Value}");
				if (Packet.MayContainData) {
					pw.FutureIndent++;
					pw.AppendLine();
					Packet.PrettyWrite(pw);
					pw.FutureIndent--;
				}
			} else {
				pw.Append("demo parsing failed here, packet type doesn't correspond with any known packet");
			}
		}
	}
}