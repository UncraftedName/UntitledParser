using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using static UncraftedDemoParser.obsolete.OriginalParser;

namespace UncraftedDemoParser.obsolete {
	
	// Keeps track of "special ticks" which can be anything, for example abnormal cases
	// such as view angles not matching local view angles, or the NET/SVC message type having an enum value
	// that doesn't match the list in Nekzor's parser: https://nekzor.github.io/dem#types-1.
	// To use this, add your own TickType along with a description, create a tracker instance before parsing,
	// then while parsing call tracker.Add() with your TickType and the current tick. 
	// After parsing, call tracker.ToString() to see all 'special' ticks that you've added.
	public class SpecialTickTracker {
		
		public enum TickType {
			[Description("View angles don't match local view angles")]
			ViewMismatchWithLocalView,
			[Description("View/Pos 2 isn't zero")]
			View2NonZero,
			[Description("Unknown svc message type")]
			UnknownSvcMessageType,
			[Description("Sequence doesn't match the command number in UserCMD packet")]
			CommandMismatchWithSequence,
			[Description("Difference between tick in UserCMD packet and the current tick is unusual")]
			BadCmdTickCount,
			[Description("The movement amount in the UserCMD packet is unusual")]
			BadMovementInUserCmd,
			[Description("These ticks don't contain a UserCMD packet")]
			MissingUserCmdPacket,
			[Description("There are two of the same packet type in a single tick")]
			PacketRepeatOnSingleTick
		}

		// a very simplified packet, containing only the type and tick that it's on
		public struct SimplePacket {
			
			public readonly PacketType Type;
			public readonly int Tick;
			
			public SimplePacket(PacketType type, int tick) {
				Type = type;
				Tick = tick;
			}

			public bool Equals(SimplePacket other) {
				return Type == other.Type && Tick == other.Tick;
			}

			public override bool Equals(object obj) {
				return obj is SimplePacket other && Equals(other);
			}

			public override int GetHashCode() {
				unchecked {
					return ((int) Type * 397) ^ Tick;
				}
			}
		}
		
		
		private readonly SortedSet<int>[] _specialTickList;
		public int CmdTickDifference = Int32.MinValue; // for tracking BadCmdTickCount
		public readonly Dictionary<SimplePacket, int> PacketCounter = new Dictionary<SimplePacket, int>();


		public SpecialTickTracker() {
			_specialTickList = new SortedSet<int>[Enum.GetNames(typeof(TickType)).Length];
			for (int i = 0; i < _specialTickList.Length; i++)
				_specialTickList[i] = new SortedSet<int>();
		}
		
		
		public void Add(TickType type, int tick) {
			_specialTickList[(int)type].Add(tick);
		}

		private static string GetDescription(TickType type) {
			return type
				.GetType()
				.GetMember(type.ToString())
				.FirstOrDefault()
				?.GetCustomAttribute<DescriptionAttribute>()
				?.Description;
		}

		// returns ranges of each special tick type. e.g. the special ticks 1,2,3,4,7,9,10,11 would correspond to "1...4, 7, 9...11"
		public override string ToString() {
			string output = "";
			
			// checks that no packet type except ConsoleCMD repeats in a tick
			// and checks that the UserCMD packet is present on every tick
			HashSet<int> userCmdTicks = new HashSet<int>();
			foreach (KeyValuePair<SimplePacket,int> pair in PacketCounter) {
				if (pair.Key.Type != PacketType.CONSOLECMD && pair.Value > 1)
					Add(TickType.PacketRepeatOnSingleTick, pair.Key.Tick);
				if (pair.Key.Type == PacketType.USERCMD)
					userCmdTicks.Add(pair.Key.Tick);
			}
			HashSet<int> expectedTicks = new HashSet<int>(Enumerable.Range(1, PacketCounter.Values.Max()));
			foreach (int i in expectedTicks.Except(userCmdTicks)) 
				Add(TickType.MissingUserCmdPacket, i);


			TickType[] tickTypes = (TickType[]) Enum.GetValues(typeof(TickType));
			
			for (int i = 0; i < tickTypes.Length; i++) {
				if (_specialTickList[i].Count == 0)
					continue;
				output += GetDescription(tickTypes[i]) + ": [";
				
				int streakLength = 1;
				int prevTick = _specialTickList[i].First();
				output += prevTick;
				foreach (int tick in _specialTickList[i]) {
					if (tick == _specialTickList[i].First())
						continue;
					if (tick - prevTick == 1) {
						streakLength++;
						if (streakLength == 3)
							output += "...";
					} else {
						switch (streakLength) {
							case 1:
								output += $", {tick}";
								break;
							case 2:
								output += $", {prevTick}, {tick}";
								break;
							default:
								output += $"{prevTick}, {tick}";
								break;
						}
						streakLength = 1;
					}
					prevTick = tick;
				}
				if (streakLength >= 2)
					output += $"{prevTick}";
				output += "]\n\n";
			}
			return output;
		}
	}
}