#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.GameState;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {

	public class SvcClassInfo : DemoMessage {

		public bool CreateOnClient;
		public ServerClass[]? ServerClasses;
		public ushort ClassCount;


		public SvcClassInfo(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {
			ClassCount = bsr.ReadUShort();
			CreateOnClient = bsr.ReadBool();
			if (!CreateOnClient) {
				// TODO should this update the manager?
				ServerClasses = new ServerClass[ClassCount];
				for (int i = 0; i < ServerClasses.Length; i++) {
					ServerClasses[i] = new ServerClass(DemoRef, this);
					ServerClasses[i].ParseStream(ref bsr);
				}
			}

			GameState.EntBaseLines ??= new EntityBaseLines(DemoRef!, ClassCount);
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"create on client: {CreateOnClient}");
			pw.Append($"{ServerClasses?.Length ?? ClassCount} server classes{(CreateOnClient ? "" : ":")}");
			if (!CreateOnClient && ServerClasses != null) {
				pw.FutureIndent++;
				foreach (ServerClass serverClass in ServerClasses) {
					pw.AppendLine();
					serverClass.PrettyWrite(pw);
				}
				pw.FutureIndent--;
			}
		}
	}


	// DataTableName <-> ClassName lookup, I use this as an identifier in data tables and ent parsing
	public class ServerClass : DemoComponent, IEquatable<ServerClass> {

		private readonly SvcClassInfo? _classInfoRef; // needed to get the length of the array for toString()
		public int DataTableId; // this references the nth data table this class refers to
		public string ClassName;
		public string DataTableName; // this is the name of the data table this class refers to

		public ServerClass(SourceDemo? demoRef, SvcClassInfo? classInfoRef) : base(demoRef) {
			_classInfoRef = classInfoRef;
		}


		protected override void Parse(ref BitStreamReader bsr) {
			DataTableId = _classInfoRef == null
				? bsr.ReadUShort()
				: (int)bsr.ReadUInt(GameState.DataTablesManager.ServerClassBits);
			ClassName = bsr.ReadNullTerminatedString();
			DataTableName = bsr.ReadNullTerminatedString();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"[{DataTableId}] {ClassName} ({DataTableName})");
		}

		// I don't think I'll need these hashcode methods if the the ID always matches the index

		public bool Equals(ServerClass? other) {
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return DataTableId == other.DataTableId
				   && ClassName == other.ClassName
				   && DataTableName == other.DataTableName;
		}


		public override bool Equals(object? obj) {
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj.GetType() == GetType() && Equals((ServerClass)obj);
		}


		[SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
		public override int GetHashCode() {
			return HashCode.Combine(DataTableId, ClassName, DataTableName);
		}


		public static bool operator ==(ServerClass left, ServerClass right) {
			return Equals(left, right);
		}


		public static bool operator !=(ServerClass left, ServerClass right) {
			return !Equals(left, right);
		}
	}
}
