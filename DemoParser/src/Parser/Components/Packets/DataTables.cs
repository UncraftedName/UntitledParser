#nullable enable
using System.Collections.Generic;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.EntityStuff;
using DemoParser.Parser.GameState;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets {
	/*
	 * So fuck me this is pretty complicated, but this packet consists of a bunch of tables, each of which contain a
	 * list of properties and/or references to other tables. See the data tables manager to see how those properties
	 * are unpacked and flattened. Once they are, they serve as a massive lookup table to decode entity properties.
	 * For instance, when reading the entity info, it will say something along the lines of 'update the 3rd property
	 * of the 107th class'. Then you would use the lookup tables to determine that that corresponds to the m_vecOrigin
	 * property of the 'CProp_Portal' class, which has a corresponding data table called 'DT_Prop_Portal'.
	 * That property is also a vector 3, and must be read accordingly. Needless to say, it gets a little wack.
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
			while (dBsr.ReadBool()) {
				var table = new SendTable(DemoRef);
				Tables.Add(table);
				table.ParseStream(ref dBsr);
				if (dBsr.HasOverflowed)
					return;
			}

			ushort classCount = dBsr.ReadUShort();
			ServerClasses = new List<ServerClass>(classCount);
			for (int i = 0; i < classCount; i++) {
				var serverClass = new ServerClass(DemoRef, null);
				ServerClasses.Add(serverClass);
				serverClass.ParseStream(ref dBsr);
				if (dBsr.HasOverflowed)
					return;
			}
			ParseSuccessful = true;

			// in case SvcServerInfo parsing fails
			GameState.EntBaseLines ??= new EntityBaseLines(DemoRef!, ServerClasses.Count);

			// re-init the baselines if the count doesn't match (maybe I should just init them from here?)
			if (GameState.EntBaseLines.Baselines.Length != classCount)
				GameState.EntBaseLines.ClearBaseLineState(classCount);

			// create the prop list for each class
			GameState.DataTableParser = new DataTableParser(DemoRef!, this);
			GameState.DataTableParser.FlattenClasses(true);
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
			if (SendProps.Count > 0) {
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
