#nullable enable
using System.Collections.Generic;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.EntityStuff;

namespace DemoParser.Parser.GameState {

	// like the StringTables, the baselines are updated as we parse the demo
	public class EntityBaseLines {

		/* The baseline is an array of all server classes and their default properties.
		 * However, a server class will not appear in the demo unless it is actually used in the level,
		 * therefore any element of the baseline will be null until that corresponding slot is initialized.
		 * In addition, once initialized, apparently not every property for a given class
		 * is always given a default value. (I'm guessing this is for properties that are known to be set to
		 * something as soon as the entity is created.)
		 */
		public (ServerClass? serverClass, EntityProperty?[]? entityProperties)[] Baselines;
		private readonly SourceDemo _demoRef;


		public EntityBaseLines(SourceDemo demoRef, int maxServerClasses) {
			_demoRef = demoRef;
			ClearBaseLineState(maxServerClasses);
		}


		public void ClearBaseLineState(int maxServerClasses) {
			Baselines = new (ServerClass? serverClass, EntityProperty?[]? entityProperties)[maxServerClasses];
		}


		// either updated from the instance baseline in string tables or the dynamic baseline in the entities message
		// this might be wrong, it's possible all updates are stored to a new baseline, so effectively only the last one matters
		public void UpdateBaseLine(
			ServerClass serverClass,
			IEnumerable<(int propIndex, EntityProperty prop)> props,
			int entPropCount)
		{
			int i = serverClass.DataTableId;
			if (Baselines[i]! == default) { // just init that class slot, i'll write the properties next anyway
				Baselines[i].serverClass = serverClass;
				Baselines[i].entityProperties = new EntityProperty[entPropCount];
			}

			// update the slot
			foreach ((int propIndex, EntityProperty from) in props) {
				ref EntityProperty? to = ref Baselines[i].entityProperties[propIndex];
				if (to is ArrEntProp toArr)
					((ArrEntProp)from).UpdateArrayProp(toArr);
				else if (from is ArrEntProp fromArr)
					to = fromArr.CopyArrayProp();
				else
					to = from;
			}
		}


		internal Entity EntFromBaseLine(ServerClass serverClass, uint serial) {

			int classIndex = serverClass.DataTableId;
			ref EntityProperty?[]? props = ref Baselines[classIndex].entityProperties;

			// In most versions I can't parse all string table updates which contain baselines, so create a blank baseline
			if (props == null) {
				_demoRef.LogError($"{GetType().Name}: creating new empty baseline for class ({serverClass.ToString()})");
				List<FlattenedProp> fProps = _demoRef.State.DataTableParser.FlattenedProps[classIndex].flattenedProps;
				props = new EntityProperty[fProps.Count];
				Baselines[classIndex].serverClass = serverClass;
			}

			EntityProperty?[] newProps = new EntityProperty?[props.Length];
			for (int i = 0; i < newProps.Length; i++) {
				if (props[i] is ArrEntProp fromArr)
					newProps[i] = fromArr.CopyArrayProp();
				else
					newProps[i] = props[i];
			}

			return new Entity(serverClass, newProps, serial);
		}
	}
}
