#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace UntitledParser.Utils {
	
	public class IndentedWriter {

		private readonly List<string> _lines;
		private readonly List<int> _indentCount;
		private int _maxIndent;
		
		private int _futureIndent;
		public int FutureIndent {
			get => _futureIndent;
			set {
				if (_futureIndent > _maxIndent)
					_maxIndent = _futureIndent;
				_futureIndent = value;
			}
		}
		
		public string IndentStr = "\t"; // dabs or spaces or whatever your heart desires


		public IndentedWriter(int initialIndentCount = 0, int initialLineCapacity = 5) {
			_lines = new List<string>(initialLineCapacity) {""};
			_indentCount = new List<int>(initialLineCapacity) {initialIndentCount};
			FutureIndent = initialIndentCount;
		}


		public IndentedWriter([NotNull]string s, int tabCount = 0, int initialLineCapacity = 5) :
			this(tabCount, initialLineCapacity) {
			Append(s);
		}


		public void AddIndent() => FutureIndent++;


		public void SubIndent() {
			if (FutureIndent != 0)
				FutureIndent--;
		}


		public void Append([NotNull]string s) {
			// string[] newLines = s.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.None);
			string[] newLines = s.Split('\n');
			for (int i = 0; i < newLines.Length; i++) {
				if (i == 0) {
					_lines[^1] += newLines[0];
				} else {
					_lines.Add(newLines[i]);
					_indentCount.Add(FutureIndent);
				}
			}
		}


		public void AppendLine([NotNull]string s) {
			Append(s);
			AppendLine();
		}


		public void AppendLine() {
			_lines.Add("");
			_indentCount.Add(FutureIndent);
		}


		public void AppendFormat(string format, params object?[] args) {
			Append(string.Format(format, args));
		}


		public void AddWriter(IndentedWriter writer) {
			_lines.AddRange(writer._lines);
			_indentCount.AddRange(writer._indentCount);
		}


		public void PadLastLine(int count, char c) {
			_lines[^1] = _lines[^1].PadRight(count, c);
		}


		// these are mutable operators, just syntactic convenience to be able to use +=
		public static IndentedWriter operator +(IndentedWriter writer1, IndentedWriter writer2) {
			writer1.AddWriter(writer2);
			return writer1;
		}


		// mutable
		public static IndentedWriter operator +(IndentedWriter writer, [NotNull]string str) {
			writer.Append(str);
			return writer;
		}


		public override string ToString() {
			Debug.Assert(_indentCount.TrueForAll(i => i >= 0), "indent count is negative");
			
			// I have literally no reason to optimize this, I was just curious how to use this stuff. (but it is pretty fast now :p)
			
			// calculate the total length of the string
			int outLen = _lines.Count * Environment.NewLine.Length;
			int indentLen = IndentStr.Length;
			for (int i = 0; i < _lines.Count; i++) {
				outLen += _lines[i].Length;
				outLen += _indentCount[i] * indentLen;
			}
			
			return string.Create<object>(outLen, null, (chars, _) => {
				ReadOnlySpan<char> indent = new ReadOnlySpan<char>(string.Concat(Enumerable.Repeat(IndentStr, _maxIndent)).ToCharArray());
				ReadOnlySpan<char> newLine = Environment.NewLine.AsSpan();

				int index = 0; // index in entire string
				
				for (int i = 0; i < _lines.Count; i++) {
					// copy indent
					indent.Slice(0, _indentCount[i]).CopyTo(chars.Slice(index));
					index += _indentCount[i] * IndentStr.Length;
					// copy line
					_lines[i].AsSpan().CopyTo(chars.Slice(index));
					index += _lines[i].Length;
					// copy new line
					newLine.CopyTo(chars.Slice(index));
					index += newLine.Length;
				}
			});
		}
	}
}