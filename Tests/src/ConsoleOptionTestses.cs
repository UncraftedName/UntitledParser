using System;
using System.IO;
using System.Linq;
using ConsoleApp;
using ConsoleApp.DemoArgProcessing.Options;
using DemoParser.Parser;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.Components.Messages.UserMessages;
using DemoParser.Utils;
using NUnit.Framework;

namespace Tests {

	public class ConsoleOptionTestses : DemoParseTests {

		private enum BasicEnum {
			Val0,
			Val1,
			Val2,
		}


		[Flags]
		private enum BasicFlagsEnum {
			Val1 = 1,
			Val2 = 2,
			Val4 = 4,
			Val8 = 8
		}


		[TestCase("Val0", BasicEnum.Val0, true)]
		[TestCase(" VAL2 \t ", BasicEnum.Val2, true)]
		[TestCase(" 1 ", BasicEnum.Val1, true)]
		[TestCase("val1  \t", BasicFlagsEnum.Val1, true)]
		[TestCase("", (BasicFlagsEnum)0, true)]
		[TestCase("0", (BasicFlagsEnum)0, true)]
		[TestCase("none", (BasicFlagsEnum)0, true)]
		[TestCase(" val2|val4 ", BasicFlagsEnum.Val2 | BasicFlagsEnum.Val4, true)]
		[TestCase("val4, val8", BasicFlagsEnum.Val4 | BasicFlagsEnum.Val8, true)]
		[TestCase("val1 | val1, val2", BasicFlagsEnum.Val1 | BasicFlagsEnum.Val2, true)]
		[TestCase("4 | 4 |1 |2", (BasicFlagsEnum)(1|2|4), true)]
		[TestCase("14", (BasicFlagsEnum)(2|4|8), true)]
		[TestCase("", BasicEnum.Val0, false)]
		[TestCase("e", BasicEnum.Val0, false)]
		[TestCase("-1", BasicEnum.Val0, false)]
		[TestCase("3", BasicEnum.Val0, false)]
		[TestCase("e", BasicFlagsEnum.Val1, false)]
		[TestCase("val1|val3|e", BasicFlagsEnum.Val1, false)]
		[TestCase("-1", BasicFlagsEnum.Val1, false)]
		[TestCase("16", BasicFlagsEnum.Val1, false)]
		public void ParseEnum<T>(string arg, T expected, bool valid) where T : Enum {
			// if not valid, expected is ignored (but we still it to get its type)
			if (valid) {
				Assert.AreEqual(Utils.ParseEnum<T>(arg), expected);
			} else {
				try {
					Utils.ParseEnum<T>(arg);
					Assert.Fail();
				} catch (ArgProcessUserException) {
					Assert.Pass();
				}
			}
		}


		[TestCase("portal 1 3420.dem", Description = "Portal 1 3420 demo without captions")]
		[TestCase("portal 1 5135 captions.dem"), Description("Portal 1 5135 demo with captions")]
		[Parallelizable(ParallelScope.All)]
		public void RemoveCaptions(string fileName) {
			var before = GetDemo(fileName);
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
		[TestCase("portal 1 5135 spliced.dem", Description = "Portal 1 (5135 Spliced)")]
		[TestCase("portal 1 steampipe.dem", Description = "Portal 1 (SteamPipe)")]
		[TestCase("portal 1 3420.dem", Description = "Portal 1 (3420)")]
		[TestCase("portal 1 5135 captions.dem"), Description("Portal 1 5135 demo with captions")]
		[Parallelizable(ParallelScope.All)]
		public void ChangeDemoDir(string fileName) {
			const string newDir = "sussy baka uwu";
			var before = GetDemo(fileName);
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
			Assert.AreEqual(after.Header.GameDirectory, newDir);
			Assert.That(after.FilterForMessage<SvcServerInfo>().Select(t => t.message.GameDir), Is.All.EqualTo(newDir));
		}
	}
}
