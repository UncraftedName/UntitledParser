#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace DemoParser.Utils {
	
	// todo option to write to file immediately 
	public class IndentedWriter {

		private readonly List<string> _lines;
		private readonly List<int> _indentCount;
		private int _maxIndent;
		
		private int _futureIndent;
		public int FutureIndent {
			get => _futureIndent;
			set {
				_maxIndent = Math.Max(_maxIndent, _futureIndent);
				_futureIndent = Math.Max(value, 0);
			}
		}
		public int LastLineLength => _lines[^1].Length;


		public IndentedWriter() {
			_lines = new List<string> {""};
			_indentCount = new List<int>{0};
			FutureIndent = 0;
		}


		public void Append(Appendable appendable) {
			appendable.AppendToWriter(this);
		}


		public void Append(string s) {
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


		public void AppendLine(string s) {
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


		public void AppendWriter(IndentedWriter writer) {
			_lines.AddRange(writer._lines);
			_indentCount.AddRange(writer._indentCount);
		}


		public void PadLastLine(int count, char c) {
			_lines[^1] = _lines[^1].PadRight(count, c);
		}


		public override string ToString() {
			return ToString("\t");
		}


		public string ToString(string indentStr) {
			Debug.Assert(_indentCount.TrueForAll(i => i >= 0), "indent count is negative");
			
			// I have literally no reason to optimize this, I was just curious how to use this stuff. (but it is pretty fast now :p)
			
			// calculate the total length of the string
			int outLen = (_lines.Count - 1) * Environment.NewLine.Length;
			int indentLen = indentStr.Length;
			for (int i = 0; i < _lines.Count; i++) {
				outLen += _lines[i].Length;
				outLen += _indentCount[i] * indentLen;
			}

			Span<char> buf = new char[outLen].AsSpan();
			ReadOnlySpan<char> indent = new ReadOnlySpan<char>(string.Concat(Enumerable.Repeat(indentStr, _maxIndent)).ToCharArray());
			ReadOnlySpan<char> newLine = Environment.NewLine.AsSpan();

			int index = 0; // index in entire string
			for (int i = 0; i < _lines.Count; i++) {
				// copy indent
				indent.Slice(0, _indentCount[i]).CopyTo(buf.Slice(index));
				index += _indentCount[i] * indentStr.Length;
				// copy line
				_lines[i].AsSpan().CopyTo(buf.Slice(index));
				index += _lines[i].Length;
				// copy new line (unless on last line)
				if (i != _lines.Count - 1) {
					newLine.CopyTo(buf.Slice(index));
					index += newLine.Length;
				}
			}
			return buf.ToString();
		}


		// close the writer yourself
		public void WriteLines(TextWriter textWriter, string indentStr) {
			for (int i = 0; i < _lines.Count; i++) {
				for (int j = 0; j < _indentCount[i]; j++) 
					textWriter.Write(indentStr);
				textWriter.WriteLine(_lines[i]);
			}
			textWriter.Flush();
		}
	}


	// This lets me see the toString() representation by just using the append function that I implemented for every
	// demo component anyway.
	public abstract class Appendable {
		
		public abstract void AppendToWriter(IndentedWriter iw);
		
		public new virtual string ToString() {
			IndentedWriter iw = new IndentedWriter();
			iw.Append(this);
			return iw.ToString();
		}
	}
}
