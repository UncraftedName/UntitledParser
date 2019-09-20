using System;

namespace SegmentedBot.Utils {
	
	public abstract class UniqueTree<T> {

		public abstract bool Add(T item, T previous);
		

		public abstract bool Remove(T item);
		

		public static UniqueTree<T2> FromString<T2>(string from) {
			throw new NotImplementedException();
		}
	}
}