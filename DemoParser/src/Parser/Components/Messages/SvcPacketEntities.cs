#nullable enable
using System;
using System.Collections.Generic;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.EntityStuff;
using DemoParser.Parser.GameState;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {

	public class SvcPacketEntities : DemoMessage {

		private const int HandleSerialNumberBits = 10;

		public ushort MaxEntries;
		public bool IsDelta;
		public int DeltaFrom;
		public uint BaseLine; // which baseline we read from
		public ushort UpdatedEntries;
		// Server requested to use this snapshot as baseline update.
		// Any time this happens, the game swaps the baseline to read from starting on the next packet (hopefully).
		public bool UpdateBaseline;
		public List<EntityUpdate>? Updates;


		public SvcPacketEntities(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {

			// first, we read the main message info here
			MaxEntries = (ushort)bsr.ReadUInt(11);
			IsDelta = bsr.ReadBool();
			DeltaFrom = IsDelta ? bsr.ReadSInt() : -1;
			BaseLine = bsr.ReadUInt(1);
			UpdatedEntries = (ushort)bsr.ReadUInt(11);
			uint dataLen = bsr.ReadUInt(20);
			UpdateBaseline = bsr.ReadBool();
			BitStreamReader entBsr = bsr.ForkAndSkip((int)dataLen);

#if !FORCE_PROCESS_ENTS
			if ((DemoRef.DemoParseResult & DemoParseResult.EntParsingEnabled) == 0 ||
				(DemoRef.DemoParseResult & DemoParseResult.EntParsingFailed) != 0)
				return;
#endif
			// now, we do some setup for ent parsing
			ref EntitySnapshot? snapshot = ref GameState.EntitySnapshot;
			snapshot ??= new EntitySnapshot(DemoRef);

			if (IsDelta && snapshot.EngineTick != DeltaFrom) {
				// If the messages ever arrive in a different order I should queue them,
				// but for now just exit if we're updating from a non-existent snapshot.
				DemoRef.LogError($"{GetType().Name}: attempted to retrieve non existent snapshot on engine tick {DeltaFrom}");
				DemoRef.DemoParseResult |= DemoParseResult.EntParsingFailed;
				return;
			}

			Updates = new List<EntityUpdate>(UpdatedEntries);
			DataTablesManager? dtMgr = GameState.DataTablesManager;
			Entity?[] ents = snapshot.Entities; // current entity state

			if (dtMgr?.FlattenedProps == null) {
				DemoRef.LogError($"{GetType().Name}: ent parsing cannot continue because data tables have not been parsed");
				DemoRef.DemoParseResult |= DemoParseResult.EntParsingFailed;
				return;
			}

			// the journey begins in src_main\engine\servermsghandler.cpp line 663, warning: it goes 8 layers deep

			if (!IsDelta)
				snapshot.ClearEntityState();

			int idx = -1;

			for (int _ = 0; _ < UpdatedEntries; _++) {

				idx += 1 + (DemoInfo.NewDemoProtocol
					? (int)entBsr.ReadUBitInt()
					: (int)entBsr.ReadUBitVar());

				EntityUpdate update;
				// vars used in enter pvs & delta
				ServerClass entClass;
				List<FlattenedProp> fProps;
				int iClass;
				uint updateType = entBsr.ReadUInt(2);
				switch (updateType) {
					case 0: // delta
						if (snapshot.Entities[idx] == null)
							throw new ArgumentException("attempt to delta null entity");
						iClass = snapshot.Entities[idx].ServerClass.DataTableId;
						(entClass, fProps) = dtMgr.FlattenedProps[iClass];
						update = new Delta(idx, entClass, entBsr.ReadEntProps(fProps, DemoRef));
						snapshot.ProcessDelta((Delta)update);
						break;
					case 2: // enter PVS
						iClass = (int)entBsr.ReadUInt(dtMgr.ServerClassBits);
						uint iSerial = (uint)entBsr.ReadULong(HandleSerialNumberBits);
						(entClass, fProps) = dtMgr.FlattenedProps[iClass];
						bool bNew = ents[idx] == null || ents[idx].Serial != iSerial;
						update = new EnterPvs(idx, entClass, entBsr.ReadEntProps(fProps, DemoRef), iSerial, bNew);
						snapshot.ProcessEnterPvs(this, (EnterPvs)update); // update baseline check in here
						break;
					case 1: // leave PVS
					case 3: // delete
						if (snapshot.Entities[idx] == null)
							throw new ArgumentException("attempt to delete null entity");
						update = new LeavePvs(idx, ents[idx].ServerClass, updateType == 3);
						snapshot.ProcessLeavePvs((LeavePvs)update);
						break;
					default:
						throw new ArgumentException($"unknown ent update type: {updateType}");
				}
				Updates.Add(update);
			}

			// process explicit deletes
			if (IsDelta) {
				while (entBsr.ReadBool()) {
					// idx may be a null entity for some reason ¯\_(ツ)_/¯
					idx = (int)entBsr.ReadUInt(DemoInfo.MaxEdictBits);
					LeavePvs update = new LeavePvs(idx, ents[idx]?.ServerClass, true);
					snapshot.ProcessLeavePvs(update);
					Updates.Add(update);
				}
			}
			if (entBsr.BitsRemaining != 0 || entBsr.HasOverflowed)
				bsr.SetOverflow();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"max entries: {MaxEntries}");
			pw.AppendLine($"is delta: {IsDelta}");
			if (IsDelta)
				pw.AppendLine($"delta from: {DeltaFrom}");
			pw.AppendLine($"baseline: {BaseLine}");
			pw.AppendLine($"updated baseline: {UpdateBaseline}");
			pw.Append($"{UpdatedEntries} updated entries");
			if ((DemoRef.DemoParseResult & DemoParseResult.EntParsingEnabled) != 0) {
				pw.Append(":");
				pw.FutureIndent++;
				if (Updates == null) {
					pw.Append("\nupdates could not be parsed");
				} else {
					foreach (EntityUpdate update in Updates) {
						pw.AppendLine();
						update.PrettyWrite(pw);
					}
				}
				pw.FutureIndent--;
			} else {
				pw.Append("\n(entity parsing not supported for this game)");
			}
		}
	}
}
