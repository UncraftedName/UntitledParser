#nullable enable
using System.Collections.Generic;
using System.Linq;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.EntityStuff;
using DemoParser.Parser.GameState;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets {

	/*
	 * To make entity deltas take up less space, Valve created a lookup table that contains all entity properties that
	 * you then reference when doing entity parsing. It's a pretty neat system. It's also literally the most complicated
	 * thing in the world (citation needed). At a high level, the packet looks like this:
	 *
	 * Exhibit A:
	 *
	 *   DataTables            SendTable         SendProp
	 * ┌─────────────┐        ┌──────────┐       ┌─────┐
	 * │ SendTable 1 │       ┌┤   Name   │       │Name │
	 * ├─────────────┤       │├──────────┤   ┌──►│Type │
	 * │ SendTable 2 ├──────►││SendProp 1├───┘   │Flags│
	 * ├─────────────┤       │├──────────┤       └─────┘
	 * │ SendTable 3 │       ││SendProp 2│
	 * ├─────────────┤       │├──────────┤
	 * │      .      │       ││SendProp 3│
	 * │      .      │       │├──────────┤
	 * │      .      │       ││    .     │
	 * ├─────────────┤       ││    .     │
	 * │ServerClass 1│       └┤    .     │
	 * ├─────────────┤        └──────────┘
	 * │ServerClass 2│
	 * ├─────────────┤          ServerClass
	 * │ServerClass 3├───┐   ┌───────────────┐
	 * ├─────────────┤   └──►│ServerClassName│
	 * │      .      │       │ DataTableName │
	 * │      .      │       └───────────────┘
	 * │      .      │
	 * └─────────────┘
	 *
	 * The ServerClass structure contains a mapping from the DataTable/SendTable name to the corresponding class name
	 * used in game. In this project I also use that structure as an identifier for which class a particular entity is.
	 * The packet structure might not seem so bad, but boy do I have news for you! The main complexity lies in the
	 * SendProp structure and how it references everything else. Normally, a SendProp represents a single field in a
	 * game class like an int or float or whatever. But it can also be used to represent more complex behavior like an
	 * array of other SendProp's or SendTable inheritance. Here's an example of some of the more complicated behavior:
	 *
	 * Exhibit B:
	 *
	 *                       ┌─────────────────────────────────────────────────────────┐
	 *         SendTables    │                                                         │
	 *   ┌───────────────────┴──┐                                                      ▼
	 *   │                      │                                        SendProps for DT_BasePlayer
	 *   │    DT_BasePlayer     │      (inherit properties)     ┌────────────────────────────────────────────┐
	 * ┌─┤                      │              ┌────────────────┤baseclass : DT_BaseCombatCharacter          │
	 * │ ├──────────────────────┤              │                ├────────────────────────────────────────────┤
	 * └►│                      │◄─────────────┴────────────────┤int m_blinktoggle : DT_BaseFlex             │
	 *   │DT_BaseCombatCharacter│ (don't inherit m_blinktoggle) │EXCLUDE                                     │
	 * ┌─┤                      │                               ├────────────────────────────────────────────┤
	 * │ ├──────────────────────┤                               │float m_flFOVTime                           │
	 * └►│                      │                               │(regular 32 bit float)                      │
	 *   │     DT_BaseFlex      │                               ├────────────────────────────────────────────┤
	 *   │                      │                               │float m_flMaxspeed                          │
	 *   ├──────────────────────┤                               │(read 21 bit uint and scale into [0,2047.5])│
	 *   │          .           │                               ├────────────────────────────────────────────┤
	 *   │          .           │                               │int m_hViewModel                            │
	 *   │          .           │                            ┌─►│(read 21 bit uint, part of array)           │
	 *   └──────────────────────┘      each array element is │  ├────────────────────────────────────────────┤
	 *                                  read as 21 bit uint  │  │array m_hViewModel                          │
	 *                                                       └──┤(2 elements)                                │
	 *                                                          ├────────────────────────────────────────────┤
	 *                                                          │                     .                      │
	 *                                                          │                     .                      │
	 *                                                          │                     .                      │
	 *                                                          └────────────────────────────────────────────┘
	 *
	 * The SendProp's are just instructions for how to parse entity info. When you read a message like SvcPacketEntities,
	 * you will read a couple of ints and get some info like "update the 3rd property of the 107th class". So you go to
	 * the DataTables and find that property, then you know that you have to read a 21 bit float or whatever.
	 *
	 * Parsing this packet and getting all the props (Exhibit A) is not terribly complicated. Handling inheritance and
	 * prop order (Exhibit B) is another mess which is handled in the DataTablesManager.
	 */

	/// <summary>
	/// Contains all the information needed to decode entity properties.
	/// </summary>
	public class DataTables : DemoPacket {

		public List<SendTable> Tables;
		public List<ServerClass>? ServerClasses;
		public bool ParseSuccessful;


		public DataTables(SourceDemo? demoRef, PacketFrame frameRef) : base(demoRef, frameRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			BitStreamReader dBsr = bsr.ForkAndSkip(bsr.ReadSInt() * 8);

			Tables = new List<SendTable>();
			if (DemoInfo.Game == SourceGame.PORTAL_REVOLUTION) {
				while (!dBsr.HasOverflowed) {
					var table = new SendTable(DemoRef);
					Tables.Add(table);
					// minimal protobuf parsing just to skip over the payloads
					// there's a dummy table at the end of the list that indicates
					// if we should stop parsing
					uint type = dBsr.ReadVarUInt32(); // protobuf message type
					int size = (int)dBsr.ReadVarUInt32(); // size of message
					// coincidentally, the first message member is the one we are looking for (is_end member)
					// it's an optional member but it's always set on the dummy final table
					// every member begins with a tag byte formatted as (field_number << 3) | wire_type
					// is_end has field_number 1 and wire_type 0
					// if needed in the future, net_table_name has field number 2 and wire_type 2
					int tag = dBsr.ReadByte();
					if (tag != (1 << 3)) {
						dBsr.SkipBytes(size - 1);
						continue;
					}
					byte is_end = dBsr.ReadByte();
					dBsr.SkipBytes(size - 2);
					if (is_end == 1) {
						break;
					}
				}
			}
			else {
				while (dBsr.ReadBool() && !dBsr.HasOverflowed) {
					var table = new SendTable(DemoRef);
					Tables.Add(table);
					table.ParseStream(ref dBsr);
				}
			}

			ushort classCount = dBsr.ReadUShort();
			ServerClasses = new List<ServerClass>(classCount);
			for (int i = 0; i < classCount && !dBsr.HasOverflowed; i++) {
				var serverClass = new ServerClass(DemoRef, null);
				ServerClasses.Add(serverClass);
				serverClass.ParseStream(ref dBsr);
			}
			ParseSuccessful = true;

			// hack to detect tf2 game
			if (DemoInfo.Game == SourceGame.PORTAL_1_1910503 && ServerClasses.Any(serverClass => serverClass.ClassName == "CTFBat"))
				DemoInfo.Game = SourceGame.TF2;

			// in case SvcServerInfo parsing fails
			GameState.EntBaseLines ??= new EntityBaseLines(DemoRef!, ServerClasses.Count);

			// re-init the baselines if the count doesn't match (maybe I should just init them from here?)
			if (GameState.EntBaseLines.Baselines.Length != classCount)
				GameState.EntBaseLines.ClearBaseLineState(classCount);

			// create the prop list for each class
			GameState.DataTablesManager = new DataTablesManager(DemoRef!, this);
			if (DemoInfo.Game != SourceGame.PORTAL_REVOLUTION)
				GameState.DataTablesManager.FlattenClasses(true);
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			if (!ParseSuccessful) {
				pw.Append("failed to parse");
				return;
			}
			pw.Append($"{Tables.Count} send table{(Tables.Count > 1 ? "s" : "")}:");
			pw.FutureIndent++;
			foreach (SendTable sendTable in Tables) {
				pw.AppendLine();
				sendTable.PrettyWrite(pw);
			}
			pw.FutureIndent--;
			pw.AppendLine();
			if ((ServerClasses?.Count ?? 0) > 0) {
				pw.Append($"{ServerClasses!.Count} class{(ServerClasses.Count > 1 ? "es" : "")}:");
				pw.FutureIndent++;
				foreach (ServerClass classInfo in ServerClasses) {
					pw.AppendLine();
					classInfo.PrettyWrite(pw);
				}
				pw.FutureIndent--;
			} else {
				pw.Append("no classes");
			}
		}
	}


	public class SendTable : DemoComponent {

		public bool NeedsDecoder;
		public string Name;
		public int ExpectedPropCount; // for tests
		public List<SendTableProp> SendProps;


		public SendTable(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			NeedsDecoder = bsr.ReadBool();
			Name = bsr.ReadNullTerminatedString();
			ExpectedPropCount = (int)bsr.ReadUInt(DemoInfo.Game == SourceGame.HL2_OE ? 9 : 10);
			if (ExpectedPropCount < 0 || bsr.HasOverflowed)
				return;
			SendProps = new List<SendTableProp>(ExpectedPropCount);
			for (int i = 0; i < ExpectedPropCount; i++) {
				var sendProp = new SendTableProp(DemoRef, this);
				SendProps.Add(sendProp);
				sendProp.ParseStream(ref bsr);
				if (bsr.HasOverflowed)
					return;
			}
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"{Name}{(NeedsDecoder ? "*" : "")} (");
			if (SendProps?.Count > 0) {
				pw.Append($"{SendProps.Count} prop{(SendProps.Count > 1 ? "s" : "")}):");
				pw.FutureIndent++;
				foreach (SendTableProp sendProp in SendProps) {
					pw.AppendLine();
					sendProp.PrettyWrite(pw);
				}
				pw.FutureIndent--;
			} else {
				pw.Append("no props)");
			}
		}
	}
}
