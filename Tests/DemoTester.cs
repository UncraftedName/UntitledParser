using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using ConsoleApp;
using DemoParser.Parser;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Messages;
using DemoParser.Utils;
using NUnit.Framework;

namespace Tests {
	public class DemoTester {
		private static readonly string ProjectDir =
			// bin/Debug/net461 -> ../../..
			Directory.GetParent(
				Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
				.Parent.Parent.FullName;

		[OneTimeSetUp]
		public void Init() {
			Directory.CreateDirectory(ProjectDir + "/sample demos/demo output");
			Directory.CreateDirectory(ProjectDir + "/sample demos/verbose output");
		}

		private static void ParseDemo(string path) {
			try {
				SourceDemo demo = new SourceDemo(path);
				demo.Parse();
				demo.WriteVerboseString(new StreamWriter(
					$"{ProjectDir}/sample demos/verbose output/{demo.FileName[..^4]}.txt"));
			} catch (Exception e) {
				Debug.WriteLine(e);
				Console.WriteLine(e);
				Assert.Fail($"{path} failed to parse: {e.Message}");
			}
		}

		[TestCase("portal 1 leak.dem", Description = "Portal 1 (Leak)")]
		[TestCase("portal 1 unpack.dem", Description = "Portal 1 (Source Unpack)")]
		[TestCase("portal 1 unpack hltv client.dem", Description = "Portal 1 (Source Unpack HLTV Client)")]
		[TestCase("portal 1 unpack hltv server.dem", Description = "Portal 1 (Source Unpack HLTV Server)")]
		[TestCase("portal 1 unpack spliced.dem", Description = "Portal 1 (Source Unpack Spliced)")]
		[TestCase("portal 1 steampipe.dem", Description = "Portal 1 (SteamPipe)")]
		[TestCase("portal 1 3420.dem", Description = "Portal 1 (3420)")]
		[TestCase("portal 2 sp.dem", Description = "Portal 2 single player")]
		[TestCase("portal 2 coop.dem", Description = "Portal 2 co-op")]
		[TestCase("portal 2 coop long.dem", Description = "Portal 2 co-op 2 (long)")]
		[TestCase("l4d2 2000.dem", Description = "Left 4 Dead 2 (protocol 2.0.0.0)")]
		[TestCase("l4d2 2042.dem", Description = "Left 4 Dead 2 (protocol 2.0.4.2)")]
		[TestCase("l4d2 2042_2.dem", Description = "Left 4 Dead 2 (protocol 2.0.4.2, _2)")]
		public void ParseDemos(string file) {
			ParseDemo($"{ProjectDir}/sample demos/{file}");
		}

		[Test]
		public void RemoveCaptions() {
			string demoDir = $"{ProjectDir}/sample demos";
			ConsoleFunctions.Main(new [] {
				$"{demoDir}/captions.dem", "--remove-captions", "-f", $"{demoDir}/demo output"
			});
			SourceDemo before = new SourceDemo($"{demoDir}/captions.dem");
			SourceDemo after = new SourceDemo($"{demoDir}/demo output/captions-captions_removed.dem");
			before.Parse();
			after.Parse();
			// assert that all the packets are still the same
			CollectionAssert.AreEqual(
				before.Frames.Select(frame => frame.Type),
				after.Frames.Select(frame => frame.Type));
			// assert that all the non-caption and non-nop messages are the same
			CollectionAssert.AreEqual(
				before.FilterForMessages().Where(tuple => tuple.messageType != MessageType.NetNop)
					.Where(tuple => (tuple.message as SvcUserMessageFrame)?.MessageType != UserMessageType.CloseCaption)
					.Select(tuple => tuple.messageType),
				after.FilterForMessages().Where(tuple => tuple.messageType != MessageType.NetNop)
					.Select(tuple => tuple.messageType)
			);
		}
	}
}