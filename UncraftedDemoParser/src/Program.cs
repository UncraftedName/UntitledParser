using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UncraftedDemoParser.ConsoleParsing;
using UncraftedDemoParser.Parser;
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
                    {@"../../demos", "-r", "dsp", "-v", "-l", "-m", "-c", "-p", "-R", "-f", "ya boi", "-s"});
                    
                const string demoName = "toggleduck";
                SourceDemo sd = new SourceDemo(new DirectoryInfo($@"..\..\demos\{demoName}.dem"));
                    
                Console.WriteLine(sd.FilteredForPacketType<Packet>().Where(packet => packet.MessageType == SvcMessageType.NetTick)
                    .Select(packet => packet.SvcNetMessage).Cast<NetTick>().Select(netTick => netTick.EngineTick - netTick.Tick).Distinct().QuickToString());
            }
        }
    }
}