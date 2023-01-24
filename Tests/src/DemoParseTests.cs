using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DemoParser.Parser;
using NUnit.Framework;

namespace Tests {

	// To test demo structure and things of the like, inherit from this class and use GetDemo().
	public abstract class DemoParseTests {

		public static IReadOnlyList<TestCaseData> DemoList = new [] {
			new TestCaseData("hl2 oe.dem").SetName("Half life 2 Old Engine"),

			new TestCaseData("portal 1 leak.dem").SetName("Portal 1 (Leak)"),
			new TestCaseData("portal 1 3420.dem").SetName("Portal 1 (3420)"),
			new TestCaseData("portal 1 3740.dem").SetName("Portal 1 (3740)"),
			new TestCaseData("portal 1 5135 captions.dem").SetName("Portal 1 (5135) with captions"),
			new TestCaseData("portal 1 5135 hltv client.dem").SetName("Portal 1 (5135 HLTV Client)"),
			new TestCaseData("portal 1 5135 hltv server.dem").SetName("Portal 1 (5135 HLTV Server)"),
			new TestCaseData("portal 1 5135 spliced.dem").SetName("Portal 1 (5135 Spliced)"),
			new TestCaseData("portal 1 steampipe.dem").SetName("Portal 1 (SteamPipe)"),

			new TestCaseData("portal 2 sp.dem").SetName("Portal 2 single player"),
			new TestCaseData("portal 2 coop.dem").SetName("Portal 2 co-op"),

			new TestCaseData("l4d1 37 v1005.dem").SetName("Left 4 Dead 1 (version 1.0.0.5)"),
			new TestCaseData("l4d1 1040.dem").SetName("Left 4 Dead 1 (version 1.0.4.0)"),
			new TestCaseData("l4d1 1041.dem").SetName("Left 4 Dead 1 (version 1.0.4.1)"),
			new TestCaseData("l4d2 2000.dem").SetName("Left 4 Dead 2 (version 2.0.0.0)"),
			new TestCaseData("l4d2 2012.dem").SetName("Left 4 Dead 2 (version 2.0.1.2)"),
			new TestCaseData("l4d2 2027.dem").SetName("Left 4 Dead 2 (version 2.0.2.7)"),
			new TestCaseData("l4d2 2042 v2045.dem").SetName("Left 4 Dead 2 (version 2.0.4.5)"),
			new TestCaseData("l4d2 2042 v2063.dem").SetName("Left 4 Dead 2 (version 2.0.6.3)"),
			new TestCaseData("l4d2 2042 v2075.dem").SetName("Left 4 Dead 2 (version 2.0.7.5)"),
			new TestCaseData("l4d2 2042 v2091.dem").SetName("Left 4 Dead 2 (version 2.0.9.1)"),
			new TestCaseData("l4d2 2042 v2147.dem").SetName("Left 4 Dead 2 (version 2.1.4.7)"),
			new TestCaseData("l4d2 2100 v2203.dem").SetName("Left 4 Dead 2 (version 2.2.0.3)"),
			new TestCaseData("l4d2 2100 v2220.dem").SetName("Left 4 Dead 2 (version 2.2.2.0)"),
		};


		// only load from disk once (in case like hundreds of tests are added in the future)
		private static readonly Dictionary<string, byte[]> RawDemoDataLookup = RawDemoDataLookup = new Dictionary<string, byte[]>();
		private static bool _loaded;

		public static readonly string ProjectDir =
			// bin/Debug/net461 -> ../../..
			Directory.GetParent(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!)!.Parent!.Parent!.FullName;


		[OneTimeSetUp]
		protected virtual void Init() {
			lock (RawDemoDataLookup) {
				if (_loaded)
					return;
				// load each demo file, but don't parse anything
				foreach (TestCaseData testCase in DemoList) {
					var file = testCase.Arguments[0].ToString();
					RawDemoDataLookup[file] = File.ReadAllBytes($"{ProjectDir}/sample demos/{file}");
				}
				_loaded = true;
			}
		}


		// copy the demo file and parse it to ensure that tests are self contained
		protected static SourceDemo GetDemo(string fileName) {
			if (!_loaded)
				Assert.Fail($"call {nameof(Init)} before calling {nameof(GetDemo)}");
			if (RawDemoDataLookup.TryGetValue(fileName, out byte[] data)) {
				byte[] copy = new byte[data.Length];
				Array.Copy(data, copy, data.Length);
				SourceDemo demo = new SourceDemo(copy, demoName: fileName);
				demo.Parse();
				return demo;
			}
			Assert.Fail($"demo \"{fileName}\" not in {nameof(DemoList)}");
			return null;
		}
	}
}
