using System;
using System.Diagnostics;
using System.IO;
using DemoParser.Parser;
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
                File.WriteAllText(
                    $"{ProjectDir}/sample demos/verbose output/{demo.FileName[..^4]}.txt", 
                    demo.ToVerboseString());
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
    }
}