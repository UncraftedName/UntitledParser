using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Abstract {

	/// <summary>
	/// Represents a well-defined 'chunk' of data in a demo. Optionally, this may come from a compressed stream, in
	/// which case the stream must be decompressed and stored in lookup table which can be accessed after parsing via
	/// the reader.
	/// </summary>
	public abstract class DemoComponent : PrettyClass {

		private int _absoluteBitStart;
		private int _absoluteBitEnd;
		public virtual BitStreamReader Reader =>
			_decompressedIndex.HasValue
				? new BitStreamReader(GameState.DecompressedLookup[_decompressedIndex.Value], _absoluteBitEnd - _absoluteBitStart, _absoluteBitStart)
				: new BitStreamReader(DemoRef!.Reader.Data, _absoluteBitEnd - _absoluteBitStart, _absoluteBitStart);

		// if set, this is the index to the decompressed data in the lookup table for accessing the reader later
		private readonly int? _decompressedIndex;

		internal readonly SourceDemo? DemoRef;
		protected DemoInfo DemoInfo => DemoRef.DemoInfo;
		protected GameState.GameState GameState => DemoRef.State;
		public virtual bool MayContainData => true; // used in ToString() calls


		protected DemoComponent(SourceDemo? demoRef, int? decompressedIndex = null) {
			DemoRef = demoRef;
			_decompressedIndex = decompressedIndex;
		}


		protected abstract void Parse(ref BitStreamReader bsr);


		public void ParseStream(ref BitStreamReader bsr) {
			_absoluteBitStart = bsr.AbsoluteBitIndex;
			_absoluteBitEnd = bsr.AbsoluteBitIndex + bsr.BitLength;
			Parse(ref bsr);
			_absoluteBitEnd = bsr.AbsoluteBitIndex;
		}


		public int ParseStream(BitStreamReader bsr) {
			_absoluteBitStart = bsr.AbsoluteBitIndex;
			_absoluteBitEnd = bsr.AbsoluteBitIndex + bsr.BitLength;
			Parse(ref bsr);
			return bsr.HasOverflowed ? -1 : bsr.BitsRemaining;
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"Not Implemented - {GetType().FullName}");
		}
	}
}
