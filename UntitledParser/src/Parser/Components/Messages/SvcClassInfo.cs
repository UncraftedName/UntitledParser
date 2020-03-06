#nullable enable
using System.Diagnostics;
using UntitledParser.Parser.Components.Abstract;
using UntitledParser.Utils;

namespace UntitledParser.Parser.Components.Messages {
	
	public class SvcClassInfo : DemoMessage {

		public bool CreateOnClient;
		public ServerClass[] ServerClasses;
		private ushort _classCount;
		
		
		public SvcClassInfo(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			_classCount = bsr.ReadUShort();
			CreateOnClient = bsr.ReadBool();
			if (!CreateOnClient) {
				
				// if this ever gets used then it should update the C_tables
				string s = $"I haven't implemented {GetType().Name} to update the C_tables.";
				DemoRef.AddError(s);
				Debug.WriteLine(s);
				
				ServerClasses = new ServerClass[_classCount];
				for (int i = 0; i < ServerClasses.Length; i++) {
					ServerClasses[i] = new ServerClass(DemoRef, bsr, this);
					ServerClasses[i].ParseStream(bsr);
				}
			}
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw.Append($"{ServerClasses?.Length ?? _classCount} server classes{(CreateOnClient ? "\n" : ":")}");
			if (!CreateOnClient) {
				iw.AddIndent();
				foreach (ServerClass serverClass in ServerClasses) {
					iw.AppendLine();
					serverClass.AppendToWriter(iw);
				}
				iw.SubIndent();
			}
			iw.Append($"create on client: {CreateOnClient}");
		}
	}
	
	
	public class ServerClass : DemoComponent {

		private readonly SvcClassInfo? _classInfoRef; // needed to get the length of the array
		public uint DataTableID; // this references the nth data table this class refers to
		public string ClassName;
		public string DataTableName; // this is the name of the data table this class refers to
		
		
		public ServerClass(SourceDemo demoRef, BitStreamReader reader, SvcClassInfo classInfoRef = null) : base(demoRef, reader) {
			_classInfoRef = classInfoRef;
		}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			DataTableID = _classInfoRef == null
				? bsr.ReadUShort()
				: bsr.ReadBitsAsUInt(BitUtils.HighestBitIndex((uint)_classInfoRef.ServerClasses.Length) + 1);
			ClassName = bsr.ReadNullTerminatedString();
			DataTableName = bsr.ReadNullTerminatedString();
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw += $"[{DataTableID}] {ClassName} ({DataTableName})";
		}
	}
}