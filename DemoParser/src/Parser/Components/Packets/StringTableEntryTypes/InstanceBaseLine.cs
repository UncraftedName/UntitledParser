#nullable enable
// making the compiler happy
#pragma warning disable 8604, 8625

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.HelperClasses.EntityStuff;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets.StringTableEntryTypes {
	
	// this is the baseline in the string tables, each one only holds the baseline for a single server class
	public class InstanceBaseline : StringTableEntryData {

		private readonly string _entryName;
		private BitStreamReader _bsr;
		// might not get set until later
		private PropLookup? _propLookup;
		public ServerClass? ServerClassRef;
		public IReadOnlyList<(int propIndex, EntityProperty prop)> Properties;


		public InstanceBaseline(SourceDemo demoRef, BitStreamReader reader, string entryName,
			PropLookup? propLookup) : base(demoRef, reader) 
		{
			_entryName = entryName;
			_propLookup = propLookup;
		}
		
		 // if we're parsing this before the data tables, just leave it for now
		internal override void ParseStream(BitStreamReader bsr) {
			_bsr = bsr;
			if (_propLookup != null)
				ParseBaseLineData(_propLookup);
		}

		
		// called once 
		internal void ParseBaseLineData(PropLookup propLookup) {
			_propLookup = propLookup;
			int id = int.Parse(_entryName);
			ServerClassRef = _propLookup[id].serverClass;

			// I assume in critical parts of the ent code that the server class ID matches the index it's on,
			// this is where I actually verify this.
			Debug.Assert(ReferenceEquals(ServerClassRef, _propLookup.Single(tuple => tuple.serverClass.DataTableId == id).serverClass),
				"the server classes must be searched to match ID; cannot use ID as index");
			
			List<FlattenedProp> fProps = propLookup[id].flattenedProps;

			try {
				Properties = _bsr.ReadEntProps(fProps, DemoRef);
				// once we're done, update the C_baselines so I can actually use this for prop creation
				if (DemoSettings.ProcessEnts)
					DemoRef.CBaseLines?.UpdateBaseLine(ServerClassRef, Properties, fProps.Count);
			} catch (Exception e) {
				DemoRef.LogError($"error while parsing baseline for class {ServerClassRef.ClassName}: {e.Message}");
			}
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			if (ServerClassRef != null) {
				iw.AppendLine($"class: {ServerClassRef.ClassName} ({ServerClassRef.DataTableName})");
				iw.Append("props:");
				iw.FutureIndent++;
				if (Properties != null) {
					foreach ((int i, EntityProperty prop) in Properties) {
						iw.AppendLine();
						iw.Append($"({i}) ");
						prop.AppendToWriter(iw);
					}
				} else {
					iw.Append("\nerror during parsing");
				}
				iw.FutureIndent--;
			}
		}
	}
}
