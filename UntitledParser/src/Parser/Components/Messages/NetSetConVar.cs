using System.Collections.Generic;
using UntitledParser.Parser.Components.Abstract;
using UntitledParser.Utils;

namespace UntitledParser.Parser.Components.Messages {
	
	public class NetSetConVar : DemoMessage {

		public List<(string, string)> ConVars;


		public NetSetConVar(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}


		internal override void ParseStream(BitStreamReader bsr) {
			//ConVars = new List<(CharArray, CharArray)>();
			byte count = bsr.ReadByte();
			ConVars = new List<(string, string)>(count);
			for (int i = 0; i < count; i++)
				ConVars.Add((bsr.ReadNullTerminatedString(), bsr.ReadNullTerminatedString()));
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			for (int i = 0; i < ConVars.Count; i++) {
				iw.Append($"{{name: {ConVars[i].Item1}, value: {ConVars[i].Item2}}}");
				if (i != ConVars.Count - 1)
					iw.AppendLine();
			}
		}
	}
}