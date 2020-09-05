using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets.StringTableEntryTypes {
    
    public class GameRulesCreation : StringTableEntryData {
        
        internal override bool InlineToString => true;
        public string ClassName;


        public GameRulesCreation(SourceDemo? demoRef) : base(demoRef) {}


        internal override StringTableEntryData CreateCopy() {
            return new GameRulesCreation(DemoRef) {ClassName = ClassName};
        }


        protected override void Parse(ref BitStreamReader bsr) {
            ClassName = bsr.ReadNullTerminatedString();
        }


        internal override void WriteToStreamWriter(BitStreamWriter bsw) {
            throw new NotImplementedException();
        }


        public override void AppendToWriter(IndentedWriter iw) {
            iw.Append(ClassName);
        }
    }
}