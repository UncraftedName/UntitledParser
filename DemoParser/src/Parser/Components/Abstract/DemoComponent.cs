using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Abstract {
	
	/// <summary>
	/// Represents a well-defined 'chunk' of data in the demo.
	/// </summary>
	public abstract class DemoComponent : Appendable {
		
		internal readonly SourceDemo DemoRef; // if null, this IS the SourceDemo class
		protected DemoSettings DemoSettings => DemoRef.DemoSettings;
		private readonly BitStreamReader _reader;
		public BitStreamReader Reader => _reader.FromBeginning();
		public virtual bool MayContainData => true; // used in ToString() calls
		// todo remove reader, just keep absolute offset instead 
		// todo implemented bits remaining after parsed method. suggestion: setEnd() makes this 0, markEnd() sets to however many left
		
		
		protected DemoComponent(SourceDemo demoRef, BitStreamReader reader) {
			DemoRef = demoRef;
			_reader = reader?.SubStream(); // makes current bit the beginning, todo get rid of this?
		}


		// this can be used for error checking to set a limit on how many bits can be read
		// also can be used for a lazy implementation of WriteToStreamWriter() since you can just get the entire stream of this component that way
		protected void SetLocalStreamEnd(BitStreamReader bsr) {
			_reader.BitLength = bsr.AbsoluteBitIndex - _reader.Start;
		}


		internal abstract void ParseStream(BitStreamReader bsr);


		internal int ParseOwnStream() {
			BitStreamReader r = Reader;
			ParseStream(r);
			return r.BitsRemaining;
		}


		internal abstract void WriteToStreamWriter(BitStreamWriter bsw);


		public override void AppendToWriter(IndentedWriter iw) {
			iw.Append($"Not Implemented - {GetType().FullName}");
		}
	}
}