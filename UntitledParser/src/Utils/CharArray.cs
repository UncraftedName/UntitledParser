using System;

namespace UntitledParser.Utils {
	
	public class CharArray {

		private readonly int _length;
		private string _str;
		
		public string Str {
			get => _str;
			set {
				if (value.Length > _length)
					throw new ArgumentException("the provided string must not be longer than the provided length", nameof(_length));
				_str = value;
			}
		}


		public CharArray(string str, int length) {
			_length = length;
			Str = str;
		}


		public CharArray(byte[] bytes) {
			_length = bytes.Length;
			_str = ParserTextUtils.ByteArrayAsString(bytes);
		}
		

		public byte[] AsByteArray() {
			return ParserTextUtils.StringAsByteArray(_str, _length);
		}


		public override string ToString() {
			return _str;
		}
	}
}