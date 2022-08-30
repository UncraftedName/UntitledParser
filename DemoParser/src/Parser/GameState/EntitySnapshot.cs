using System;
using System.Diagnostics;
using System.Linq;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.EntityStuff;

namespace DemoParser.Parser.GameState {

	/// <summary>
	/// Represents the entity state, updated as the demo is parsed.
	/// </summary>
	public class EntitySnapshot {

		private readonly SourceDemo _demoRef;
		public readonly Entity?[] Entities;
		public uint EngineTick; // set from the packet packet


		public EntitySnapshot(SourceDemo demoRef) {
			_demoRef = demoRef;
			Entities = new Entity?[DemoInfo.MaxEdicts];
		}


		internal void ClearEntityState() {
			Array.Clear(Entities, 0, Entities.Length);
		}


		internal void ProcessEnterPvs(SvcPacketEntities msg, EnterPvs u) {
			Debug.Assert(_demoRef.State.EntBaseLines != null, "baselines are null");
			if (u.New) {
				// create the ent
				Entities[u.EntIndex] = _demoRef.State.EntBaseLines.EntFromBaseLine(u.ServerClass!, u.Serial);
			}
			Entity e = Entities[u.EntIndex] ?? throw new InvalidOperationException($"entity {u.EntIndex} should not be null by now");
			e.InPvs = true;
			ProcessDelta(u);
			if (msg.UpdateBaseline) { // if update baseline then set the current baseline to the ent props, wacky
				_demoRef.State.EntBaseLines.UpdateBaseLine(
					u.ServerClass!,
					e.Props.Select((property, i) => (i, property)).Where(tuple => tuple.property != null)!,
					e.Props.Length);
			}
		}


		internal void ProcessLeavePvs(LeavePvs u) {
			if (u.Delete)
				Entities[u.Index] = null;
			else
				Entities[u.Index].InPvs = false;
		}


		internal void ProcessDelta(Delta u) {
			foreach ((int propIndex, EntityProperty prop) in u.Props) {
				ref EntityProperty? old = ref Entities[u.EntIndex].Props[propIndex];
				if (prop is ArrEntProp newArr) {
					if (old == null)
						old = newArr.CopyArrayProp();
					else
						newArr.UpdateArrayProp((ArrEntProp)old);
				} else {
					old = prop;
				}
			}
		}
	}
}
