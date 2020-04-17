using System.Collections.Generic;
using DemoParser.Parser.Components.Messages;
using DemoParser.Utils;

namespace DemoParser.Parser.HelperClasses.EntityStuff {
    
    public abstract class EntityUpdate : Appendable {} // base class
    

    public class Delta : EntityUpdate {
		
        public readonly int EntIndex;
        public readonly ServerClass ServerClass;
        public readonly IReadOnlyList<(int propIndex, EntityProperty prop)> Props;
        
		
        public Delta(int entIndex, ServerClass serverClass, IReadOnlyList<(int propIndex, EntityProperty prop)> props) {
            EntIndex = entIndex;
            ServerClass = serverClass;
            Props = props;
        }
        
		
        public override void AppendToWriter(IndentedWriter iw) {
            iw.Append($"({EntIndex}) DELTA - class: ({ServerClass.ToString()})");
            iw.AddIndent();
            foreach ((int propIndex, EntityProperty prop) in Props) {
                iw.AppendLine();
                iw.Append($"({propIndex}) {prop.ToString()}");
            }
            iw.SubIndent();
        }
    }
	
    
    // After creating the ent if necessary and marking it as in the PVS, this IS a delta.
    public class EnterPvs : Delta {

        public readonly uint Serial;
        public readonly bool New;


        public EnterPvs(
            int entIndex, 
            ServerClass serverClass, 
            IReadOnlyList<(int propIndex, EntityProperty prop)> props, 
            uint serial, 
            bool @new) 
            : base(entIndex, serverClass, props) 
        {
            Serial = serial;
            New = @new;
        }
        

        public override void AppendToWriter(IndentedWriter iw) {
            iw.Append($"({EntIndex}) {(New ? "CREATE" : "ENTER_PVS")} {ServerClass.ToString()}");
            if (New)
                iw.Append($", serial: {Serial}");
            iw.AddIndent();
            foreach ((int propIndex, EntityProperty prop) in Props) {
                iw.AppendLine();
                iw.Append($"({propIndex}) {prop.ToString()}");
            }
            iw.SubIndent();
        }
    }
	
    
    public class LeavePvs : EntityUpdate {
		
        public readonly int Index;
        public readonly ServerClass ServerClass;
        public readonly bool Delete;
        
		
        public LeavePvs(int index, ServerClass serverClass, bool delete) {
            Index = index;
            ServerClass = serverClass;
            Delete = delete;
        }
        

        public override void AppendToWriter(IndentedWriter iw) {
            iw.Append($"({Index}) {(Delete ? "DELETE" : "LEAVE_PVS")} - class: ({ServerClass.ToString()})");
        }
    }
}
