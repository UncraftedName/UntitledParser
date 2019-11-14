using System;
using System.Diagnostics;
using UncraftedDemoParser.Parser.Misc;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.Parser.Components.Abstract {
	
	// used to describe the various components of a demo,
	// constructed with a byte array, and contains fields that can be updated and rewritten to a new byte array
	public abstract class DemoComponent {

		public byte[] Bytes {get;protected set;}
		public SourceDemo DemoRef; // literally only needed for stop packet, otherwise source demo settings would be passed
		// maybe add int? tick 

		protected DemoComponent(byte[] data, SourceDemo demoRef) {
			Bytes = data;
			DemoRef = demoRef;
		}

		
		protected DemoComponent(BitFieldReader bfr, SourceDemo demoRef) {
			DemoRef = demoRef;
			ParseBytes(bfr);
		}


		// override this or (override ParseBytes(bfr))
		protected virtual void ParseBytes() {}


		protected virtual void ParseBytes(BitFieldReader bfr) {}


		// call this to populate the fields from the byte array
		// virtual to allow packets call
		public DemoComponent TryParse(int? tick = null) {
			try {
				ParseBytes();
			} catch (FailedToParseException) {
				Debug.WriteLine($"Demo failed to parse on tick {tick}");
				throw;
			} catch (Exception e) {
				Debug.WriteLine(e.ToString());
				Debug.WriteLine(e.StackTrace);
				throw new FailedToParseException(this, tick);
			}
			return this;
		}


		// If any fields of a packet are ever updated, call this to get the modified byte array.
		// Behavior is undefined if fields are updated and this isn't called.
		public virtual void UpdateBytes() {
			Debug.WriteLine($"not sure how to update {GetType().Name}");
		}


		public override string ToString() {
			return "Not implemented";
		}
	}
}