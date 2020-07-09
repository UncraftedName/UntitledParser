using System.Collections.Generic;
using DemoParser.Parser.Components.Messages;
using DemoParser.Utils;

namespace DemoParser.Parser.HelperClasses.EntityStuff {

	// base class
	public abstract class EntityUpdate : Appendable {
		
		public readonly ServerClass ServerClass;
		
		protected EntityUpdate(ServerClass serverClass) {
			ServerClass = serverClass;
		}
	}
	

	public class Delta : EntityUpdate {
		
		public readonly int EntIndex;
		public readonly IReadOnlyList<(int propIndex, EntityProperty prop)> Props;
		
		
		public Delta(int entIndex, ServerClass serverClass, IReadOnlyList<(int propIndex, EntityProperty prop)> props) 
			: base(serverClass)
		{
			EntIndex = entIndex;
			Props = props;
		}
		
		
		public override void AppendToWriter(IndentedWriter iw) {
			iw.Append($"({EntIndex}) DELTA - ({ServerClass.ToString()})");
			iw.FutureIndent++;
			foreach ((int propIndex, EntityProperty prop) in Props) {
				iw.AppendLine();
				iw.Append($"({propIndex}) {prop.ToString()}");
			}
			iw.FutureIndent--;
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
			iw.Append($"({EntIndex}) {(New ? "CREATE" : "ENTER_PVS")} - ({ServerClass.ToString()})");
			if (New)
				iw.Append($", serial: {Serial}");
			iw.FutureIndent++;
			foreach ((int propIndex, EntityProperty prop) in Props) {
				iw.AppendLine();
				iw.Append($"({propIndex}) {prop.ToString()}");
			}
			iw.FutureIndent--;
		}
	}
	
	
	public class LeavePvs : EntityUpdate {
		
		public readonly int Index;
		public readonly bool Delete;
		
		
		public LeavePvs(int index, ServerClass serverClass, bool delete) : base(serverClass) {
			Index = index;
			Delete = delete;
		}
		

		public override void AppendToWriter(IndentedWriter iw) {
			iw.Append($"({Index}) {(Delete ? "DELETE" : "LEAVE_PVS")} - ({ServerClass.ToString()})");
		}
	}
}
