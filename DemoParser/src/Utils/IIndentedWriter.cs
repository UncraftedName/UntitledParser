#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DemoParser.Utils {

	public interface IIndentedWriter : IDisposable {
		int FutureIndent {get;set;}
		int LastLineLength {get;}
		void Append(string s);
		void AppendLine(string s);
		void AppendLine();
		void AppendFormat(string format, params object?[] args);
		void PadLastLine(int count, char c);
	}
	


	public class IndentedToStringWriter : IIndentedWriter {
		
		private readonly List<string> _lines;
		private readonly List<int> _indentCount;
		private int _maxIndent;
		private int _futureIndent;
		
		public int FutureIndent {
			get => _futureIndent;
			set {
				_futureIndent = Math.Max(value, 0);
				_maxIndent = Math.Max(_maxIndent, _futureIndent);
			}
		}
		public int LastLineLength => _lines[^1].Length;
		
		
		public IndentedToStringWriter() {
			_lines = new List<string> {""};
			_indentCount = new List<int>{0};
		}


		public void Append(string s) {
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


		public void AppendFormat(string format, params object?[] args) => Append(string.Format(format, args));


		public void PadLastLine(int count, char c) => _lines[^1] = _lines[^1].PadRight(count, c);


		public void Dispose() {}


		public override string ToString() => ToString("\t");


		public string ToString(string indentStr) {
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
	}
	
	
	
	public class IndentedTextWriter : StreamWriter, IIndentedWriter {
		
		private int _futureIndent;

		public int FutureIndent {
			get => _futureIndent;
			set => _futureIndent = Math.Max(value, 0);
		}
		public int LastLineLength {get;private set;}
		private char[] _tmpBuf;
		private bool _pendingNewLineIndent;
		private readonly string _indentStr;


		public IndentedTextWriter(Stream stream, int bufferSize = 1024, bool leaveOpen = false, string indentStr = "\t")
			: this(stream, Encoding.UTF8, bufferSize, leaveOpen, indentStr) {}


		public IndentedTextWriter(
			Stream stream,
			Encoding encoding,
			int bufferSize = 1024,
			bool leaveOpen = false,
			string indentStr = "\t")
			: base(stream, encoding, bufferSize, leaveOpen)
		{
			_indentStr = indentStr;
			_tmpBuf = Array.Empty<char>();
		}


		public void Append(string s) {
			if (s.Length == 0)
				return;
			CheckForIndent();
			if (_futureIndent == 0) {
				Write(s);
				int nlIndex = s.LastIndexOf('\n');
				if (nlIndex == -1)
					LastLineLength += s.Length;
				else
					LastLineLength = s.Length - nlIndex - 1;
			} else {
				int count;
				for (int i = 0; i < s.Length; i += count + 1) {
					int nlIndex = s.IndexOf('\n', i);
					if (i == 0) {
						if (nlIndex == -1) {
							Write(s);
							LastLineLength += s.Length;
							return;
						}
					} else {
						CheckForIndent();
					}
					count = (nlIndex == -1 ? s.Length : nlIndex) - i;
					if (count > _tmpBuf.Length)
						_tmpBuf = new char[count];
					s.CopyTo(i, _tmpBuf, 0, count);
					Write(_tmpBuf, 0, count);
					LastLineLength += count;
					if (nlIndex != -1)
						AppendLine();
				}
			}
		}


		public void AppendLine(string s) {
			Append(s);
			AppendLine();
		}


		public void AppendLine() {
			CheckForIndent();
			WriteLine();
			_pendingNewLineIndent = true;
			LastLineLength = 0;
		}


		public void AppendFormat(string format, params object?[] args) => Append(string.Format(format, args));


		public void PadLastLine(int count, char c) => Write(new string(c, Math.Max(0, count - LastLineLength)));


		private void CheckForIndent() {
			if (_pendingNewLineIndent)
				for (int i = 0; i < _futureIndent; i++)
					Write(_indentStr);
			_pendingNewLineIndent = false;
		}


		public new void Dispose() {
			Flush();
			base.Dispose();
			_tmpBuf = null!;
		}
	}
	
	
	
	public interface IAppendable {
		void AppendToWriter(IIndentedWriter iw);
	}


	// This lets me see the toString() representation by just using the append function that I implemented for every
	// demo component anyway.
	public abstract class AppendableClass : IAppendable {
		
		public abstract void AppendToWriter(IIndentedWriter iw);
		
		public new virtual string ToString() {
			return AppendHelper(this);
		}

		public static string AppendHelper(IAppendable ia) {
			IIndentedWriter iw = new IndentedToStringWriter();
			ia.AppendToWriter(iw);
			return iw.ToString();
		}
	}
}