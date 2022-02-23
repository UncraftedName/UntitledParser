using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets {

	/// <summary>
	/// Contains direct user input info such as mouse delta and buttons pressed.
	/// </summary>
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

		public UserCmd(SourceDemo? demoRef, PacketFrame frameRef) : base(demoRef, frameRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Cmd = bsr.ReadUInt();
			int byteSize = bsr.ReadSInt();
			BitStreamReader uBsr = bsr.SplitAndSkip(byteSize * 8);
			CommandNumber = uBsr.ReadUIntIfExists();
			TickCount = uBsr.ReadUIntIfExists();
			ViewAngleX = uBsr.ReadFloatIfExists();
			ViewAngleY = uBsr.ReadFloatIfExists();
			ViewAngleZ = uBsr.ReadFloatIfExists();
			SidewaysMovement = uBsr.ReadFloatIfExists();
			ForwardMovement = uBsr.ReadFloatIfExists();
			VerticalMovement = uBsr.ReadFloatIfExists();
			Buttons = (Buttons?)uBsr.ReadUIntIfExists();
			Impulse = uBsr.ReadByteIfExists();
			if (uBsr.ReadBool()) {
				WeaponSelect = uBsr.ReadUInt(11);
				WeaponSubtype = uBsr.ReadUIntIfExists(6);
			}
			MouseDx = (short?)uBsr.ReadUShortIfExists();
			MouseDy = (short?)uBsr.ReadUShortIfExists();
			if (uBsr.HasOverflowed)
				DemoRef.LogError($"{GetType().Name}: reader overflowed");
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"cmd: {Cmd}\n");
			pw.Append($"command number: {CommandNumber}\n");
			pw.Append($"tick count: {TickCount}\n");
			pw.AppendFormat("view angles: {0,11} {1,11} {2,11}\n",
				$"{(ViewAngleX.HasValue ? $"{ViewAngleX.Value:F2}°" : "null ")},",
				$"{(ViewAngleY.HasValue ? $"{ViewAngleY.Value:F2}°" : "null ")},",
				$"{(ViewAngleZ.HasValue ? $"{ViewAngleZ.Value:F2}°" : "null ")}");
			pw.AppendFormat("movement:    {0,11} {1,11} {2,11}\n",
				$"{(SidewaysMovement.HasValue ? $"{SidewaysMovement.Value:F2}" : "null")} ,",
				$"{(ForwardMovement.HasValue  ? $"{ForwardMovement.Value:F2}" : "null")} ,",
				$"{(VerticalMovement.HasValue ? $"{VerticalMovement.Value:F2}" : "null")} ");
			pw.Append($"buttons: {Buttons?.ToString() ?? "null"}\n");
			pw.Append($"impulse: {Impulse?.ToString() ?? "null"}\n");
			pw.AppendFormat("weapon, subtype:  {0,4}, {1,4}\n",
				WeaponSelect?.ToString() ?? "null", WeaponSubtype?.ToString() ?? "null");
			pw.AppendFormat("mouseDx, mouseDy: {0,4}, {1,4}",
				MouseDx?.ToString() ?? "null", MouseDy?.ToString() ?? "null");
		}
	}


	// https://github.com/alliedmodders/hl2sdk/blob/portal2/game/shared/in_buttons.h
	// se2007/game/shared/in_buttons.h
	[Flags]
	public enum Buttons {
		None            = 0,
		Attack          = 1,
		Jump            = 1 << 1,
		Duck            = 1 << 2,
		Forward         = 1 << 3,
		Back            = 1 << 4,
		Use             = 1 << 5,
		Cancel          = 1 << 6,
		Left            = 1 << 7,
		Right           = 1 << 8,
		MoveLeft        = 1 << 9,
		MoveRight       = 1 << 10,
		Attack2         = 1 << 11,
		Run             = 1 << 12,
		Reload          = 1 << 13,
		Alt1            = 1 << 14,
		Alt2            = 1 << 15,
		Score           = 1 << 16,
		Speed           = 1 << 17,
		Walk            = 1 << 18,
		Zoom            = 1 << 19,
		Weapon1         = 1 << 20,
		Weapon2         = 1 << 21,
		BullRush        = 1 << 22,
		Grenade1        = 1 << 23,
		Grenade2        = 1 << 24,
		LookSpin        = 1 << 25,
		CurrentAbility  = 1 << 26,
		PreviousAbility = 1 << 27,
		Ability1        = 1 << 28,
		Ability2        = 1 << 29,
		Ability3        = 1 << 30,
		Ability4        = 1 << 31
	}
}
