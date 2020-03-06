using System;
using System.Linq;
using System.Numerics;
using ConsoleParser;
using UntitledParser.Parser;
using UntitledParser.Parser.Components.Messages;
using UntitledParser.Parser.Components.Packets;
using UntitledParser.Utils;

namespace My_Playground {
	
	internal static class Program {

		private static void Main() {
			Console.WriteLine();
			//ConsoleFunctions.Main("2020.02.29-10.47.31 -R .* -l -f simple".Split());
			// difference in angle testing
			/*SourceDemo demo1 = new SourceDemo("2020.02.29-10.47.31/first.dem");
			SourceDemo demo2 = new SourceDemo("2020.02.29-10.47.31/saveload from end of first.dem");
			demo1.Parse();
			demo2.Parse();
			CmdInfo firstInfo = demo2.FilterForPacketType<Packet>().First().PacketInfo[0];
			demo1
				.FilterForPacketType<Packet>()
				.Select(packet => (AngDiff: AngDiff(packet.PacketInfo[0], firstInfo), packet.Tick))
				.OrderBy(tuple => tuple.AngDiff)
				.Take(30)
				.ToList()
				.ForEach(tuple => Console.WriteLine($"[{tuple.Tick}] {tuple.AngDiff}"));*/


			SourceDemo demo = new SourceDemo("015-qv2b.dem");
			demo.Parse();

			/*BitStreamReader r = demo.FilterForMessageType<SvcPacketEntities>()
				.ElementAt(0)
				.Reader;*/

			BitStreamReader r = demo.FilterForMessageType<SvcUpdateStringTables>()
				.ElementAt(0)
				.Reader;

			/*BitStreamReader r = demo.FilterForPacketType<StringTables>().First()
				.Tables.First(table => table.Name == "instancebaseline")
				.TableEntries.Where(entry => entry != null)
				.Select(entry => entry.EntryData)
				.Cast<InstanceBaseLine>()
				.First(baseline => baseline.ServerClassRef.ClassName == "CHL2_Player")
				.Reader;*/

			BitStreamWriter tmp = new BitStreamWriter();
			tmp.WriteBitsFromInt(90, 10);
			byte[] bytesToWrite = tmp.AsArray;
			
			r.SubStream().FindUIntAllInstances(100, 10)
				.Select(i => i + r.AbsoluteBitIndex)
				.ToList()
				.ForEach(i => {
					BitStreamWriter bsw = new BitStreamWriter(demo.Reader.Data);
					bsw.EditBitsAtIndex(i, bytesToWrite, 10);
					//File.WriteAllBytes($"{demo.FileName[..^4]}_{i}_{i - r.AbsoluteBitIndex}.dem", bsw.AsArray);
					Console.WriteLine($"{demo.FileName[..^4]}_{i}_{i - r.AbsoluteBitIndex}");
				});
		}


		private static float AngDiff(CmdInfo info1, CmdInfo info2) {
			return (float)Math.Acos(Vector3.Dot(InGameAngsToVec3(info1.ViewAngles), InGameAngsToVec3(info2.ViewAngles))
									/ (info1.ViewAngles.Length() * info2.ViewAngles.Length()));
		}


		private static Vector3 InGameAngsToVec3(Vector3 v3) {
			const float toRad = (float)(Math.PI / 180.0f);
			return new Vector3 {
				X = (float)(Math.Cos(v3.Y * toRad) * Math.Cos(-v3.X * toRad)),
				Y = (float)(Math.Sin(v3.Y * toRad) * Math.Cos(-v3.X * toRad)),
				Z = (float)Math.Sin(-v3.X * toRad)
			};
		}
	}
}