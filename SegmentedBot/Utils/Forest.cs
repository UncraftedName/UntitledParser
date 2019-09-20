using System;
using System.Collections.Generic;

namespace SegmentedBot.Utils {
	
	public class Forest<T> {

		public readonly List<Forest<T>> Forests;


		public Forest() {
			Forests = new List<Forest<T>>();
		}
		
		
		private Forest(List<Forest<T>> forests) {
			Forests = forests;
		}


		public Forest<T> GetNMostRecentNodes(int n) {
			throw new NotImplementedException();
		}


		public override string ToString() {
			throw new NotImplementedException();
		}


		public static Forest<T2> FromString<T2>(string from) {
			throw new NotImplementedException();
		}
	}
}