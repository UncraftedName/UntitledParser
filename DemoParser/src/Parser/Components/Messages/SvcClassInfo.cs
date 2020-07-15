#nullable enable
using System;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.HelperClasses.EntityStuff;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	public class SvcClassInfo : DemoMessage {

		public bool CreateOnClient;
		public ServerClass[]? ServerClasses;
		public ushort ClassCount;
		
		
		public SvcClassInfo(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			ClassCount = bsr.ReadUShort();
			CreateOnClient = bsr.ReadBool();
			if (!CreateOnClient) {
				
				// if this ever gets used then it should update the C_tables
				string s = $"I haven't implemented {GetType().Name} to update the C_string tables.";
				DemoRef.LogError(s);
				Debug.WriteLine(s);
				
				ServerClasses = new ServerClass[ClassCount];
				for (int i = 0; i < ServerClasses.Length; i++) {
					ServerClasses[i] = new ServerClass(DemoRef, bsr, this);
					ServerClasses[i].ParseStream(bsr);
					// this is an assumption I make in the structure of all the entity stuff, very critical
					if (i != ServerClasses[i].DataTableId)
						throw new ConstraintException("server class ID does not match it's index in the list");
				}
			}
			SetLocalStreamEnd(bsr);
			
			if (DemoRef.CBaseLines == null) // init baselines here if still null
				DemoRef.CBaseLines = new CurBaseLines(ClassCount, DemoRef);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"create on client: {CreateOnClient}");
			iw.Append($"{ServerClasses?.Length ?? ClassCount} server classes{(CreateOnClient ? "" : ":")}");
			if (!CreateOnClient && ServerClasses != null) {
				iw.FutureIndent++;
				foreach (ServerClass serverClass in ServerClasses) {
					iw.AppendLine();
					serverClass.AppendToWriter(iw);
				}
				iw.FutureIndent--;
			}
		}
	}
	
	
	// a very important part of the demo - corresponds to an entity class. used extensively in data tables and ent parsing
	public class ServerClass : DemoComponent, IEquatable<ServerClass> {

		private readonly SvcClassInfo? _classInfoRef; // needed to get the length of the array for toString()
		public int DataTableId; // this references the nth data table this class refers to
		public string ClassName;
		public string DataTableName; // this is the name of the data table this class refers to


		public ServerClass(SourceDemo demoRef, BitStreamReader reader, SvcClassInfo? classInfoRef) 
			: base(demoRef, reader) 
		{
			_classInfoRef = classInfoRef;
		}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			DataTableId = _classInfoRef == null
				? bsr.ReadUShort()
				: (int)bsr.ReadBitsAsUInt(DemoRef.DataTableParser.ServerClassBits);
			ClassName = bsr.ReadNullTerminatedString();
			DataTableName = bsr.ReadNullTerminatedString();
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.Append($"[{DataTableId}] {ClassName} ({DataTableName})");
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