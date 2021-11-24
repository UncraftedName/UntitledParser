using System;
using System.IO;
using System.Linq;
using ConsoleApp.DemoArgProcessing.Options;
using DemoParser.Parser;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.Components.Messages.UserMessages;
using DemoParser.Utils;
using NUnit.Framework;

namespace Tests {
	
	public class ConsoleOptionTests {

		[TestCase("portal 1 unpack.dem", Description = "Portal 1 5135 demo without captions")]
		[TestCase("captions.dem"), Description("Portal 1 5135 demo with captions")]
		public void RemoveCaptions(string fileName) {
			SourceDemo before = new SourceDemo($"{ParseTests.ProjectDir}/sample demos/{fileName}");
			before.Parse();
			MemoryStream outStream = new MemoryStream();
			OptRemoveCaptions.RemoveCaptions(before, outStream);
			SourceDemo after = new SourceDemo(outStream.ToArray());
			after.Parse();
			// check that the new demo doesn't have captions
			CollectionAssert.IsEmpty(after.FilterForUserMessage<CloseCaption>());
			// check that all the packets are unchanged
			CollectionAssert.AreEqual(
				before.Frames.Select(frame => frame.Type),
				after.Frames.Select(frame => frame.Type));
			// check that all the non-caption and non-nop messages are unchanged
			CollectionAssert.AreEqual(
				before.FilterForMessages().Where(tuple => tuple.messageType != MessageType.NetNop)
					.Where(tuple => (tuple.message as SvcUserMessage)?.MessageType != UserMessageType.CloseCaption)
					.Select(tuple => tuple.messageType),
				after.FilterForMessages().Where(tuple => tuple.messageType != MessageType.NetNop)
					.Select(tuple => tuple.messageType)
			);
		}


		[TestCase("portal 1 leak.dem", Description = "Portal 1 (Leak)")]
		[TestCase("portal 1 unpack.dem", Description = "Portal 1 (Source Unpack)")]
		[TestCase("portal 1 unpack spliced.dem", Description = "Portal 1 (Source Unpack Spliced)")]
		[TestCase("portal 1 steampipe.dem", Description = "Portal 1 (SteamPipe)")]
		[TestCase("portal 1 3420.dem", Description = "Portal 1 (3420)")]
		public void ChangeDemoDir(string fileName) {
			const string newDir = "sussy baka uwu";
			SourceDemo before = new SourceDemo($"{ParseTests.ProjectDir}/sample demos/{fileName}");
			before.Parse();
			MemoryStream outStream = new MemoryStream();
			OptChangeDemoDir.ChangeDemoDir(before, outStream, newDir);
			SourceDemo after = new SourceDemo(outStream.ToArray());
			after.Parse();
			// check that all the packets are unchanged
			CollectionAssert.AreEqual(
				before.Frames.Select(frame => frame.Type),
				after.Frames.Select(frame => frame.Type));
			// check that all messages are unchanged
			CollectionAssert.AreEqual(
				before.FilterForMessages().Select(t => t.messageType),
				after.FilterForMessages().Select(t => t.messageType));
			// check that the new demo has the new dir in header and SvcServerInfo messages
			Assert.AreEqual(after.Header.GameDirectory.Str, newDir);
			Assert.That(after.FilterForMessage<SvcServerInfo>().Select(t => t.message.GameDir), Is.All.EqualTo(newDir));
		}
	}
}
