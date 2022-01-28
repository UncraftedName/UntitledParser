using System.IO;
using System.Text;
using DemoParser.Parser;
using DemoParser.Parser.Components.Packets;
using DemoParser.Parser.HelperClasses.EntityStuff;
using DemoParser.Utils;
using NUnit.Framework;

namespace Tests {

	public class DemoStructureTests : DemoParseTests {

		private static readonly string DumpFolder = $"{ProjectDir}/sample demos/demo dump";


		[OneTimeSetUp]
		protected override void Init() {
			base.Init();
			Directory.CreateDirectory(DumpFolder);
		}


		private static bool IsAscii(string s) {
			return Encoding.UTF8.GetByteCount(s) == s.Length;
		}


		[TestCaseSource(nameof(DemoList)), Parallelizable(ParallelScope.All)]
		public void ParseAndDumpDemo(string fileName) {
			var demo = GetDemo(fileName);
			using PrettyStreamWriter psw = new PrettyStreamWriter(
				new FileStream($"{DumpFolder}/{demo.FileName![..^4]}.txt", FileMode.Create));
			demo.PrettyWrite(psw);
		}


		[TestCaseSource(nameof(DemoList)), Parallelizable(ParallelScope.All)]
		public void BasicTimeTest(string fileName) {
			var demo = GetDemo(fileName);
			// all these demos should be a few seconds to a few minutes long
			Assert.Greater(demo.TickCount(false), 100);
			Assert.Less(demo.TickCount(false), 100000);
			Assert.Greater(demo.AdjustedTickCount(false), 100);
			Assert.Less(demo.AdjustedTickCount(false), 100000);
			Assert.Greater(demo.DemoInfo.TickInterval, 1.0/100);
			Assert.Less(demo.DemoInfo.TickInterval, 1.0/10);
		}


		[TestCaseSource(nameof(DemoList)), Parallelizable(ParallelScope.All)]
		public void EnsureDataTablesParsed(string fileName) {
			var demo = GetDemo(fileName);
			foreach (DataTables dataTables in demo.FilterForPacket<DataTables>()) {
				Assert.NotNull(dataTables.Tables);
				CollectionAssert.AllItemsAreNotNull(dataTables.Tables);
				foreach (SendTable sendTable in dataTables.Tables) {
					Assert.NotNull(sendTable.SendProps);
					CollectionAssert.AllItemsAreNotNull(sendTable.SendProps);
					Assert.That(IsAscii(sendTable.Name), $"table name \"{sendTable.Name}\" contains non-ascii chars");
					Assert.AreEqual(sendTable.ExpectedPropCount, sendTable.SendProps.Count, "wrong number of send props read");
					foreach (SendTableProp sendProp in sendTable.SendProps)
						Assert.That(IsAscii(sendProp.Name), $"table prop name \"{sendProp.Name}\" contains non-ascii chars");
				}
			}
		}


		[TestCase("portal 1 leak.dem", Description = "Portal 1 (Leak)")]
		[TestCase("portal 1 5135 spliced.dem", Description = "Portal 1 (5135 Spliced)")]
		[TestCase("portal 1 3420.dem", Description = "Portal 1 (3420)")]
		[TestCase("portal 1 5135 captions.dem", Description = "Portal 1 5135 demo with captions")]
		[TestCase("portal 1 5135 hltv client.dem", Description = "Portal 1 (5135 HLTV Client)")]
		[TestCase("portal 1 5135 hltv server.dem", Description = "Portal 1 (5135 HLTV Server)")]
		[TestCase("portal 2 sp.dem", Description = "Portal 2 single player")]
		[TestCase("portal 2 coop.dem", Description = "Portal 2 co-op")]
		[Parallelizable(ParallelScope.All)]
		public void EnsureEntsParsed(string fileName) {
			var demo = GetDemo(fileName);
			// flag will get set to 0 if ent parsing fails
			Assert.That((demo.DemoParseResult & DemoParseResult.EntParsingEnabled) != 0);
		}
	}
}
