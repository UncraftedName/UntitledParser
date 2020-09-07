#nullable enable
using System;
using System.Collections.Generic;
using DemoParser.Parser.Components.Abstract;
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
		public List<EntityUpdate>? Updates;
		

		public SvcPacketEntities(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			
			// first, we read the main message info here
			MaxEntries = (ushort)bsr.ReadBitsAsUInt(11);
			IsDelta = bsr.ReadBool();
			DeltaFrom = IsDelta ? bsr.ReadSInt() : -1;
			BaseLine = bsr.ReadBitsAsUInt(1);
			UpdatedEntries = (ushort)bsr.ReadBitsAsUInt(11);
			uint dataLen = bsr.ReadBitsAsUInt(20);
			UpdateBaseline = bsr.ReadBool();
			_entBsr = bsr.SplitAndSkip(dataLen);

#if !FORCE_PROCESS_ENTS
			if (!DemoSettings.ProcessEnts)
				return;
#endif
			// now, we do some setup for ent parsing
			ref CurEntitySnapshot? snapshot = ref DemoRef.CurEntitySnapshot;
			snapshot ??= new CurEntitySnapshot(DemoRef);
			
			if (IsDelta && snapshot.EngineTick != DeltaFrom) {
				// If the messages ever arrive in a different order I should queue them,
				// but for now just exit if we're updating from a non-existent snapshot.
				DemoRef.LogError(
					$"{GetType().Name} failed to process entity delta, " +
					$"attempted to retrieve non existent snapshot on engine tick: {DeltaFrom}");
				return;
			}
			
			Updates = new List<EntityUpdate>(UpdatedEntries);
			DataTableParser? tableParser = DemoRef.DataTableParser;
			Entity?[] ents = snapshot.Entities; // current entity state

			if (tableParser == null) {
				DemoRef.LogError("ent parsing cannot continue because data tables are not set");
				return;
			}
			
			try { // the journey begins in src_main\engine\servermsghandler.cpp line 663, warning: it goes 8 layers deep
				if (!IsDelta)
					snapshot.ClearEntityState();
				
				// game uses different entity frames, so "oldI = old ent index = from" & "newI = new ent index = to"
				int oldI = -1, newI = -1;
				NextOldEntIndex(ref oldI, ents);
				
				for (int _ = 0; _ < UpdatedEntries; _++) {
					
					newI += 1 + (DemoSettings.NewDemoProtocol
						? (int)_entBsr.ReadUBitInt()
						: (int)_entBsr.ReadUBitVar());
					
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
					uint updateType = _entBsr.ReadBitsAsUInt(2);
					switch (updateType) {
						case 0: // delta
							if (oldI != newI)
								throw new ArgumentException("oldEntSlot != newEntSlot");
							iClass = snapshot.Entities[newI].ServerClass.DataTableId;
							(entClass, fProps) = tableParser.FlattenedProps[iClass];
							update = new Delta(newI, entClass, _entBsr.ReadEntProps(fProps, DemoRef));
							snapshot.ProcessDelta((Delta)update);
							NextOldEntIndex(ref oldI, ents);
							break;
						case 2: // enter PVS
							iClass = (int)_entBsr.ReadBitsAsUInt(tableParser.ServerClassBits);
							uint iSerial = _entBsr.ReadBitsAsUInt(DemoSettings.HandleSerialNumberBits);
							(entClass, fProps) = tableParser.FlattenedProps[iClass];
							bool bNew = ents[newI] == null || ents[newI].Serial != iSerial;
							update = new EnterPvs(newI, entClass, _entBsr.ReadEntProps(fProps, DemoRef), iSerial, bNew);
							snapshot.ProcessEnterPvs(this, (EnterPvs)update); // update baseline check in here
							if (oldI == newI)
								NextOldEntIndex(ref oldI, ents);
							break;
						case 1: // leave PVS
						case 3: // delete
							update = new LeavePvs(oldI, ents[oldI].ServerClass, updateType == 3);
							snapshot.ProcessLeavePvs((LeavePvs)update);
							NextOldEntIndex(ref oldI, ents);
							break;
						default:
							throw new ArgumentException($"unknown ent update type: {updateType}");
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
					index = int.MaxValue - 1; // entity sentinel, something bigger than the number of entities
					return;
				}
			} while (oldEnts[index] == null);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IIndentedWriter iw) {
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
}