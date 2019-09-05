using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.obsolete {
	
	public class UserCmdOld {

		public readonly byte[] _data;
		public readonly int? 	_commandNumber;
		public readonly int? 	_tickCount;
		public readonly float? _viewAngleX, _viewAngleY, _viewAngleZ;
		public readonly float? _sideMove, _forwardMove, _upMove;
		public readonly int? 	_buttons;
		public readonly byte? 	_impulse;
		public readonly int? 	_weaponSelect, _weaponSubtype;
		public readonly short? _mouseDx, _mouseDy;
		private static readonly Dictionary<int, int> NameMapper = new Dictionary<int, int>(); 
		
		
		public UserCmdOld(byte[] data, int tick) {
			_data = data;
			BitFieldReader bitReader = new BitFieldReader(data);
			try {
				if (bitReader.ReadBool())
					_commandNumber = bitReader.ReadInt();
				if (bitReader.ReadBool())
					_tickCount = bitReader.ReadInt();
				if (bitReader.ReadBool())
					_viewAngleX = bitReader.ReadFloat();
				if (bitReader.ReadBool())
					_viewAngleY = bitReader.ReadFloat();
				if (bitReader.ReadBool())
					_viewAngleZ = bitReader.ReadFloat();
				if (bitReader.ReadBool())
					_sideMove = bitReader.ReadFloat();
				if (bitReader.ReadBool())
					_forwardMove = bitReader.ReadFloat();
				if (bitReader.ReadBool())
					_upMove = bitReader.ReadFloat();
				if (bitReader.ReadBool())
					_buttons = bitReader.ReadInt();
				if (bitReader.ReadBool())
					_impulse = bitReader.ReadByte();
				if (bitReader.ReadBool()) {
					byte[] bytes = new byte[4];
					Array.Copy(bitReader.ReadBits(11), bytes, 0);
					bytes[2] = bytes[3] = 0;
					if (BitConverter.IsLittleEndian)
						Array.Reverse(bytes);
					_weaponSelect = BitConverter.ToInt32(bytes, 0);
					if (bitReader.ReadBool())
						_weaponSubtype = bitReader.ReadBits(6)[0];
				}
				if (bitReader.ReadBool())
					_mouseDx = bitReader.ReadShort();
				if (bitReader.ReadBool())
					_mouseDy = bitReader.ReadShort();
			} catch (IndexOutOfRangeException) { // temporary - write any packets where the parser fails to tmp
				Console.WriteLine($"UserCmd packet not formatted properly on tick {tick}");
				if (NameMapper.ContainsKey(tick))
					NameMapper[tick]++;
				else
					NameMapper[tick] = 1;
				File.WriteAllBytes($@"..\out\tmp\cmdpacket {tick}-{NameMapper[tick]}", data);
			}
		}

		public override string ToString() {
			StringBuilder output = new StringBuilder();
			output.AppendLine($"\tcommand number: {_commandNumber}");
			output.AppendLine($"\ttick count: {_tickCount}");
			output.AppendLine($"\tview angle x: {_viewAngleX}   view angle y: {_viewAngleY}   view angle z: {_viewAngleZ}");
			output.AppendLine($"\tside move: {_sideMove}   forward move: {_forwardMove}   up move: {_upMove}");
			output.AppendLine($"\tbuttons: {_buttons}");
			output.AppendLine($"\timpulse: {_impulse}");
			output.AppendLine($"\tweapon select: {_weaponSelect}");
			output.AppendLine($"\tweapon subtype: {_weaponSubtype}");
			output.AppendLine($"\tmouse dx: {_mouseDx}   mouse dy: {_mouseDy}");
			return output.ToString();
		}
	}
}