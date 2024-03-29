using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using ConsoleApp.GenericArgProcessing;
using DemoParser.Parser;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.Components.Packets;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace ConsoleApp.DemoArgProcessing.Options {

	public class OptChangeDemoDir : DemoOption<string> {

		public static readonly ImmutableArray<string> DefaultAliases = new[] {"--change-demo-dir", "-C"}.ToImmutableArray();


		private static string ParseString(string s) {
			if (s.Length > 260)
				throw new ArgProcessUserException("string too long");
			return s;
		}


		public OptChangeDemoDir() : base(
			DefaultAliases,
			Arity.One,
			"Create a new demo with the specified mod directory",
			"new_dir_name",
			ParseString,
			null!) {}


		protected override void AfterParse(DemoParsingSetupInfo setupObj, string arg, bool isDefault) {
			setupObj.ExecutableOptions++;
			setupObj.EditsDemos = true;
		}


		protected override void Process(DemoParsingInfo infoObj, string arg, bool isDefault) {
			infoObj.PrintOptionMessage("changing demo dir");
			Stream s = infoObj.StartWritingBytes("new_dir", ".dem");
			try {
				ChangeDemoDir(infoObj.CurrentDemo, s, arg);
			} catch (Exception) {
				Utils.Warning("Changing demo directory failed.\n");
				infoObj.CancelOverwrite = true;
			}
		}


		public static void ChangeDemoDir(SourceDemo demo, Stream s, string newDir) {

			void Write(byte[] buf) => s.Write(buf, 0, buf.Length);

			string old = demo.Header.GameDirectory;
			if (old == newDir) {
				// no difference
				Write(demo.Reader.Data);
				return;
			}
			int lenDiff = newDir.Length - old.Length;
			BitStreamReader bsr = demo.Reader;
			byte[] dirBytes = Encoding.ASCII.GetBytes(newDir);

			Write(bsr.ReadBytes(796));
			Write(dirBytes); // header doesn't matter but I change it anyway
			Write(new byte[260 - newDir.Length]);
			bsr.SkipBytes(260);
			Write(bsr.ReadBytes(12));
			byte[] tmp = BitConverter.GetBytes((uint)(bsr.ReadUInt() + lenDiff));
			if (!BitConverter.IsLittleEndian)
				tmp = tmp.Reverse().ToArray();
			Write(tmp);

			foreach (SignOn signOn in demo.FilterForPacket<SignOn>().Where(signOn => signOn.FilterForMessage<SvcServerInfo>().Any())) {
				// catch up to signOn packet
				int byteCount = (signOn.Reader.AbsoluteBitIndex - bsr.AbsoluteBitIndex) / 8;
				Write(bsr.ReadBytes(byteCount));
				bsr.SkipBits(signOn.Reader.BitLength);

				BitStreamWriter bsw = new BitStreamWriter();
				BitStreamReader signOnReader = signOn.Reader;
				bsw.WriteBits(signOnReader.ReadRemainingBits());
				signOnReader = signOnReader.FromBeginning();
				int bytesToMessageStreamSize = demo.DemoInfo.MaxSplitscreenPlayers * 76 + 8;
				signOnReader.SkipBytes(bytesToMessageStreamSize);
				// edit the message stream length - read uint, and edit at index before the reading of said uint
				bsw.EditIntAtIndex((int)(signOnReader.ReadUInt() + lenDiff), signOnReader.CurrentBitIndex - 32, 32);

				// actually change the game dir
				SvcServerInfo serverInfo = signOn.FilterForMessage<SvcServerInfo>().Single();
				int editIndex = serverInfo.GameDirBitIndex - signOn.Reader.AbsoluteBitIndex;
				bsw.RemoveBitsAtIndex(editIndex, old.Length * 8);
				bsw.InsertBitsAtIndex(dirBytes, editIndex, newDir.Length * 8);
				Write(bsw.AsArray);
			}
			Write(bsr.ReadRemainingBits().bytes);
		}


		protected override void PostProcess(DemoParsingInfo infoObj, string arg, bool isDefault) {}
	}
}
