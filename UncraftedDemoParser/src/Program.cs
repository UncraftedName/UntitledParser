using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UncraftedDemoParser.ConsoleParsing;
using UncraftedDemoParser.Parser;
using UncraftedDemoParser.Parser.Components;
using UncraftedDemoParser.Parser.Components.Abstract;
using UncraftedDemoParser.Parser.Components.Packets;
using UncraftedDemoParser.Parser.Components.SvcNetMessages;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser { 
    
    internal static class Program {

        public static void Main(string[] args) {
            if (!Debugger.IsAttached) {
                ConsoleOptions.ParseOptions(args);
                Environment.Exit(0);
            } else {
                ConsoleOptions.ParseOptions(new[]
                    {@"3420demo.dem", "jless-00-07-bill_play3-1579.dem", "-v", "-f", "ya boi", "-m"});
                
                const string demoName = "problem demos\\18_camerafly_attempt_1_2";
                //SourceDemo sd = new SourceDemo(new DirectoryInfo($@"..\..\demos\{demoName}.dem"), false);
                SourceDemo sd = new SourceDemo("3420demo.dem", false);
                sd.QuickParse();
                sd.ParseBytes();
                
                
                //File.WriteAllText(@"B:\Projects\Rider\UncraftedDemoParser\UncraftedDemoParser\bin\Debug\output.txt", sd.AsVerboseString());
            }
        }
    }
}