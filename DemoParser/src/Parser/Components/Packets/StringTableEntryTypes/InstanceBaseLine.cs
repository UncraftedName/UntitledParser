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
using DemoParser.Parser.HelperClasses.GameState;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets.StringTableEntryTypes {

	// this is the baseline in the string tables, each one only holds the baseline for a single server class
	public class InstanceBaseline : StringTableEntryData {

		private readonly string _entryName;
		private PropLookup? _propLookup; // save if we copy this entry (in case of spliced demos or something idk)
		public ServerClass? ServerClassRef;
		public IReadOnlyList<(int propIndex, EntityProperty prop)>? Properties;


		public InstanceBaseline(SourceDemo? demoRef, int? decompressedIndex, string entryName)
			: base(demoRef, decompressedIndex)
		{
			_entryName = entryName;
		}


		internal override StringTableEntryData CreateCopy() {
			return new InstanceBaseline(DemoRef, DecompressedIndex, _entryName) {ServerClassRef = ServerClassRef, Properties = Properties};
		}


		protected override void Parse(ref BitStreamReader bsr) {
			_propLookup ??= DemoRef.DataTableParser?.FlattenedProps;
			if (_propLookup == null) {
				// we don't have data tables yet, come back later...
				bsr.SkipToEnd();
				return;
			}
			// we should only get to here once
			int id = int.Parse(_entryName);
			ServerClassRef = _propLookup[id].serverClass;

			// I assume in critical parts of the ent code that the server class ID matches the index it's on,
			// this is where I actually verify this.
			Debug.Assert(ReferenceEquals(ServerClassRef, _propLookup.Single(tuple => tuple.serverClass.DataTableId == id).serverClass),
				"the server classes must be searched to match ID; cannot use ID as index");

			List<FlattenedProp> fProps = _propLookup[id].flattenedProps;

			try {
				Properties = bsr.ReadEntProps(fProps, DemoRef);
				// once we're done, update the Cur baselines so I can actually use this for prop creation
				if ((DemoRef.DemoParseResult & DemoParseResult.EntParsingEnabled) != 0)
					DemoRef.EntBaseLines?.UpdateBaseLine(ServerClassRef, Properties!, fProps.Count);
			} catch (Exception e) {
				DemoRef.LogError($"error while parsing baseline for class {ServerClassRef.ClassName}: {e.Message}");
			}
			if (bsr.BitsRemaining < 8) // suppress warnings
				bsr.SkipToEnd();
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			if (ServerClassRef != null) {
				pw.AppendLine($"class: {ServerClassRef.ClassName} ({ServerClassRef.DataTableName})");
				pw.Append("props:");
				pw.FutureIndent++;
				if (Properties != null) {
					foreach ((int i, EntityProperty prop) in Properties) {
						pw.AppendLine();
						pw.Append($"({i}) ");
						prop.PrettyWrite(pw);
					}
				} else {
					pw.Append("\nerror during parsing");
				}
				pw.FutureIndent--;
			}
		}
	}
}
