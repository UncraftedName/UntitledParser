using System;
using System.Diagnostics;
using System.Linq;
using DemoParser.Parser.Components.Messages;

namespace DemoParser.Parser.HelperClasses.EntityStuff {
    
    /// <summary>
    /// Represents the entity state at any given time.
    /// </summary>
    public class C_EntitySnapshot {
        
        private readonly SourceDemo _demoRef;
        public readonly Entity[] Entities;
        public uint EngineTick; // set from the packet packet


        public C_EntitySnapshot(SourceDemo demoRef) {
            _demoRef = demoRef;
            Entities = new Entity[SourceDemoSettings.MaxEdicts];
        }


        private C_EntitySnapshot(SourceDemo demoRef, Entity[] entities, uint engineTick) {
            _demoRef = demoRef;
            Entities = entities;
            EngineTick = engineTick;
        }


        public C_EntitySnapshot DeepCopy() => 
            new C_EntitySnapshot(_demoRef, Entities.Select(entity => entity?.Duplicate()).ToArray(), EngineTick);


        internal void ClearEntityState() {
            Array.Clear(Entities, 0, Entities.Length);
        }
        

        internal void ProcessEnterPvs(SvcPacketEntities msg, EnterPvs u) {
            if (u.New) // force a recreate
                Entities[u.EntIndex] = _demoRef.CBaseLines.EntFromBaseLine(u.ServerClass, u.Serial);
            Entity e = Entities[u.EntIndex];
            Debug.Assert(e != null, "entity should not be null by now");
            e.InPvs = true;
            ProcessDelta(u);
            if (msg.UpdateBaseline) { // if update baseline then set the current baseline to the ent props, wacky
                _demoRef.CBaseLines.UpdateBaseLine(
                    u.ServerClass,
                    e.Props.Select((property, i) => (i, property)).Where(tuple => tuple.property != null),
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
                ref EntityProperty old = ref Entities[u.EntIndex].Props[propIndex];
                if (old == null)
                    old = prop.CopyProperty();
                else
                    prop.CopyPropertyTo(old);
            }
        }
    }
}