using System;
using System.IO;
using System.Text;
using DemoParser.Utils;
using NUnit.Framework;

namespace Tests {

	public class PrettyWriterTests {

		private const string Is = " [INDENT] ";          // indent string
		private static string Nl => Environment.NewLine; // current behavior is to convert '\n' to OS-specific newline

		public static IPrettyWriter[] TestWriters => new IPrettyWriter[] {
			new PrettyToStringWriter(indentStr: Is),
			new PrettyStreamWriter(new MemoryStream(), Encoding.ASCII, indentStr: Is) // using ascii to ignore utf-8 BOM
		};


		private static void CheckWriterContents(IPrettyWriter pw, string expected) {
			switch (pw) {
				case PrettyToStringWriter ptsw:
					Assert.AreEqual(expected, ptsw.Contents, $"incorrect string in {typeof(PrettyToStringWriter)}");
					break;
				case PrettyStreamWriter psw:
					psw.Flush();
					MemoryStream ms = (MemoryStream)psw.BaseStream;
					Assert.AreEqual(expected, Encoding.ASCII.GetString(ms.ToArray()), $"incorrect string in {typeof(PrettyStreamWriter)}");
					break;
				default:
					Assert.Fail($"unknown {nameof(IPrettyWriter)} type: \"{pw.GetType()}\"");
					break;
			}
			pw.Dispose();
		}


		[TestCaseSource(nameof(TestWriters)), Parallelizable(ParallelScope.Self)]
		public void WriteManualNewLines(IPrettyWriter pw) {
			const string line1 = "maybe";
			const string line2 = "tas_strafe_button presidenttrump";
			const string line3 = "is the solution";
			pw.Append(line1);
			pw.AppendLine();
			pw.Append(line2);
			pw.AppendLine();
			pw.Append(line3);
			CheckWriterContents(pw, $"{line1}{Nl}{line2}{Nl}{line3}");
		}


		[TestCaseSource(nameof(TestWriters)), Parallelizable(ParallelScope.Self)]
		public void WriteAutoNewLines(IPrettyWriter pw) {
			const string line1 = "Can someone make a coop peeing simulator?";
			const string line2 = "Imanex - 2021";
			pw.Append($"{line1}\n{line2}");
			CheckWriterContents(pw, $"{line1}{Nl}{line2}");
		}


		[TestCaseSource(nameof(TestWriters)), Parallelizable(ParallelScope.Self)]
		public void WriteManualNewLinesWithIndent(IPrettyWriter pw) {
			const string line1 = "This demo parser";
			const string line2 = "it brought to you in part by";
			const string line3 = "RAID Shadow Legends";
			pw.FutureIndent = 2;
			pw.AppendLine();
			pw.Append(line1);
			pw.AppendLine();
			pw.Append(line2);
			pw.FutureIndent--;
			pw.AppendLine();
			pw.Append(line3);
			pw.FutureIndent--;
			pw.AppendLine();
			CheckWriterContents(pw, $"{Nl}{Is}{Is}{line1}{Nl}{Is}{Is}{line2}{Nl}{Is}{line3}{Nl}");
		}


		[TestCaseSource(nameof(TestWriters)), Parallelizable(ParallelScope.Self)]
		public void WriteAutoNewLinesWithIndent(IPrettyWriter pw) {
			const string line1 = "eeeee";
			const string line2 = "zzzzz";
			const string line3 = "ooooo";
			pw.FutureIndent++;
			pw.AppendLine($"{line1}\n{line2}");
			pw.FutureIndent++;
			pw.Append($"\n\n{line3}");
			pw.FutureIndent = 5;
			pw.Append("\n");
			CheckWriterContents(pw, $"{line1}{Nl}{Is}{line2}{Nl}{Is}{Is}{Nl}{Is}{Is}{Nl}{Is}{Is}{line3}{Nl}");
		}
	}
}
