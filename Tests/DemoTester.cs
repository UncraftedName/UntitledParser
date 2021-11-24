using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using ConsoleApp.DemoArgProcessing.Options;
using DemoParser.Parser;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.Components.Messages.UserMessages;
using DemoParser.Utils;
using NUnit.Framework;

namespace Tests {
	
	public class DemoTester {
		
		private static readonly string ProjectDir =
			// bin/Debug/net461 -> ../../..
			Directory.GetParent(
				Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
				.Parent!.Parent!.FullName;

		private static readonly string DumpFolder = $"{ProjectDir}/sample demos/demo dump";
		

		[OneTimeSetUp]
		public void Init() {
			Directory.CreateDirectory(DumpFolder);
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
		[TestCase("l4d1 1005.dem", Description = "Left 4 Dead 1 (version 1.0.0.5)")]
		[TestCase("l4d2 2000.dem", Description = "Left 4 Dead 2 (protocol 2.0.0.0)")]
		[TestCase("l4d2 2042.dem", Description = "Left 4 Dead 2 (protocol 2.0.4.2)")]
		[TestCase("l4d2 2042_2.dem", Description = "Left 4 Dead 2 (protocol 2.0.4.2, _2)")]
		[TestCase("hl2 oe.dem", Description = "Half life 2 Old Engine")]
		public void ParseAndDumpDemos(string fileName) {
			try {
				SourceDemo demo = new SourceDemo($"{ProjectDir}/sample demos/{fileName}");
				demo.Parse();
				using PrettyStreamWriter psw = new PrettyStreamWriter(
					new FileStream($"{DumpFolder}/{demo.FileName![..^4]}.txt", FileMode.Create));
				demo.PrettyWrite(psw);
			} catch (Exception e) {
				Debug.WriteLine(e);
				Console.WriteLine(e);
				Assert.Fail($"{fileName} failed to parse: {e.Message}");
			}
		}
		

		[TestCase("portal 1 unpack.dem", Description = "Portal 1 5135 demo without captions")]
		[TestCase("captions.dem"), Description("Portal 1 5135 demo with captions")]
		public void RemoveCaptions(string fileName) {
			SourceDemo before = new SourceDemo($"{ProjectDir}/sample demos/{fileName}");
			before.Parse();
			MemoryStream outStream = new MemoryStream();
			OptRemoveCaptions.RemoveCaptions(before, outStream);
			SourceDemo after = new SourceDemo(outStream.ToArray());
			after.Parse();
			// assert that the new demo doesn't have captions
			CollectionAssert.IsEmpty(after.FilterForUserMessage<CloseCaption>());
			// assert that all the packets are still the same
			CollectionAssert.AreEqual(
				before.Frames.Select(frame => frame.Type),
				after.Frames.Select(frame => frame.Type));
			// assert that all the non-caption and non-nop messages are the same
			CollectionAssert.AreEqual(
				before.FilterForMessages().Where(tuple => tuple.messageType != MessageType.NetNop)
					.Where(tuple => (tuple.message as SvcUserMessage)?.MessageType != UserMessageType.CloseCaption)
					.Select(tuple => tuple.messageType),
				after.FilterForMessages().Where(tuple => tuple.messageType != MessageType.NetNop)
					.Select(tuple => tuple.messageType)
			);
		}
	}
}
