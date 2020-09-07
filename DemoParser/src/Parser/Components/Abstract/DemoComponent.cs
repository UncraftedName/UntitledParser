using System.Diagnostics;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Abstract {
	
	/// <summary>
	/// Represents a well-defined 'chunk' of data in the demo.
	/// </summary>
	public abstract class DemoComponent : AppendableClass {
		
		private int _absoluteBitStart;
		private int _absoluteBitEnd;
		public virtual BitStreamReader Reader =>
			DemoRef!.ReaderFromOffset(_absoluteBitStart, _absoluteBitEnd - _absoluteBitStart);
		
		internal readonly SourceDemo? DemoRef;
		protected DemoSettings DemoSettings => DemoRef.DemoSettings;
		public virtual bool MayContainData => true; // used in ToString() calls
		
		
		protected DemoComponent(SourceDemo? demoRef) {
			DemoRef = demoRef;
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
			if (bsr.BitsRemaining > 0)
				Debug.WriteLine($"{GetType().Name} didn't finish reading all bits! {bsr.BitsRemaining} left.");
			return bsr.BitsRemaining;
		}


		internal abstract void WriteToStreamWriter(BitStreamWriter bsw);


		public override void AppendToWriter(IIndentedWriter iw) {
			iw.Append($"Not Implemented - {GetType().FullName}");
		}
	}
}