using UntitledParser.Utils;

namespace UntitledParser.Parser.Components.Abstract {
	
	public abstract class DemoComponent {

		public readonly SourceDemo DemoRef; // if null, this IS the SourceDemo class
		private BitStreamReader _reader;
		public BitStreamReader Reader => _reader.FromBeginning();
		public virtual bool MayContainData => true; // used for to string conversions
		
		
		protected DemoComponent(SourceDemo demoRef, BitStreamReader reader) {
			DemoRef = demoRef;
			_reader = reader.SubStream(); // makes current bit the beginning
		}


		// this can be used for error checking to set a limit on how many bits can be read
		// also can be used for a lazy implementation of WriteToStreamWriter() since you can just get the entire stream of this component that way
		protected void SetLocalStreamEnd(BitStreamReader bsr) {
			_reader = _reader.WithEndAt(bsr);
		}


		protected void SetLocalStreamEndAfterNBits(BitStreamReader bsr, int bitCount) {
			_reader = Reader.SubStream(bsr.CurrentBitIndex + bitCount);
		}


		protected void SetLocalStreamEndAfterNBytes(BitStreamReader bsr, int byteCount) =>
			SetLocalStreamEndAfterNBits(bsr, byteCount << 3);


		internal abstract void ParseStream(BitStreamReader bsr);


		internal int ParseOwnStream() {
			BitStreamReader r = Reader;
			ParseStream(r);
			return r.BitsRemaining;
		}


		internal abstract void WriteToStreamWriter(BitStreamWriter bsw);


		internal virtual void AppendToWriter(IndentedWriter iw) {
			iw += $"Not Implemented - {GetType().FullName}";
		}
		
		
		public override string ToString() {
			IndentedWriter writer = new IndentedWriter();
			AppendToWriter(writer);
			return writer.ToString();
		}
	}
}