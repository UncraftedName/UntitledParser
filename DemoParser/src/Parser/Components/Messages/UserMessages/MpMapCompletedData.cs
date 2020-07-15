using System;
using System.Collections.Generic;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;
using static DemoParser.Parser.DemoSettings;

namespace DemoParser.Parser.Components.Messages.UserMessages {
	
	// portal 2 specific
	public class MpMapCompletedData : SvcUserMessage {
		
		public List<MapCompletedInfo> Info;
		
		
		public MpMapCompletedData(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			Info = new List<MapCompletedInfo>();
			Span<byte> bytes = stackalloc byte[2 * MaxPortal2CoopBranches * MaxPortal2CoopLevelsPerBranch / 8];
			bytes.Clear();
			bsr.ReadBytesToSpan(bytes);
			int current = 0;
			int mask = 0x01;
			for (int player = 0; player < 2; player++) {
				for (int branch = 0; branch < MaxPortal2CoopBranches; branch++) {
					for (int level = 0; level < MaxPortal2CoopLevelsPerBranch; level++) {
						if ((bytes[current] & mask) != 0)
							Info.Add(new MapCompletedInfo {Player = player, Branch = branch, Level = level});
						mask <<= 1;
						if (mask >= 0x0100) {
							current++;
							mask = 0x01;
						}
					}
				}
			}
		}
		
		
		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}
		
		
		public override void AppendToWriter(IndentedWriter iw) {
			if (Info.Count == 0) {
				iw.Append("none");
			} else {
				iw.Append("maps set as completed: (1-indexed)");
				iw.FutureIndent++;
				for (var i = 0; i < Info.Count; i++) {
					iw.AppendLine();
					iw.Append($"player: {Info[i].Player+1}, branch: {Info[i].Branch+1}, level: {Info[i].Level+1}");
				}
				iw.FutureIndent--;
			}
		}
		
		
		public struct MapCompletedInfo {
			public int Player;
			public int Branch;
			public int Level;
		}
	}
}