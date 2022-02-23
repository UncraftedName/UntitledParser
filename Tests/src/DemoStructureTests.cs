using System.IO;
using System.Linq;
using System.Text;
using DemoParser.Parser;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.Components.Packets;
using DemoParser.Parser.EntityStuff;
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


		[TestCaseSource(nameof(DemoList)), Parallelizable(ParallelScope.All)]
		public void EnsureEntsParsed(string fileName) {
			var demo = GetDemo(fileName);
			if ((demo.DemoParseResult & DemoParseResult.EntParsingEnabled) == 0)
				Assert.Ignore("entity parsing is not enabled for this game");
			Assert.That((demo.DemoParseResult & DemoParseResult.EntParsingFailed) == 0, "entity parsing enabled, but failed");
		}


		// This is a critical assumption I make for entity parsing stuff. If this fails, you're either doing something
		// wrong or you're completely screwed.
		[TestCaseSource(nameof(DemoList)), Parallelizable(ParallelScope.All)]
		public void AssertServerClassIndices(string fileName) {
			foreach (DataTables dataTables in GetDemo(fileName).FilterForPacket<DataTables>())
				for (int i = 0; i < dataTables.ServerClasses!.Count; i++)
					Assert.AreEqual(i, dataTables.ServerClasses![i].DataTableId, "server class ID does not match its index");
		}


		// we use net tick to check that ents are being parsed goodly
		[TestCaseSource(nameof(DemoList)), Parallelizable(ParallelScope.All)]
		public void AssertSingleNetTickMessage(string fileName) {
			foreach (Packet packet in GetDemo(fileName).FilterForPacket<Packet>())
				Assert.LessOrEqual(packet.FilterForMessage<NetTick>().Count(), 1, $"there exists a packet with more than one {nameof(NetTick)} message");
		}
	}
}
