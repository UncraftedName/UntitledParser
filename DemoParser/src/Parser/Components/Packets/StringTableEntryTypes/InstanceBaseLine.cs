#nullable enable
using System.Collections.Generic;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.EntityStuff;
using DemoParser.Parser.GameState;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;
using static DemoParser.Parser.DemoParseResult;

namespace DemoParser.Parser.Components.Packets.StringTableEntryTypes {

	// this is the baseline in the string tables, each one only holds the baseline for a single server class
	public class InstanceBaseline : StringTableEntryData {

		private readonly string _entryName;
		private PropLookup? _propLookup; // save if we copy this entry (in case of spliced demos or something idk)
		public ServerClass? ServerClassRef;
		public IReadOnlyList<(int propIndex, EntityProperty prop)>? Properties;


		public InstanceBaseline(SourceDemo? demoRef, string entryName, int? decompressedIndex = null)
			: base(demoRef, decompressedIndex)
		{
			_entryName = entryName;
		}


		protected override void Parse(ref BitStreamReader bsr) {
			if ((_propLookup ??= DemoRef.State.DataTableParser?.FlattenedProps) == null)
				return; // we don't have data tables yet, come back later...

			// we should only get to here once
			if (!int.TryParse(_entryName, out int i) || i < 0 || i >= _propLookup.Count) {
				DemoRef.LogError($"{GetType().Name}: could not convert string table entry name \"{_entryName}\" to a valid server class index");
				DemoRef.DemoParseResult |= EntParsingFailed;
				return;
			}

			List<FlattenedProp> fProps;
			(ServerClassRef, fProps) = _propLookup[i];

			// I can still usually parse the baselines even if entity parsing is disabled
			Properties = bsr.ReadEntProps(fProps, DemoRef);

			if (bsr.HasOverflowed) {
				DemoRef.LogError($"{GetType().Name}: failed to parse entity baselines for class ({ServerClassRef.ToString()})");
				DemoRef.DemoParseResult |= EntParsingFailed;
				Properties = null;
			}

			// once we're done, update the baselines so I can actually use this for prop creation
			if ((DemoRef.DemoParseResult & (EntParsingEnabled | EntParsingFailed)) == EntParsingEnabled)
				DemoRef.State.EntBaseLines?.UpdateBaseLine(ServerClassRef, Properties!, fProps.Count);
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			if (ServerClassRef! != null!) {
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
