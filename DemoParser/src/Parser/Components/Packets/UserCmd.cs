using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;

namespace DemoParser.Parser.Components.Packets {
	
	public class UserCmd : DemoPacket {
		
		public uint Cmd;
		public uint? CommandNumber;
		public uint? TickCount;
		public float? ViewAngleX, ViewAngleY, ViewAngleZ;
		public float? SidewaysMovement, ForwardMovement, VerticalMovement;
		public Buttons? Buttons;
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
			Buttons = (Buttons?)bsr.ReadUIntIfExists();
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
			throw new NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw.Append($"cmd: {Cmd}\n");
			iw.Append($"command number: {CommandNumber}\n");
			iw.Append($"tick count: {TickCount}\n");
			iw.AppendFormat("view angles: {0,11} {1,11} {2,11}\n",
				$"{(ViewAngleX.HasValue ? $"{ViewAngleX.Value:F2}°" : "null ")},",
				$"{(ViewAngleY.HasValue ? $"{ViewAngleY.Value:F2}°" : "null ")},",
				$"{(ViewAngleZ.HasValue ? $"{ViewAngleZ.Value:F2}°" : "null ")}");
			iw.AppendFormat("movement:    {0,11} {1,11} {2,11}\n",
				$"{(SidewaysMovement.HasValue ? $"{SidewaysMovement.Value:F2}" : "null")} ,",
				$"{(ForwardMovement.HasValue  ? $"{ForwardMovement.Value:F2}" : "null")} ,",
				$"{(VerticalMovement.HasValue ? $"{VerticalMovement.Value:F2}" : "null")} ");
			iw.Append($"buttons: {Buttons?.ToString() ?? "null"}\n");
			iw.Append($"impulse: {Impulse?.ToString() ?? "null"}\n");
			iw.AppendFormat("weapon, subtype:  {0,4}, {1,4}\n", 
				WeaponSelect?.ToString() ?? "null", WeaponSubtype?.ToString() ?? "null");
			iw.AppendFormat("mouseDx, mouseDy: {0,4}, {1,4}",
				MouseDx?.ToString() ?? "null", MouseDy?.ToString() ?? "null");
		}
	}


	[Flags]
	public enum Buttons : uint { // se2007/game/shared/in_buttons.h
		None		= 0,
		Attack 		= 1,
		Jump 		= 1 << 1,
		Duck		= 1 << 2,
		Forward		= 1 << 3,
		Back		= 1 << 4,
		Use			= 1 << 5,
		Cancel		= 1 << 6,
		Left		= 1 << 7,
		Right		= 1 << 8,
		MoveLeft	= 1 << 9,
		MoveRight	= 1 << 10,
		Attack2		= 1 << 11,
		Run			= 1 << 12,
		Reload		= 1 << 13,
		Alt1		= 1 << 14,
		Alt2		= 1 << 15,
		Score		= 1 << 16,
		Speed		= 1 << 17,
		Walk		= 1 << 18,
		Zoom		= 1 << 19,
		Weapon1		= 1 << 20,
		Weapon2		= 1 << 21,
		BullRush	= 1 << 22,
		Grenade1	= 1 << 23,
		Grenade2	= 1 << 24
	}
}