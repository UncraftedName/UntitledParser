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
                /*ConsoleOptions.ParseOptions(new[]
                    {@"../../demos/cm-08-inbounds-1182.dem", "-r", "-v", "-l", "-m", "-c", "-p", "-R", "-f", "ya boi", "-s"});*/
                    
                const string demoName = "problem demos\\chapter7";
                SourceDemo sd = new SourceDemo(new DirectoryInfo($@"..\..\demos\{demoName}.dem"), false);
                //sd.QuickParse();
                sd.ParseBytes();
                    
                Console.WriteLine(sd.FilteredForPacketType<Packet>().Where(packet => packet.MessageType == SvcMessageType.NetTick)
                    .Select(packet => packet.SvcNetMessage).Cast<NetTick>().Select(netTick => netTick.EngineTick - netTick.Tick).Distinct().QuickToString());

                //File.WriteAllText(@"B:\Projects\Rider\UncraftedDemoParser\UncraftedDemoParser\bin\Debug\output.txt", sd.AsVerboseString());
                //sd.UpdateBytes();
                //File.WriteAllBytes(@"B:\Projects\Rider\UncraftedDemoParser\UncraftedDemoParser\bin\Debug\ademo.dem", sd.Bytes);
            }
        }
    }
}