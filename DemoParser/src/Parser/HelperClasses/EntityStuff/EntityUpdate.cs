using System.Collections.Generic;
using DemoParser.Parser.Components.Messages;
using DemoParser.Utils;

namespace DemoParser.Parser.HelperClasses.EntityStuff {

	// base class
	public abstract class EntityUpdate : PrettyClass {
		
		public readonly ServerClass ServerClass;
		
		protected EntityUpdate(ServerClass serverClass) {
			ServerClass = serverClass;
		}
	}
	

	/// <summary>
	/// An entity update consisting of only deltas to a previous entity state.
	/// </summary>
	public class Delta : EntityUpdate {
		
		public readonly int EntIndex;
		public readonly IReadOnlyList<(int propIndex, EntityProperty prop)> Props;
		
		
		public Delta(int entIndex, ServerClass serverClass, IReadOnlyList<(int propIndex, EntityProperty prop)> props) 
			: base(serverClass)
		{
			EntIndex = entIndex;
			Props = props;
		}
		
		
		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"({EntIndex}) DELTA - ");
			ServerClass.PrettyWrite(pw);
			pw.FutureIndent++;
			foreach ((int propIndex, EntityProperty prop) in Props) {
				pw.AppendLine();
				pw.Append($"({propIndex}) ");
				prop.PrettyWrite(pw);
			}
			pw.FutureIndent--;
		}
	}
	
	
	/// <summary>
	/// An entity update where an entity enters the PVS, as well as any deltas from the baseline/previous entity state.
	/// </summary>
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
		

		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"({EntIndex}) {(New ? "CREATE" : "ENTER_PVS")} - ");
			ServerClass.PrettyWrite(pw);
			if (New)
				pw.Append($", serial: {Serial}");
			pw.FutureIndent++;
			foreach ((int propIndex, EntityProperty prop) in Props) {
				pw.AppendLine();
				pw.Append($"({propIndex}) ");
				prop.PrettyWrite(pw);
			}
			pw.FutureIndent--;
		}
	}
	
	
	/// <summary>
	/// An entity update where an entity leaves the PVS, possibly being deleted.
	/// </summary>
	public class LeavePvs : EntityUpdate {
		
		public readonly int Index;
		public readonly bool Delete;
		
		
		public LeavePvs(int index, ServerClass serverClass, bool delete) : base(serverClass) {
			Index = index;
			Delete = delete;
		}
		

		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"({Index}) {(Delete ? "DELETE" : "LEAVE_PVS")} - ");
			ServerClass.PrettyWrite(pw);
		}
	}
}
