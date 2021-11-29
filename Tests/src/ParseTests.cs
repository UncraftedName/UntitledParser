using System.IO;
using System.Reflection;
using DemoParser.Parser;
using DemoParser.Utils;
using NUnit.Framework;

namespace Tests {
	
	public class ParseTests {
		
		public static readonly string ProjectDir =
			// bin/Debug/net461 -> ../../..
			Directory.GetParent(
				Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
				.Parent!.Parent!.FullName;

		private static readonly string DumpFolder = $"{ProjectDir}/sample demos/demo dump";
		

		[OneTimeSetUp]
		public void Init() {
			Directory.CreateDirectory(DumpFolder);
		}


		private static object[] _demoList = {
			new TestCaseData("hl2 oe.dem").SetName("Half life 2 Old Engine"),
			
			new TestCaseData("portal 1 leak.dem").SetName("Portal 1 (Leak)"),
			new TestCaseData("portal 1 3420.dem").SetName("Portal 1 (3420)"),
			new TestCaseData("portal 1 5135.dem").SetName("Portal 1 (5135)"),
			new TestCaseData("portal 1 5135 hltv client.dem").SetName("Portal 1 (5135 HLTV Client)"),
			new TestCaseData("portal 1 5135 hltv server.dem").SetName("Portal 1 (5135 HLTV Server)"),
			new TestCaseData("portal 1 5135 spliced.dem").SetName("Portal 1 (5135 Spliced)"),
			new TestCaseData("portal 1 steampipe.dem").SetName("Portal 1 (SteamPipe)"),
			
			new TestCaseData("portal 2 sp.dem").SetName("Portal 2 single player"),
			new TestCaseData("portal 2 coop.dem").SetName("Portal 2 co-op"),
			
			new TestCaseData("l4d1 37 v1005.dem").SetName("Left 4 Dead 1 (version 1.0.0.5)"),
			new TestCaseData("l4d1 1040.dem").SetName("Left 4 Dead 1 (version 1.0.4.0)"),
			new TestCaseData("l4d2 2000.dem").SetName("Left 4 Dead 2 (version 2.0.0.0)"),
			new TestCaseData("l4d2 2012.dem").SetName("Left 4 Dead 2 (version 2.0.1.2)"),
			new TestCaseData("l4d2 2027.dem").SetName("Left 4 Dead 2 (version 2.0.2.7)"),
			new TestCaseData("l4d2 2042 v2063.dem").SetName("Left 4 Dead 2 (version 2.0.6.3)"),
			new TestCaseData("l4d2 2042 v2075.dem").SetName("Left 4 Dead 2 (version 2.0.7.5)"),
			new TestCaseData("l4d2 2042 v2091.dem").SetName("Left 4 Dead 2 (version 2.0.9.1)"),
			new TestCaseData("l4d2 2100 v2220.dem").SetName("Left 4 Dead 2 (version 2.2.2.0)"),
		};
		
		
		[TestCaseSource(nameof(_demoList)), Parallelizable(ParallelScope.All)]
		public void ParseAndDumpDemo(string fileName) {
			SourceDemo demo = new SourceDemo($"{ProjectDir}/sample demos/{fileName}");
			demo.Parse();
			using PrettyStreamWriter psw = new PrettyStreamWriter(
				new FileStream($"{DumpFolder}/{demo.FileName![..^4]}.txt", FileMode.Create));
			demo.PrettyWrite(psw);
		}


		[TestCaseSource(nameof(_demoList)), Parallelizable(ParallelScope.All)]
		public void BasicTimeTest(string fileName) {
			SourceDemo demo = new SourceDemo($"{ProjectDir}/sample demos/{fileName}");
			demo.Parse();
			// all these demos should be a few seconds to a few minutes long
			Assert.Greater(demo.TickCount(false), 100);
			Assert.Less(demo.TickCount(false), 100000);
			Assert.Greater(demo.AdjustedTickCount(false), 100);
			Assert.Less(demo.AdjustedTickCount(false), 100000);
			Assert.Greater(demo.DemoInfo.TickInterval, 1.0/100);
			Assert.Less(demo.DemoInfo.TickInterval, 1.0/10);
		}
	}
}
