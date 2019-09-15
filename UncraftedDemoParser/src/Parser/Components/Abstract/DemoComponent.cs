using System;
using System.Diagnostics;
using UncraftedDemoParser.Parser.Misc;

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


		protected abstract void ParseBytes();

		
		// call this to populate the fields from the byte array
		public DemoComponent TryParse(int? tick = null) { // experimental, might remove
			try {
				ParseBytes();
			} catch (FailedToParseException) {
				throw;
			} catch (Exception e) {
				Debug.WriteLine(e.StackTrace);
				Debug.WriteLine(e.ToString());
				throw new FailedToParseException(this, tick);
			}
			return this;
		}


		// If any fields of a packet are ever updated, call this to get the modified byte array.
		// Behavior is undefined if fields are updated and this isn't called.
		public virtual void UpdateBytes() {
			Debug.WriteLine($"not sure how to update {this.GetType().Name}");
		}


		public override string ToString() {
			return "Not implemented";
		}
	}
}