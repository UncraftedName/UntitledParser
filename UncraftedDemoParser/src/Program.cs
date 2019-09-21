using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UncraftedDemoParser.Parser;
using UncraftedDemoParser.Parser.Components.Abstract;
using UncraftedDemoParser.Parser.Components.Packets;
using UncraftedDemoParser.Parser.Components.SvcNetMessages;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser { 
    internal static class Program {

        public static void Main(string[] args) {
            if (!Debugger.IsAttached) { // this is a mess atm
                if (args.Length == 0 || args[0].Equals("--help") || args[0].Equals("/?") || args[0].Equals("?"))
                    ConsoleOptions.PrintHelpAndExit();
                else
                    new ConsoleOptions(args).ProcessOptions();
                Environment.Exit(0);
            } else {
                // assume that running from IDE (using debugger)
                const string demoName = "portal2";
                SourceDemo sd = new SourceDemo(new DirectoryInfo($@"..\..\demos\{demoName}.dem"));
                sd.PrintListdemoOutput(printHeader: true, printName: true);
                sd.AsVerboseString().WriteToFiles(@"..\..\out\_verbose.txt", $@"..\..\out\verbose-{demoName}.txt");
                sd.SvcMessagesCounterAsString().WriteToFiles(@"..\..\out\_svc message counter.txt",
                    $@"..\..\out\svc message counter-{demoName}.txt");
                sd.SvcMessagesAsString().WriteToFiles(@"..\..\out\_sub packet data.txt",
                    $@"..\..\out\sub packet data - {demoName}.txt");
                
                Console.WriteLine(sd.FilterForPacketType<Packet>().Where(packet => packet.MessageType == SvcMessageType.NetTick)
                    .Select(packet => packet.SvcNetMessage).Cast<NetTick>().Select(netTick => netTick.EngineTick - netTick.Tick).Distinct().QuickToString());
            }
        }
    }
}