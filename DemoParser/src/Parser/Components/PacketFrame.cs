using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components {
	
	/// <summary>
	/// Serves as a wrapper for all packets.
	/// </summary>
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
			Type = DemoPacket.ByteToPacketType(DemoSettings, bsr.ReadByte());
			
			// stop tick is cut off in portal demos, not that it really matters
			int tick = Type == PacketType.Stop && !DemoSettings.NewEngine
				? (int)bsr.ReadBitsAsUInt(24) | (DemoRef.Frames[^2].Tick & (0xff << 24))
				: bsr.ReadSInt();
			
			if (DemoSettings.HasPlayerSlot && bsr.BitsRemaining > 0) // last player slot byte is cut off in l4d2 demos
				PlayerSlot = bsr.ReadByte();
			
			Packet = PacketFactory.CreatePacket(DemoRef, bsr, tick, Type);
			Packet.ParseStream(bsr);
			bsr.EnsureByteAlignment(); // make sure the next frame starts on a byte boundary
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			if (Packet != null) {
				iw.Append($"[{Tick}] {Type.ToString().ToUpper()} ({DemoPacket.PacketTypeToByte(DemoSettings, Type)})");
				if (DemoSettings.NewEngine && PlayerSlot.HasValue)
					iw.Append($"\nplayer slot: {PlayerSlot.Value}");
				if (Packet.MayContainData) {
					iw.AddIndent();
					iw.AppendLine();
					Packet.AppendToWriter(iw);
					iw.SubIndent();
				}
			} else {
				iw.Append("demo parsing failed here, packet type doesn't correspond with any known packet");
			}
		}
	}
}