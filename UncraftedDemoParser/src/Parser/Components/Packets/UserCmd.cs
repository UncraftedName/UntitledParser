using System.Text;
using UncraftedDemoParser.Parser.Components.Abstract;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.Parser.Components.Packets {
	
	public class UserCmd : DemoPacket {

		public int Cmd;
		// int size -> size of the rest of the data in bytes, not stored for simplicity
		public int? CommandNumber;
		public int? TickCount;
		public float? ViewAngleX, ViewAngleY, ViewAngleZ;
		public float? SidewaysMovement, ForwardMovement, VerticalMovement;
		public int? Buttons;
		public byte? Impulse;
		public int? WeaponSelect, WeaponSubtype;
		public short? MouseDx, MouseDy;
		
		
		public UserCmd(byte[] data, SourceDemo demoRef, int tick) : base(data, demoRef, tick) {}


		protected override void ParseBytes() {
			BitFieldReader bfr = new BitFieldReader(Bytes);
			Cmd = bfr.ReadInt();
			bfr.ReadBytes(4); // skip over size of data
			CommandNumber = bfr.ReadIntIfExists();
			TickCount = bfr.ReadIntIfExists();
			ViewAngleX = bfr.ReadFloatIfExists();
			ViewAngleY = bfr.ReadFloatIfExists();
			ViewAngleZ = bfr.ReadFloatIfExists();
			SidewaysMovement = bfr.ReadFloatIfExists();
			ForwardMovement = bfr.ReadFloatIfExists();
			VerticalMovement = bfr.ReadFloatIfExists();
			Buttons = bfr.ReadIntIfExists();
			Impulse = bfr.ReadByteIfExists();
			if (bfr.ReadBool()) {
				WeaponSelect = bfr.ReadBitsAsInt(11);
				WeaponSubtype = bfr.ReadBitsAsIntIfExists(6);
			}
			MouseDx = bfr.ReadShortIfExists();
			MouseDy = bfr.ReadShortIfExists();
		}

		public override void UpdateBytes() {
			BitFieldWriter bfwOptionalData = new BitFieldWriter(20); // to be optimized
			bfwOptionalData.WriteIntIfExists(CommandNumber);
			bfwOptionalData.WriteIntIfExists(TickCount);
			bfwOptionalData.WriteFloatIfExists(ViewAngleX);
			bfwOptionalData.WriteFloatIfExists(ViewAngleY);
			bfwOptionalData.WriteFloatIfExists(ViewAngleZ);
			bfwOptionalData.WriteFloatIfExists(SidewaysMovement);
			bfwOptionalData.WriteFloatIfExists(ForwardMovement);
			bfwOptionalData.WriteFloatIfExists(VerticalMovement);
			bfwOptionalData.WriteIntIfExists(Buttons);
			bfwOptionalData.WriteIntIfExists(Impulse);
			bfwOptionalData.WriteBitsFromIntIfExists(WeaponSelect, 11);
			if (WeaponSelect.HasValue)
				bfwOptionalData.WriteBitsFromIntIfExists(WeaponSubtype, 6);
			bfwOptionalData.WriteShortIfExists(MouseDx);
			bfwOptionalData.WriteShortIfExists(MouseDy);
			
			BitFieldWriter bfwAllData = new BitFieldWriter(bfwOptionalData.SizeInBytes + 8);
			bfwAllData.WriteInt(Cmd);
			bfwAllData.WriteInt(bfwOptionalData.SizeInBytes + 1); // idk why it's + 1 tbh
			bfwAllData.WriteBytes(bfwOptionalData.Data);
			Bytes = bfwAllData.Data;
		}


		public override string ToString() { // kill me already
			StringBuilder output = new StringBuilder();
			output.AppendLine($"\tcmd: {Cmd}");
			output.AppendLine($"\tcommand number: {CommandNumber}");
			output.AppendLine($"\ttick count: {TickCount}");
			output.AppendFormat("\tview angles: {0,11} {1,11} {2,11}\n",
				$"{(ViewAngleX.HasValue ? $"{ViewAngleX.Value:F2}°" : "none ")},",
				$"{(ViewAngleY.HasValue ? $"{ViewAngleY.Value:F2}°" : "none ")},",
				$"{(ViewAngleZ.HasValue ? $"{ViewAngleZ.Value:F2}°" : "none ")}");
			output.AppendFormat("\tmovement:    {0,11} {1,11} {2,11}\n",
				$"{(SidewaysMovement.HasValue ? $"{SidewaysMovement.Value:F2}" : "none")} ,",
				$"{(ForwardMovement.HasValue  ? $"{ForwardMovement.Value:F2}" : "none")} ,",
				$"{(VerticalMovement.HasValue ? $"{VerticalMovement.Value:F2}" : "none")} ");
			output.AppendLine($"\tbuttons: {Buttons}");
			output.AppendLine($"\timpulse: {Impulse}");
			output.AppendFormat("\tweapon, subtype:  {0,4}, {1,4}\n", 
				WeaponSelect.HasValue  ? WeaponSelect.Value.ToString() : "none", 
				WeaponSubtype.HasValue ? WeaponSubtype.Value.ToString() : "none");
			output.AppendFormat("\tmouseDx, mouseDy: {0,4}, {1,4}",
				MouseDx.HasValue ? MouseDx.Value.ToString() : "none",
				MouseDy.HasValue ? MouseDy.Value.ToString() : "none");
			return output.ToString();
		}
	}
}