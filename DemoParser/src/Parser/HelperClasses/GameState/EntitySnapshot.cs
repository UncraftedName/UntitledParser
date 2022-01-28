using System;
using System.Diagnostics;
using System.Linq;
using C5;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.HelperClasses.EntityStuff;

namespace DemoParser.Parser.HelperClasses.GameState {

	/// <summary>
	/// Represents the entity state, updated as the demo is parsed.
	/// </summary>
	public class EntitySnapshot {

		private readonly SourceDemo _demoRef;
		public readonly Entity?[] Entities;
		private readonly TreeSet<int> _nonNullEnts;
		public uint EngineTick; // set from the packet packet


		public EntitySnapshot(SourceDemo demoRef) {
			_demoRef = demoRef;
			Entities = new Entity?[DemoInfo.MaxEdicts];
			_nonNullEnts = new TreeSet<int>();
		}


		private EntitySnapshot(SourceDemo demoRef, Entity?[] entities, uint engineTick) {
			_demoRef = demoRef;
			Entities = entities;
			_nonNullEnts = new TreeSet<int>();
			_nonNullEnts.AddSorted(entities.Select((e,  i) => (e, i)).Where(t => t.e != null).Select(t => t.i));
			EngineTick = engineTick;
		}


		public EntitySnapshot DeepCopy() =>
			new EntitySnapshot(_demoRef, Entities.Select(entity => entity?.DeepCopy()).ToArray(), EngineTick);


		internal void ClearEntityState() {
			Array.Clear(Entities, 0, Entities.Length);
			_nonNullEnts.Clear();
		}


		internal void ProcessEnterPvs(SvcPacketEntities msg, EnterPvs u) {
			Debug.Assert(_demoRef.EntBaseLines != null, "baselines are null");
			if (u.New) {
				// create the ent
				Entities[u.EntIndex] = _demoRef.EntBaseLines.EntFromBaseLine(u.ServerClass, u.Serial);
				_nonNullEnts.UpdateOrAdd(u.EntIndex);
			}
			Entity e = Entities[u.EntIndex] ?? throw new InvalidOperationException($"entity {u.EntIndex} should not be null by now");
			e.InPvs = true;
			ProcessDelta(u);
			if (msg.UpdateBaseline) { // if update baseline then set the current baseline to the ent props, wacky
				_demoRef.EntBaseLines.UpdateBaseLine(
					u.ServerClass,
					e.Props.Select((property, i) => (i, property)).Where(tuple => tuple.property != null),
					e.Props.Length);
			}
		}


		internal void ProcessLeavePvs(LeavePvs u) {
			if (u.Delete) {
				Entities[u.Index] = null;
				_nonNullEnts.Remove(u.Index);
			} else {
				Entities[u.Index].InPvs = false;
			}
		}


		internal void ProcessDelta(Delta u) {
			foreach ((int propIndex, EntityProperty prop) in u.Props) {
				ref EntityProperty? old = ref Entities[u.EntIndex].Props[propIndex];
				if (old == null)
					old = prop.CopyProperty();
				else
					prop.CopyPropertyTo(old);
			}
		}


		internal void GetNextNonNullEntIndex(ref int index) {
			if (index >= Entities.Length)
				return;
			if (!_nonNullEnts.TrySuccessor(index, out index))
				index = int.MaxValue; // something bigger than the max number of ents
		}
	}
}
