#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DemoParser.Utils {

	// Allows writing in indented sections, so you can increase the indent and all future lines will start with spaces or whatever.
	// Any new characters will get converted to the OS-specific version (only \n is replaced, \r is still written).
	public interface IPrettyWriter : IDisposable {
		int FutureIndent {get;set;}
		int LastLineLength {get;}
		void Append(string s);
		void AppendLine(string s);
		void AppendLine();
		void AppendFormat(string format, params object?[] args);
		void PadLastLine(int count, char c);
	}



	public class PrettyToStringWriter : IPrettyWriter {

		private int _futureIndent;

		public int FutureIndent {
			get => _futureIndent;
			set => _futureIndent = Math.Max(value, 0);
		}
		private readonly StringBuilder _sb;
		public int LastLineLength {get;private set;}
		private bool _pendingNewLineIndent;
		private readonly string _indentStr;


		public PrettyToStringWriter(string indentStr = "\t") {
			_indentStr = indentStr;
			_sb = new StringBuilder();
		}


		public unsafe void Append(string s) {
			if (s.Length == 0)
				return;
			CheckForIndent();
			fixed (char* ptr = s) { // bypass bound checks in StringBuilder
				int idx = 0;
				int nIdx;
				while (idx < s.Length && (nIdx = s.IndexOf('\n', idx)) != -1) {
					CheckForIndent();
					_sb.Append(ptr + idx, nIdx - idx);
					idx = nIdx + 1;
					AppendLine();
				}
				if (idx < s.Length) {
					CheckForIndent();
					_sb.Append(ptr + idx, s.Length - idx);
					LastLineLength += s.Length - idx;
				}
			}
		}


		public void AppendLine(string s) {
			Append(s);
			AppendLine();
		}


		public void AppendLine() {
			CheckForIndent();
			_sb.AppendLine();
			_pendingNewLineIndent = true;
			LastLineLength = 0;
		}


		private void CheckForIndent() {
			if (_pendingNewLineIndent)
				for (int i = 0; i < FutureIndent; i++)
					_sb.Append(_indentStr);
			_pendingNewLineIndent = false;
		}


		public void AppendFormat(string format, params object?[] args) => _sb.AppendFormat(format, args);

		public void PadLastLine(int count, char c) => _sb.Append(c, count);

		public void Dispose() {}

		public string Contents => _sb.ToString(); // I want to keep the default ToString()
	}



	// note: you have to flush this manually; for some reason a flush to a textwriter wrapper of this might not work
	public class PrettyStreamWriter : StreamWriter, IPrettyWriter {

		private int _futureIndent;

		public int FutureIndent {
			get => _futureIndent;
			set => _futureIndent = Math.Max(value, 0);
		}
		public int LastLineLength {get;private set;}
		private char[] _tmpBuf;
		private bool _pendingNewLineIndent;
		private readonly string _indentStr;


		public PrettyStreamWriter(Stream stream, int bufferSize = 1024, bool leaveOpen = false, string indentStr = "\t")
			: this(stream, Encoding.UTF8, bufferSize, leaveOpen, indentStr) {}


		public PrettyStreamWriter(
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
				s.AsSpan(i, count).CopyTo(_tmpBuf);
				Write(_tmpBuf, 0, count);
				LastLineLength += count;
				if (nlIndex != -1)
					AppendLine();
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



	// only use this in case of structs or possibly multiple inheritance, just override ToString() like below
	public interface IPretty {
		void PrettyWrite(IPrettyWriter pw);
	}


	// This lets me see the toString() representation by just using the append function that I implemented for every
	// demo component anyway. Use this over the interface version for convenience.
	[DebuggerDisplay("{DebuggerString}")]
	public abstract class PrettyClass : IPretty {

		// writing out the full pretty string can make the debugger crash
		private string DebuggerString => GetType().ToString();

		public abstract void PrettyWrite(IPrettyWriter pw);

		// a struct that inherits from IPretty may also override ToString() in the same way
		public new virtual string ToString() {
			return PrettyToStringHelper(this);
		}

		public static string PrettyToStringHelper(IPretty ip) {
			var psw = new PrettyToStringWriter();
			ip.PrettyWrite(psw);
			return psw.Contents;
		}
	}
}
