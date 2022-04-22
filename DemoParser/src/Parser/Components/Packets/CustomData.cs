#nullable enable
using System.Collections.Generic;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.GameState;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets {

	/// <summary>
	/// Contains a single custom game message, only exists in Portal 2?
	/// </summary>
	public class CustomData : DemoPacket {

		public int TypeVal;

		public List<string>? NewEntries;

		public string? TypeName;
		public CustomDataMessage? Data;


		public CustomData(SourceDemo? demoRef, PacketFrame frameRef) : base(demoRef, frameRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			TypeVal = bsr.ReadSInt();
			int byteLen = bsr.ReadSInt() * 8;
			if (TypeVal == -1) {
				int numEntries = bsr.ReadSInt();
				NewEntries = new List<string>();
				for (int i = 0; i < numEntries; i++)
					NewEntries.Add(bsr.ReadNullTerminatedString());
				DemoRef.State.CustomDataManager ??= new CustomDataManager(DemoRef, NewEntries);
			} else {
				TypeName = DemoRef.State.CustomDataManager.GetDataType(TypeVal);
				Data = DemoRef.State.CustomDataManager.CreateCustomDataMessage(TypeName);
				Data.ParseStream(bsr.ForkAndSkip(byteLen));
			}
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			if (NewEntries != null) {
				pw.Append($"{NewEntries.Count} new {(NewEntries.Count == 1 ? "entry" : "entries")}:");
				pw.FutureIndent++;
				foreach (string entry in NewEntries) {
					pw.AppendLine();
					pw.Append(entry);
				}
				pw.FutureIndent--;
			} else {
				if (Data == null) {
					pw.Append($"unknown type ({TypeVal})");
				} else {
					pw.Append($"[{TypeVal}] {TypeName}");
					pw.FutureIndent++;
					pw.AppendLine();
					Data.PrettyWrite(pw);
					pw.FutureIndent--;
				}
			}
		}
	}
}
