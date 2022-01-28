#nullable enable
using System.Collections.Generic;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.HelperClasses.EntityStuff;

namespace DemoParser.Parser.HelperClasses.GameState {

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
            IEnumerable<(int propIndex, EntityProperty? prop)> props,
            int entPropCount)
        {
            int i = serverClass.DataTableId;
            if (Baselines[i]! == default) { // just init that class slot, i'll write the properties next anyway
                Baselines[i].serverClass = serverClass;
                Baselines[i].entityProperties = new EntityProperty[entPropCount];
            }

            // update the slot
            foreach ((int propIndex, EntityProperty? from) in props) {
                ref EntityProperty? to = ref Baselines[i].entityProperties[propIndex];
                if (to == null)
                    to = from.CopyProperty();
                else
                    from.CopyPropertyTo(to); // I think this is only necessary for arrays, where old elements may be kept
            }
        }


        internal Entity EntFromBaseLine(ServerClass serverClass, uint serial) {

            // assume classes[ID] is valid
            int classIndex = serverClass.DataTableId;
            ref EntityProperty?[]? props = ref Baselines[classIndex].entityProperties;

            // I need this because for now I cannot parse string tables that are encoded with dictionaries,
            // so in anything that isn't 3420 I cannot parse baseline table updates outside of the string table packet.
            if (props == null) {
                _demoRef.LogError("attempted to create entity from a non-existing baseline, " +
                                  $"creating new empty baseline for class: ({serverClass.ToString()})");

                List<FlattenedProp> fProps = _demoRef.DataTableParser.FlattenedProps[classIndex].flattenedProps;
                props = new EntityProperty[fProps.Count];
                Baselines[classIndex].serverClass = serverClass;
            }

            EntityProperty?[] newEntProps = new EntityProperty[props.Length];
            for (int i = 0; i < newEntProps.Length; i++) {
                if (props[i] == null)
                    newEntProps[i] = null;
                else
                    newEntProps[i] = props[i].CopyProperty();
            }

            return new Entity(serverClass, newEntProps, serial);
        }
    }
}
