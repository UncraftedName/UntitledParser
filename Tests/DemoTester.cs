using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ConsoleApp;
using DemoParser.Parser;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Messages;
using DemoParser.Utils;
using NUnit.Framework;

namespace Tests {
    
    // i'm not sure how to set this up properly don't judge me
    public class DemoTester {

        private static readonly string ProjectDir = 
            Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
        

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
        

        [Test]
        public void Portal1Leak() {
            ParseDemo($"{ProjectDir}/sample demos/portal 1 leak.dem");
        }
        
        
        [Test]
        public void Portal1Unpack() {
            ParseDemo($"{ProjectDir}/sample demos/portal 1 unpack.dem");
            ParseDemo($"{ProjectDir}/sample demos/portal 1 unpack hltv client.dem");
            ParseDemo($"{ProjectDir}/sample demos/portal 1 unpack hltv server.dem");
        }


        [Test]
        public void Portal1UnpackSpliced() {
            ParseDemo($"{ProjectDir}/sample demos/portal 1 unpack spliced.dem");
        }
        
        
        [Test]
        public void Portal1Steampipe() {
            ParseDemo($"{ProjectDir}/sample demos/portal 1 steampipe.dem");
        }
        
        
        [Test]
        public void Portal13420() {
            ParseDemo($"{ProjectDir}/sample demos/portal 1 3420.dem");
        }
        
        
        [Test]
        public void Portal2Coop() {
            ParseDemo($"{ProjectDir}/sample demos/portal 2 coop.dem");
            ParseDemo($"{ProjectDir}/sample demos/portal 2 coop long.dem");
        }
        
        
        [Test]
        public void Portal2SinglePlayer() {
            ParseDemo($"{ProjectDir}/sample demos/portal 2 sp.dem");
        }
        
        
        [Test]
        public void L4D2_2000() {
            ParseDemo($"{ProjectDir}/sample demos/l4d2 2000.dem");
        }
        
        
        [Test]
        public void L4D2_2042() {
            ParseDemo($"{ProjectDir}/sample demos/l4d2 2042.dem");
            ParseDemo($"{ProjectDir}/sample demos/l4d2 2042_2.dem");
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