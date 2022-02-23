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
		private BitStreamReader _entBsr;
		public BitStreamReader EntStream => _entBsr.FromBeginning();
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
			_entBsr = bsr.SplitAndSkip((int)dataLen);

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
			DataTableParser? tableParser = GameState.DataTableParser;
			Entity?[] ents = snapshot.Entities; // current entity state

			if (tableParser?.FlattenedProps == null) {
				DemoRef.LogError($"{GetType().Name}: ent parsing cannot continue because data tables have not been parsed");
				DemoRef.DemoParseResult |= DemoParseResult.EntParsingFailed;
				return;
			}

			// the journey begins in src_main\engine\servermsghandler.cpp line 663, warning: it goes 8 layers deep

			if (!IsDelta)
				snapshot.ClearEntityState();

			// game uses different entity frames, so "oldI = old ent index = from" & "newI = new ent index = to"
			int oldI = -1, newI = -1;
			snapshot.GetNextNonNullEntIndex(ref oldI);

			for (int _ = 0; _ < UpdatedEntries; _++) {

				newI += 1 + (DemoInfo.NewDemoProtocol
					? (int)_entBsr.ReadUBitInt()
					: (int)_entBsr.ReadUBitVar());

				// get the old ent index up to at least the new one
				if (newI > oldI) {
					oldI = newI - 1;
					snapshot.GetNextNonNullEntIndex(ref oldI);
				}

				EntityUpdate update;
				// vars used in enter pvs & delta
				ServerClass entClass;
				List<FlattenedProp> fProps;
				int iClass;
				uint updateType = _entBsr.ReadUInt(2);
				switch (updateType) {
					case 0: // delta
						if (oldI != newI)
							throw new ArgumentException("oldEntSlot != newEntSlot");
						iClass = snapshot.Entities[newI].ServerClass.DataTableId;
						(entClass, fProps) = tableParser.FlattenedProps[iClass];
						update = new Delta(newI, entClass, _entBsr.ReadEntProps(fProps, DemoRef));
						snapshot.ProcessDelta((Delta)update);
						snapshot.GetNextNonNullEntIndex(ref oldI);
						break;
					case 2: // enter PVS
						iClass = (int)_entBsr.ReadUInt(tableParser.ServerClassBits);
						uint iSerial = (uint)_entBsr.ReadULong(HandleSerialNumberBits);
						(entClass, fProps) = tableParser.FlattenedProps[iClass];
						bool bNew = ents[newI] == null || ents[newI].Serial != iSerial;
						update = new EnterPvs(newI, entClass, _entBsr.ReadEntProps(fProps, DemoRef), iSerial, bNew);
						snapshot.ProcessEnterPvs(this, (EnterPvs)update); // update baseline check in here
						if (oldI == newI)
							snapshot.GetNextNonNullEntIndex(ref oldI);
						break;
					case 1: // leave PVS
					case 3: // delete
						update = new LeavePvs(oldI, ents[oldI].ServerClass, updateType == 3);
						snapshot.ProcessLeavePvs((LeavePvs)update);
						snapshot.GetNextNonNullEntIndex(ref oldI);
						break;
					default:
						throw new ArgumentException($"unknown ent update type: {updateType}");
				}
				Updates.Add(update);
			}
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"max entries: {MaxEntries}");
			pw.AppendLine($"is delta: {IsDelta}");
			if (IsDelta)
				pw.AppendLine($"delta from: {DeltaFrom}");
			pw.AppendLine($"baseline: {BaseLine}");
			pw.AppendLine($"updated baseline: {UpdateBaseline}");
			pw.AppendLine($"length in bits: {_entBsr.BitLength}");
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
