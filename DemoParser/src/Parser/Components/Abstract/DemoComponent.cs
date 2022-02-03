using System.Diagnostics;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Abstract {

	/// <summary>
	/// Represents a well-defined 'chunk' of data in the demo.
	/// </summary>
	public abstract class DemoComponent : PrettyClass {

		protected int AbsoluteBitStart;
		protected int AbsoluteBitEnd;
		public virtual BitStreamReader Reader =>
			DemoRef!.ReaderFromOffset(AbsoluteBitStart, AbsoluteBitEnd - AbsoluteBitStart);

		internal readonly SourceDemo? DemoRef;
		protected DemoInfo DemoInfo => DemoRef.DemoInfo;
		public virtual bool MayContainData => true; // used in ToString() calls


		protected DemoComponent(SourceDemo? demoRef) {
			DemoRef = demoRef;
		}


		protected abstract void Parse(ref BitStreamReader bsr);


		public void ParseStream(ref BitStreamReader bsr) {
			AbsoluteBitStart = bsr.AbsoluteBitIndex;
			AbsoluteBitEnd = bsr.AbsoluteBitIndex + bsr.BitLength;
			Parse(ref bsr);
			AbsoluteBitEnd = bsr.AbsoluteBitIndex;
		}


		public int ParseStream(BitStreamReader bsr) {
			AbsoluteBitStart = bsr.AbsoluteBitIndex;
			AbsoluteBitEnd = bsr.AbsoluteBitIndex + bsr.BitLength;
			Parse(ref bsr);
			if (bsr.BitsRemaining > 0)
				Debug.WriteLine($"{GetType().Name} didn't finish reading all bits! {bsr.BitsRemaining} left.");
			return bsr.BitsRemaining;
		}


		internal abstract void WriteToStreamWriter(BitStreamWriter bsw);


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"Not Implemented - {GetType().FullName}");
		}
	}
}
