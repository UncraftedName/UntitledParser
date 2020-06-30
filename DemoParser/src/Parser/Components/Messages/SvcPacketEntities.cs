#nullable enable
using System;
using System.Collections.Generic;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.HelperClasses;
using DemoParser.Parser.HelperClasses.EntityStuff;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	public class SvcPacketEntities : DemoMessage {

		public ushort MaxEntries;
		public bool IsDelta;
		public int DeltaFrom;
		public uint BaseLine; // which baseline we read from
		public ushort UpdatedEntries;
		// Server requested to use this snapshot as baseline update.
		// Any time this happens, the game swaps the baseline to read from starting on the next packet (hopefully).
		public bool UpdateBaseline;
		private BitStreamReader _entBsr;
		public BitStreamReader EntStream => _entBsr.FromBeginning();
		public List<EntityUpdate> Updates;
		

		public SvcPacketEntities(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			
			// first, we read the main message info here
			MaxEntries = (ushort)bsr.ReadBitsAsUInt(11);
			IsDelta = bsr.ReadBool();
			DeltaFrom = IsDelta ? bsr.ReadSInt() : -1;
			BaseLine = bsr.ReadBitsAsUInt(1);
			UpdatedEntries = (ushort)bsr.ReadBitsAsUInt(11);
			uint dataLen = bsr.ReadBitsAsUInt(20);
			UpdateBaseline = bsr.ReadBool();
			_entBsr = bsr.SubStream(dataLen);
			bsr.SkipBits(dataLen);
			SetLocalStreamEnd(bsr);

			if (!DemoSettings.ProcessEnts)
				return;
			
			// now, we do some setup for ent parsing
			ref C_EntitySnapshot snapshot = ref DemoRef.CEntitySnapshot;
			if (snapshot == null)
				snapshot = new C_EntitySnapshot(DemoRef);

			if (IsDelta && snapshot.EngineTick != DeltaFrom) {
				// If the messages ever arrive in a different order I should queue them,
				// but for now just exit if we're updating from a non-existent snapshot.
				DemoRef.LogError(
					$"{GetType().Name} failed to process entity delta, " + 
					$"attempted to retrieve non existent snapshot on engine tick: {DeltaFrom}");
				return;
			}

			Updates = new List<EntityUpdate>(UpdatedEntries);
			DataTableParser tableParser = DemoRef.DataTableParser;
			Entity?[] ents = snapshot.Entities; // current entity state
			
			try { // the journey begins in src_main\engine\servermsghandler.cpp  line 663, warning: it goes 8 layers deep  
				if (!IsDelta)
					snapshot.ClearEntityState();

				// game uses different entity frames, so "oldI = old ent index = from" & "newI = new ent index = to"
				int oldI = -1, newI = -1;
				NextOldEntIndex(ref oldI, ents);

				for (int _ = 0; _ < UpdatedEntries; _++) {
					newI += 1 + (DemoSettings.NewDemoProtocol 
						? (int)_entBsr.ReadUBitInt()
						: (int)_entBsr.ReadUBitVar());
					
					
					UpdateType updateType;
					if (_entBsr.ReadBool()) {
						updateType = UpdateType.LeavePvs;
						if (_entBsr.ReadBool())
							updateType = UpdateType.Delete;
					} else {
						updateType = UpdateType.Delta;
						if (_entBsr.ReadBool())
							updateType = UpdateType.EnterPvs;
					}
					// get the old ent index up to at least the new one
					if (newI > oldI) {
						oldI = newI - 1;
						NextOldEntIndex(ref oldI, ents);
					}

					EntityUpdate update;
					// vars used in enter pvs & delta
					ServerClass entClass; 
					List<FlattenedProp> fProps; 
					int iClass;
					switch (updateType) {
						case UpdateType.EnterPvs:
							iClass = (int)_entBsr.ReadBitsAsUInt(tableParser.ServerClassBits);
							uint iSerial = _entBsr.ReadBitsAsUInt(DemoSettings.HandleSerialNumberBits);
							(entClass, fProps) = tableParser.FlattenedProps[iClass];
							bool bNew = ents[newI] == null || ents[newI].Serial != iSerial;
							update = new EnterPvs(newI, entClass, _entBsr.ReadEntProps(fProps, DemoRef), iSerial, bNew);
							snapshot.ProcessEnterPvs(this, (EnterPvs)update); // update baseline check in here
							if (oldI == newI)
								NextOldEntIndex(ref oldI, ents);
							break;
						case UpdateType.Delta:
							if (oldI != newI)
								throw new ArgumentException("oldEntSlot != newEntSlot");
							Entity e = snapshot.Entities[newI];
							iClass = e.ServerClass.DataTableId;
							(entClass, fProps) = tableParser.FlattenedProps[iClass];
							update = new Delta(newI, entClass, _entBsr.ReadEntProps(fProps, DemoRef));
							snapshot.ProcessDelta((Delta)update);
							NextOldEntIndex(ref oldI, ents);
							break;
						case UpdateType.LeavePvs:
						case UpdateType.Delete:
							update = new LeavePvs(oldI, ents[oldI].ServerClass, updateType == UpdateType.Delete);
							snapshot.ProcessLeavePvs((LeavePvs)update);
							NextOldEntIndex(ref oldI, ents);
							break;
						default:
							throw new ArgumentException($"unknown update type in {GetType()}, :{updateType}");
					}
					Updates.Add(update);
				}
			} catch (Exception e) {
				Updates = null;
				DemoRef.LogError($"Exception while parsing entity info in {GetType().Name}: {e.Message}");
			}
		}
		
		
		private static void NextOldEntIndex(ref int index, IReadOnlyList<Entity?> oldEnts) {
			do {
				index++;
				if (index >= oldEnts.Count) {
					index = int.MaxValue; // entity sentinel, something bigger than the number of entities
					return;
				}
			} while (oldEnts[index] == null);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"max entries: {MaxEntries}");
			iw.AppendLine($"is delta: {IsDelta}");
			if (IsDelta)
				iw.AppendLine($"delta from: {DeltaFrom}");
			iw.AppendLine($"baseline: {BaseLine}");
			iw.AppendLine($"updated baseline: {UpdateBaseline}");
			iw.AppendLine($"length in bits: {_entBsr.BitLength}");
			iw.Append($"{UpdatedEntries} updated entries");
			if (DemoSettings.ProcessEnts) {
				iw.Append(":");
				iw.FutureIndent++;
				if (Updates == null) {
					iw.Append("\nupdates could not be parsed");
				} else {
					foreach (EntityUpdate update in Updates) {
						iw.AppendLine();
						update.AppendToWriter(iw);
					}
				}
				iw.FutureIndent--;
			} else {
				iw.Append("\n(entity parsing not supported for this game)");
			}
		}
	}
	
	
	public enum UpdateType { // src_main/common/protocol.h
		EnterPvs = 0, // New entity or entity reentered PVS (potentially visible system)
		LeavePvs,     // Entity left PVS
		Delete,       // This is a LeavePVS message, but with a delete flag
		Delta         // There is a delta for this entity
	}
}