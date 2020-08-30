using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets.StringTableEntryTypes {
    
    public class GameRulesCreation : StringTableEntryData {
        
        internal override bool InlineToString => true;
        public string ClassName;


        public GameRulesCreation(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}

        
        internal override void ParseStream(BitStreamReader bsr) {
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