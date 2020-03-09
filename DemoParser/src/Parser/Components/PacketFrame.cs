using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;

namespace DemoParser.Parser.Components {
	
	public class PacketFrame : DemoComponent {
		
		public byte? PlayerSlot; // new engine only
		public DemoPacket Packet;
		// in the demo the tick is stored as part of the packet frame, 
		// but for convenience I store it as part of the packet
		public int Tick {
			get => Packet.Tick;
			set => Packet.Tick = value;
		}
		public PacketType Type;
		
		
		public PacketFrame(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}


		internal override void ParseStream(BitStreamReader bsr) {
			bsr.EnsureByteAlignment();
			Type = DemoPacket.ByteToPacketType(DemoRef, bsr.ReadByte());
			
			int tick = Type == PacketType.Stop && !DemoRef.DemoSettings.NewEngine
				? (int)bsr.ReadBitsAsUInt(24) | (DemoRef.Frames[^2].Tick & (0xff << 24)) // stop tick is cut off in portal demos, not that it really matters
				: bsr.ReadSInt();
			
			if (DemoRef.DemoSettings.HasPlayerSlot && bsr.BitsRemaining > 0)
				PlayerSlot = bsr.ReadByte();
			
			
			Packet = PacketFactory.CreatePacket(DemoRef, bsr, tick, Type);
			Packet.ParseStream(bsr);
			bsr.EnsureByteAlignment();
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			if (Packet != null) {
				iw.Append($"[{Tick}] {Type.ToString().ToUpper()} ({DemoPacket.PacketTypeToByte(Type, DemoRef.DemoSettings)})");
				if (DemoRef.DemoSettings.NewEngine && PlayerSlot.HasValue)
					iw.Append($"\nplayer slot: {PlayerSlot.Value}");
				if (Packet.MayContainData) {
					iw.AddIndent();
					iw.AppendLine();
					Packet.AppendToWriter(iw);
					iw.SubIndent();
				}
			} else {
				iw += "demo parsing failed here, packet type doesn't correspond with any known packet";
			}
		}
	}
}