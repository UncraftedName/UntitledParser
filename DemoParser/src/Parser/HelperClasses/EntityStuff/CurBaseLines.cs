#nullable enable
using System.Collections.Generic;
using DemoParser.Parser.Components.Messages;

namespace DemoParser.Parser.HelperClasses.EntityStuff {
    
    // like CurStringTablesManager, keeps a local copy of the baselines that can be updated and changed whenever
    public class CurBaseLines {

        /* The baseline is an array of all server classes and their default properties.
         * However, a server class will not appear in the demo unless it is actually used in the level, 
         * therefore any element of the baseline will be null until that corresponding slot is initialized.
         * In addition, once initialized, apparently not every property for a given class
         * is always given a default value. (I'm guessing this is for properties that are known to be set to
         * something as soon as the entity is created.)
         */
        public (ServerClass? serverClass, EntityProperty?[]? entityProperties)[] ClassBaselines;
        private readonly SourceDemo _demoRef;


        public CurBaseLines(int maxServerClasses, SourceDemo demoRef) {
            _demoRef = demoRef;
            ClearBaseLineState(maxServerClasses);
        }


        public void ClearBaseLineState(int maxServerClasses) {
            ClassBaselines = new (ServerClass? serverClass, EntityProperty?[]? entityProperties)[maxServerClasses];
        }


        // either updated from the instance baseline in string tables or the dynamic baseline in the entities message
        // this might be wrong, it's possible all updates are stored to a new baseline, so effectively only the last one matters
        public void UpdateBaseLine(
            ServerClass serverClass, 
            IEnumerable<(int propIndex, EntityProperty prop)> props,
            int entPropCount) 
        {
            int i = serverClass.DataTableId;
            if (ClassBaselines[i] == default) { // just init that class slot, i'll write the properties next anyway
                ClassBaselines[i].serverClass = serverClass;
                ClassBaselines[i].entityProperties = new EntityProperty?[entPropCount];
            }
            
            // update the slot
            foreach ((int propIndex, EntityProperty from) in props) {
                ref EntityProperty? to = ref ClassBaselines[i].entityProperties[propIndex];
                if (to.HasValue) {
                    // I think this is only necessary for arrays, where old elements may be kept
                    var tmp = to.Value;
                    from.CopyPropertyTo(ref tmp);
                    to = tmp;
                } else {
                    to = from;
                }
            }
        }


        internal Entity EntFromBaseLine(ServerClass serverClass, uint serial) {
            
            // assume classes[ID] is valid
            int classIndex = serverClass.DataTableId; 
            ref EntityProperty?[]? props = ref ClassBaselines[classIndex].entityProperties;

            // I need this because for now I cannot parse string tables that are encoded with dictionaries,
            // so in anything that isn't 3420 I cannot parse baseline table updates outside of the string table packet.
            if (props == null) {
                _demoRef.LogError("attempted to create entity from a non-existing baseline, " +
                                  $"creating new empty baseline for class: ({serverClass.ToString()})");
                
                List<FlattenedProp> fProps = _demoRef.DataTableParser.FlattenedProps[classIndex].flattenedProps;
                props = new EntityProperty?[fProps.Count];
                ClassBaselines[classIndex].serverClass = serverClass;
            }
            
            EntityProperty?[] newEntProps = new EntityProperty?[props.Length];
            for (int i = 0; i < newEntProps.Length; i++) {
                if (props[i].HasValue)
                    newEntProps[i] = props[i];
                else
                    newEntProps[i] = null;
            }

            return new Entity(serverClass, newEntProps, serial);
        }
    }
}
