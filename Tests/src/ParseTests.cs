using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using DemoParser.Parser;
using DemoParser.Utils;
using NUnit.Framework;

namespace Tests {
	
	public class ParseTests {
		
		public static readonly string ProjectDir =
			// bin/Debug/net461 -> ../../..
			Directory.GetParent(
				Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
				.Parent!.Parent!.FullName;

		private static readonly string DumpFolder = $"{ProjectDir}/sample demos/demo dump";
		

		[OneTimeSetUp]
		public void Init() {
			Directory.CreateDirectory(DumpFolder);
		}


		[TestCase("portal 1 leak.dem", Description = "Portal 1 (Leak)")]
		[TestCase("portal 1 unpack.dem", Description = "Portal 1 (Source Unpack)")]
		[TestCase("portal 1 unpack hltv client.dem", Description = "Portal 1 (Source Unpack HLTV Client)")]
		[TestCase("portal 1 unpack hltv server.dem", Description = "Portal 1 (Source Unpack HLTV Server)")]
		[TestCase("portal 1 unpack spliced.dem", Description = "Portal 1 (Source Unpack Spliced)")]
		[TestCase("portal 1 steampipe.dem", Description = "Portal 1 (SteamPipe)")]
		[TestCase("portal 1 3420.dem", Description = "Portal 1 (3420)")]
		[TestCase("portal 2 sp.dem", Description = "Portal 2 single player")]
		[TestCase("portal 2 coop.dem", Description = "Portal 2 co-op")]
		[TestCase("portal 2 coop long.dem", Description = "Portal 2 co-op 2 (long)")]
		[TestCase("l4d1 1005.dem", Description = "Left 4 Dead 1 (version 1.0.0.5)")]
		[TestCase("l4d2 2000.dem", Description = "Left 4 Dead 2 (protocol 2.0.0.0)")]
		[TestCase("l4d2 2042.dem", Description = "Left 4 Dead 2 (protocol 2.0.4.2)")]
		[TestCase("l4d2 2042_2.dem", Description = "Left 4 Dead 2 (protocol 2.0.4.2, _2)")]
		[TestCase("hl2 oe.dem", Description = "Half life 2 Old Engine")]
		[Parallelizable(ParallelScope.All)]
		public void ParseAndDumpDemo(string fileName) {
			try {
				SourceDemo demo = new SourceDemo($"{ProjectDir}/sample demos/{fileName}");
				demo.Parse();
				using PrettyStreamWriter psw = new PrettyStreamWriter(
					new FileStream($"{DumpFolder}/{demo.FileName![..^4]}.txt", FileMode.Create));
				demo.PrettyWrite(psw);
			} catch (Exception e) {
				Debug.WriteLine(e);
				Console.WriteLine(e);
				Assert.Fail($"{fileName} failed to parse: {e.Message}");
			}
		}
	}
}
