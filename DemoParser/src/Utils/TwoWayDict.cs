using System.Collections.Generic;

namespace DemoParser.Utils {
	
	// i'm not sure if the this is correctly set up, it definitely doesn't have the full functionality of a full two way dict tho
	public class TwoWayDict<T1, T2> : Dictionary<T1, T2> {
		
		private readonly Dictionary<T2, T1> _reverse;
		

		public TwoWayDict() {
			_reverse = new Dictionary<T2, T1>();
		}
		

		public new void Add(T1 t1, T2 t2) {
			base.Add(t1, t2);
			_reverse.Add(t2, t1);
		}


		public new T2 this[T1 t] {
			get => base[t];
			set {
				base[t] = value;
				_reverse[value] = t;
			}
		}


		public T1 this[T2 t] {
			get => _reverse[t];
			set {
				_reverse[t] = value;
				base[value] = t;
			}
		}


		public T2 GetValueOrDefault(T1 key, T2 defaultValue) {
			return TryGetValue(key, out T2 value) ? value : defaultValue;
		}


		public T1 GetValueOrDefault(T2 key, T1 defaultValue) {
			return _reverse.TryGetValue(key, out T1 value) ? value : defaultValue;
		}
	}
}