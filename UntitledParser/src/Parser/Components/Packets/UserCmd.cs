using UntitledParser.Parser.Components.Abstract;
using UntitledParser.Utils;

namespace UntitledParser.Parser.Components.Packets {
	
	public class UserCmd : DemoPacket {
		
		public uint Cmd;
		public uint? CommandNumber;
		public uint? TickCount;
		public float? ViewAngleX, ViewAngleY, ViewAngleZ;
		public float? SidewaysMovement, ForwardMovement, VerticalMovement;
		public uint? Buttons;
		public byte? Impulse;
		public uint? WeaponSelect, WeaponSubtype;
		public short? MouseDx, MouseDy;
		
		public UserCmd(SourceDemo demoRef, BitStreamReader reader, int tick) : base(demoRef, reader, tick) {}


		internal override void ParseStream(BitStreamReader bsr) {
			Cmd = bsr.ReadUInt();
			uint byteSize = bsr.ReadUInt();
			int indexBeforeData = bsr.CurrentBitIndex;
			CommandNumber = bsr.ReadUIntIfExists();
			TickCount = bsr.ReadUIntIfExists();
			ViewAngleX = bsr.ReadFloatIfExists();
			ViewAngleY = bsr.ReadFloatIfExists();
			ViewAngleZ = bsr.ReadFloatIfExists();
			SidewaysMovement = bsr.ReadFloatIfExists();
			ForwardMovement = bsr.ReadFloatIfExists();
			VerticalMovement = bsr.ReadFloatIfExists();
			Buttons = bsr.ReadUIntIfExists();
			Impulse = bsr.ReadByteIfExists();
			if (bsr.ReadBool()) {
				WeaponSelect = bsr.ReadBitsAsUInt(11);
				WeaponSubtype = bsr.ReadBitsAsUIntIfExists(6);
			}
			MouseDx = (short?)bsr.ReadUShortIfExists();
			MouseDy = (short?)bsr.ReadUShortIfExists();
			bsr.CurrentBitIndex = indexBeforeData + (int)(byteSize << 3);
			SetLocalStreamEnd(bsr);
		}

		
		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw += $"cmd: {Cmd}\n";
			iw += $"command number: {CommandNumber}\n";
			iw += $"tick count: {TickCount}\n";
			iw.AppendFormat("view angles: {0,11} {1,11} {2,11}\n",
				$"{(ViewAngleX.HasValue ? $"{ViewAngleX.Value:F2}°" : "none ")},",
				$"{(ViewAngleY.HasValue ? $"{ViewAngleY.Value:F2}°" : "none ")},",
				$"{(ViewAngleZ.HasValue ? $"{ViewAngleZ.Value:F2}°" : "none ")}");
			iw.AppendFormat("movement:    {0,11} {1,11} {2,11}\n",
				$"{(SidewaysMovement.HasValue ? $"{SidewaysMovement.Value:F2}" : "none")} ,",
				$"{(ForwardMovement.HasValue  ? $"{ForwardMovement.Value:F2}" : "none")} ,",
				$"{(VerticalMovement.HasValue ? $"{VerticalMovement.Value:F2}" : "none")} ");
			iw += $"buttons: {Buttons}\n";
			iw += $"impulse: {Impulse}\n";
			iw.AppendFormat("weapon, subtype:  {0,4}, {1,4}\n", 
				WeaponSelect.HasValue  ? WeaponSelect.Value.ToString() : "none", 
				WeaponSubtype.HasValue ? WeaponSubtype.Value.ToString() : "none");
			iw.AppendFormat("mouseDx, mouseDy: {0,4}, {1,4}",
				MouseDx.HasValue ? MouseDx.Value.ToString() : "none",
				MouseDy.HasValue ? MouseDy.Value.ToString() : "none");
		}
	}
}