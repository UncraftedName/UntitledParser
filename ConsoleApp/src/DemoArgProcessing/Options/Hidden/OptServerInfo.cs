using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using ConsoleApp.GenericArgProcessing;
using DemoParser.Parser;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.Components.Packets;
using DemoParser.Utils;

namespace ConsoleApp.DemoArgProcessing.Options.Hidden
{

    public class OptServerInfo : DemoOption<OptServerInfo.InfoFlags>
    {

        public static readonly ImmutableArray<string> DefaultAliases = new[] { "--server-info" }.ToImmutableArray();

        private const int FmtIdt = -25;

        [Flags]
        public enum InfoFlags
        {
            ServerCountOnly = 1,
        }

        public OptServerInfo() : base(
            DefaultAliases,
            Arity.ZeroOrOne,
            "Print SvcServerInfo" +
            $"\nUse flag \"{InfoFlags.ServerCountOnly}\" or \"1\" to print server count only.",
            "flags",
            Utils.ParseEnum<InfoFlags>,
            default,
            true)
        { }


        protected override void AfterParse(DemoParsingSetupInfo setupObj, InfoFlags arg, bool isDefault)
        {
            setupObj.ExecutableOptions++;
        }

        protected override void Process(DemoParsingInfo infoObj, InfoFlags arg, bool isDefault)
        {
            infoObj.PrintOptionMessage("searching for SvcServerInfo");
            try
            {
                bool any = false;
                foreach ((SvcServerInfo info, int tick) in infoObj.CurrentDemo.FilterForMessage<SvcServerInfo>())
                {
                    any = true;
                    WriteServerInfo(info, Console.Out, arg);
                    break;
                }
                if (!any)
                    Utils.Warning("SvcServerInfo not found");
            }
            catch (Exception)
            {
                Utils.Warning("Search for SvcServerInfo failed.\n");
            }
        }

        public static IEnumerable<SvcServerInfo> GetServerInfo(SourceDemo demo)
        {
            return
                from packet in demo.FilterForPacket<Packet>()
                from message in packet.MessageStream
                where message?.GetType() == typeof(SvcServerInfo)
                select (SvcServerInfo)message;
        }

        public static void WriteServerInfo(SvcServerInfo info, TextWriter tw, InfoFlags flags)
        {
            if (flags == InfoFlags.ServerCountOnly)
            {
                tw.Write($"{"Server count ",FmtIdt}: {info.ServerCount}\n\n");
            }
            else
            {
                tw.Write($"{"Network protocol ",FmtIdt}: {info.NetworkProtocol}" +
                         $"\n{"Server count ",FmtIdt}: {info.ServerCount}" +
                         $"\n{"Is hltv ",FmtIdt}: {info.IsHltv}" +
                         $"\n{"Is dedicated ",FmtIdt}: {info.IsDedicated}");
                if (info.RestrictWorkshopAddons != null)
                {
                    tw.Write($"\n{"Restrict workshop addons ",FmtIdt}: {info.RestrictWorkshopAddons}");
                }
                tw.Write($"\n{"Player count ",FmtIdt}: {info.PlayerCount}" +
                         $"\n{"Max player count ",FmtIdt}: {info.MaxClients}" +
                         $"\n{"Tick interval ",FmtIdt}: {info.TickInterval}" +
                         $"\n{"Platform ",FmtIdt}: {info.Platform}" +
                         $"\n{"Game directory ",FmtIdt}: {info.GameDir}" +
                         $"\n{"Map name ",FmtIdt}: {info.MapName}" +
                         $"\n{"Skybox name ",FmtIdt}: {info.SkyName}" +
                         $"\n{"Host name ",FmtIdt}: {info.HostName}");
                if (info.MissionName != null && info.MutationName != null)
                {
                    tw.Write($"\n{"Mission name ",FmtIdt}: {info.MissionName}" +
                             $"\n{"Mutation name ",FmtIdt}: {info.MutationName}");
                }

                if (info.HasReplay != null)
                {
                    tw.Write($"\n{"Has replay ",FmtIdt}: {info.HasReplay}");
                }
                tw.Write("\n\n");
            }
        }

        protected override void PostProcess(DemoParsingInfo infoObj, InfoFlags arg, bool isDefault) { }
    }
}
