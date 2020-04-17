using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets.StringTableEntryTypes {
    
    public class GameRulesCreation : StringTableEntryData {

        public string ClassName;
        
        
        public GameRulesCreation(SourceDemo demoRef, BitStreamReader reader, string entryName) : base(demoRef, reader, entryName) {}

        
        internal override void ParseStream(BitStreamReader bsr) {
            ClassName = bsr.ReadNullTerminatedString();
        }


        public override void AppendToWriter(IndentedWriter iw) {
            iw.Append(ClassName);
        }
    }
}