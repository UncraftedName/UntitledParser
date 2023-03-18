using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using ConsoleApp.DemoArgProcessing;
using ConsoleApp.DemoArgProcessing.Options;
using ConsoleApp.DemoArgProcessing.Options.Hidden;
using ConsoleApp.GenericArgProcessing;
using DemoParser.Parser;
using DemoParser.Parser.Components.Packets;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace ConsoleApp {

	/*
	 * This is the best place I can think of describing the argument parsing and option processing. Welcome
	 * to this mess of a codebase, please enjoy your stay.
	 *
	 * Third party libraries didn't give as much control as I wanted over the order of option processing.
	 * I went through many different iterations of arg processing before I settled on what I'm using now.
	 * This system was specifically designed for behaving like how I imagine listdemo+ would look like if
	 * it had more options, but there is an abstraction over the whole thing because abstraction is cool.
	 * This is a diagram of both the abstract interface and the implementation that's used:
	 *
	 *
	 *         BaseSubCommand      │                    DemoParserSubCommand
	 *           (Abstract)        │                      (Implementation)
	 *                             │
	 *      ┌──────────────────┐   │   ┌───────────────────┐
	 *      │Check Help/Version│   │   │Check Help/Version │       ┌──────────────────────────┐
	 *      │    Arguments     │   │   │     Arguments     │      ┌┤OptRemoveCaptions.Enable()│
	 *      └────────┬─────────┘   │   └─────────┬─────────┘      │├──────────────────────────┤
	 *               │             │             │                ││    OptPauses.Enable()    │
	 *               ▼             │             ▼                │├──────────────────────────┤
	 *      ┌──────────────────┐   │   ┌───────────────────┐      ││     OptTime.Enable()     │
	 *      │   ParseArgs()    │   │   │    ParseArgs()    ├─────►│├──────────────────────────┤
	 *      └────────┬─────────┘   │   └─────────┬─────────┘      ││            .             │
	 *               │             │             │                ││            .             │
	 *               ▼             │             ▼                └┤            .             │
	 *      ┌──────────────────┐   │   ┌───────────────────┐       └────────────┬─────────────┘
	 *      │    Process()     │   │   │ Enable OptTime if │                    │
	 *      └────────┬─────────┘   │   │ no other options  ├──────────┐         │
	 *               │             │   │    are enabled    │          ▼         ▼
	 *               │             │   └─────────┬─────────┘         ┌──────────────────────┐
	 *               │             │             │                   │  Set properties of   │
	 *               │             │             ▼                   │ DemoParsingSetupInfo │
	 *               │             │   ┌───────────────────┐         └──────────┬───────────┘
	 *               │             │   │Find all demo files├──────────┐         │
	 *               │             │   └─────────┬─────────┘          ▼         ▼
	 *               │             │             │                   ┌──────────────────────┐
	 *               │             │             │        ┌──────────┤Create DemoParsingInfo│
	 *               │             │             │        │          └──────────┬───────────┘
	 *               │             │             │        │                     │
	 *               │             │             ▼        ▼                     ▼
	 *               │             │   ┌───────────────────┐         ┌──────────────────────┐
	 *               │             │   │     Process()     │         │  Queue demo parsing  │
	 *               │             │   └─────────┬─────────┘         │(IParallelDemoParser) │
	 *               │             │             │                   └───────────────┬──────┘
	 *               │             │             │                                   │
	 *               │             │             │                                   │
	 * ┌─────────────┴──────────┐  │     ┌───────┴──────────────────────────────┐    │
	 * │┌──────────────────────┐│  │     │┌────────────────────────────────────┐│    │
	 * ││while (MoreThingsToDo)││  │     ││while (MoreDemosLeft())             ││    │
	 * │└─┬────────────────────┤│  │     │└─┬──────────────────────────────────┤│    │
	 * │  │option1.Process()   ││  │     │  │OptRemoveCaptions.RemoveCaptions()││    │
	 * │  ├────────────────────┤│  │     │  ├──────────────────────────────────┤│    │
	 * │  │option2.Process()   ││  │     │  │OptPauses.GetPauseTicks()         ││    │
	 * │  ├────────────────────┤│  │     │  ├──────────────────────────────────┤│    │
	 * │  │         .          ││  │     │  │                .                 ││    │
	 * │  │         .          ││  │     │  │                .                 ││    │
	 * │  │         .          ││  │     │  │                .                 ││    │
	 * │  ├────────────────────┤│  │     │  ├──────────────────────────────────┤│    │
	 * │  │GetNextThingToDo()  ││  │     │  │GetNextDemo()                     │◄────┘
	 * │┌─┴────────────────────┤│  │     │┌─┴──────────────────────────────────┤│
	 * ││DoneProcessing()      ││  │     ││DoneProcessing()                    ││
	 * │├──────────────────────┤│  │     │├────────────────────────────────────┤│
	 * ││option1.PostProcess() ││  │     ││OptTime.ShowTotalTime()             ││
	 * │├──────────────────────┤│  │     │├────────────────────────────────────┤│
	 * ││option2.PostProcess() ││  │     ││                 .                  ││
	 * │├──────────────────────┤│  │     ││                 .                  ││
	 * ││          .           ││  │     ││                 .                  ││
	 * ││          .           ││  │     │└────────────────────────────────────┘│
	 * ││          .           ││  │     └──────────────────────────────────────┘
	 * │└──────────────────────┘│  │
	 * └────────────────────────┘  │
	 */

	public static class Program {

		public static void Main(string[] args) {
			// It would be nice if the console color was reset if ctrl+c is used, but I haven't gotten that to work yet.
			// This line of code just eats ctrl+c and delays it by way too much.
			// Console.CancelKeyPress += (sender,eventArgs) => Console.ResetColor();


			SourceDemo d2 = new SourceDemo(@"D:\Games\Portal Source\portal\fullgame_29_2.dem");
			d2.Parse();
			// File.WriteAllText();

			SourceDemo d = new SourceDemo(@"D:\Games\Portal Source\portal\fullgame_29.dem");
			d.Parse();

			DataTables dataTables = d.FilterForPacket<DataTables>().Single();
			BitStreamReader bsr = d.Reader;
			BitStreamReader brTables = dataTables.Reader;
			var brSendTable = dataTables.Tables.Single(table => table.Name == "DT_PointSurvey").Reader;
			var brClass = dataTables.ServerClasses!.Values.Single(sc => sc.DataTableName == "DT_PointSurvey").Reader;

			var brClass2 = dataTables.ServerClasses!.Values.Single(sc => sc.DataTableName == "DT_PointCamera").Reader;

			// up to packet
			BitStreamWriter bsw = new BitStreamWriter();
			int readCount = brTables.AbsoluteStart;
			bsw.WriteBits(d.Reader.Data, readCount);
			bsr.SkipBits(readCount + brTables.BitLength);
			// up to table
			readCount = brSendTable.AbsoluteStart - brTables.AbsoluteStart;
			bsw.WriteBits(brTables.ReadBits(readCount), readCount);
			brTables.SkipBits(brSendTable.BitLength + 1);

			// up to start of server classes, write num server classes
			readCount = dataTables.ClassCountOff - brTables.AbsoluteBitIndex;
			bsw.WriteBits(brTables.ReadBits(readCount), readCount);
			bsw.WriteBitsFromUInt((uint)dataTables.ServerClasses.Count - 1, 16);
			brTables.SkipBits(16);
			// up to class
			readCount = brClass.AbsoluteStart - brTables.AbsoluteBitIndex;
			bsw.WriteBits(brTables.ReadBits(readCount), readCount);

			bsw.WriteBitsFromUInt(106, 16);
			brClass2.SkipBits(16);
			bsw.WriteBits(brClass2.ReadRemainingBits());

			brTables.SkipBits(brClass.BitLength);
			// after class
			bsw.WriteBits(brTables.ReadRemainingBits());

			int numBlankBits = dataTables.Reader.BitLength - (bsw.BitLength - brTables.AbsoluteStart);
			byte[] padBytes = new byte[numBlankBits / 8 + 1];
			bsw.WriteBits(padBytes, numBlankBits);
			// rest of the demo

			bsw.WriteBits(bsr.ReadRemainingBits());

			File.WriteAllBytes(@"D:\Games\Portal Source\portal\fullgame_29_2.dem", bsw);
			return;

			// I want exceptions to be in english :)
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
			CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
			CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

			Console.WriteLine("UntitledParser by UncraftedName");
			// write everything with color since we have no idea what/when could have triggered any exceptions
			try {
				// only use the one sub command - no other commands for the demo parser
				DemoParserSubCommand demoParserCommand = new DemoParserSubCommand(
					new BaseOption<DemoParsingSetupInfo, DemoParsingInfo>[] {
						// order is the same order that the options will get processed in, shouldn't really matter
						new OptOutputFolder(),
						new OptRecursive(),
						new OptOverwrite(),
						new OptRegexSearch(),
						new OptPauses(),
						new OptJumps(),
						new OptCheatCommands(),
						new OptDataTablesDump(),
						new OptStringTablesDump(),
						new OptRemoveCaptions(),
						new OptChangeDemoDir(),
						new OptSmoothGlessHops(),
						new OptRemovePauses(),
						new OptPositionDump(),
						new OptGladosShots(),
						new OptDemToTas(),
						new OptTeleports(),
						new OptPortals(),
						new OptInputs(),
						new OptDemoDump(),
						new OptTime() // this option should always be here and probably be last, it's sort of a default
					}.ToImmutableArray()
				);
				if (args.Length == 0) {
					Console.WriteLine(demoParserCommand.VersionString);
					Utils.WriteColor($"Usage: {demoParserCommand.UsageString}\n", ConsoleColor.DarkYellow);
					Utils.WriteColor(
						Utils.WillBeDestroyedOnExit()
							? $@"Open a new powershell window and use '.\{Utils.GetExeName()} --help' for help."
							: @$"Use '.\{Utils.GetExeName()} --help' for help.",
						ConsoleColor.Yellow);
				} else {
					demoParserCommand.Execute(Utils.FixPowerShellBullshit(args));
				}
			} catch (ArgProcessUserException e) {
				Utils.Warning($"User error: {e.Message}\n");
				Utils.WriteColor(@$"Use '.\{Utils.GetExeName()} --help' for help.", ConsoleColor.Yellow);
				Environment.ExitCode = 1;
			} catch (ArgProcessProgrammerException e) {
				Utils.Warning("Some programmer messed up, tell them they are silly (in a nice way).\n");
				Utils.Warning(e.ToString());
				Environment.ExitCode = 2;
			} catch (Exception e) {
				Utils.Warning("Unhandled exception! (This is not supposed to happen):\n");
				Utils.Warning(e.ToString());
				Environment.ExitCode = 3;
			}
			Console.ResetColor();
			if (Utils.WillBeDestroyedOnExit())
				Console.ReadLine();
		}
	}
}
