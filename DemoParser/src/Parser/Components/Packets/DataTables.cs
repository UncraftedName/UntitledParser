#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.HelperClasses.EntityStuff;
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


		public DataTables(SourceDemo? demoRef, PacketFrame frameRef) : base(demoRef, frameRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			int byteSize = (int)bsr.ReadUInt();
			int indexBeforeData = bsr.CurrentBitIndex;
			try {
				Tables = new List<SendTable>();
				while (bsr.ReadBool()) {
					Tables.Add(new SendTable(DemoRef));
					Tables[^1].ParseStream(ref bsr);
				}

				ushort classCount = bsr.ReadUShort();
				ServerClasses = new List<ServerClass>(classCount);
				for (int i = 0; i < classCount; i++) {
					// class info's come at the end
					ServerClasses.Add(new ServerClass(DemoRef, null));
					ServerClasses[^1].ParseStream(ref bsr);
					// this is an assumption I make in the structure of all the entity stuff, very critical
					if (i != ServerClasses[i].DataTableId)
						throw new ConstraintException("server class ID does not match its index in the list");
				}

				// in case SvcServerInfo parsing fails
				DemoRef.CBaseLines ??= new CurBaseLines(DemoRef, ServerClasses.Count);

				// re-init the baselines if the count doesn't match (maybe I should just init them from here?)
				if (DemoRef.CBaseLines!.ClassBaselines.Length != classCount)
					DemoRef.CBaseLines.ClearBaseLineState(classCount);

				// create the prop list for each class
				DemoRef.DataTableParser = new DataTableParser(DemoRef, this);
				DemoRef.DataTableParser.FlattenClasses(true);
			} catch (Exception e) {
				DemoRef.LogError($"exception while parsing datatables\n\texception: {e.Message}");
				Debug.WriteLine(e);
			}

			bsr.CurrentBitIndex = indexBeforeData + (byteSize << 3);
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			Debug.Assert(Tables.Count > 0, "there's no tables hmmmmmmmmmmmm");
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
		public List<SendTableProp> SendProps;


		public SendTable(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			NeedsDecoder = bsr.ReadBool();
			Name = bsr.ReadNullTerminatedString();
			uint propCount = bsr.ReadBitsAsUInt(10);
			SendProps = new List<SendTableProp>((int)propCount);
			for (int i = 0; i < propCount; i++) {
				SendProps.Add(new SendTableProp(DemoRef, this));
				SendProps[^1].ParseStream(ref bsr);
			}
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
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
