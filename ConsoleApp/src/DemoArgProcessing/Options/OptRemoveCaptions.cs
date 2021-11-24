using System.Collections.Immutable;
using System.IO;
using System.Linq;
using DemoParser.Parser;
using DemoParser.Parser.Components;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.Components.Messages.UserMessages;
using DemoParser.Parser.Components.Packets;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace ConsoleApp.DemoArgProcessing.Options {
	
	public class OptRemoveCaptions : DemoOption {
		
		public static readonly ImmutableArray<string> DefaultAliases = new[] {"--remove-captions", "-c"}.ToImmutableArray();
		
		public OptRemoveCaptions() : base(
			DefaultAliases,
			$"Create a new demo without captions {OptOutputFolder.RequiresString}") {}


		public override void AfterParse(DemoParsingSetupInfo setupObj) {
			setupObj.ExecutableOptions++;
			setupObj.FolderOutputRequired = true;
		}


		public override void Process(DemoParsingInfo infoObj) {
			BinaryWriter bw = infoObj.InitBinaryWriter("removing captions", "no-captions", ".dem");
			RemoveCaptions(infoObj.CurrentDemo, bw.BaseStream);
		}


		// writes a edited version of the given demo to the stream
		// this is all manually hacked together and it shall stay this way for now
		public static void RemoveCaptions(SourceDemo demo, Stream s) {
			
			void Write(byte[] buf) => s.Write(buf, 0, buf.Length);

			Packet[] closeCaptionPackets = demo.FilterForPacket<Packet>()
				.Where(packet => packet.FilterForMessage<SvcUserMessage>()
					.Any(frame => frame.MessageType == UserMessageType.CloseCaption)).ToArray();

			if (closeCaptionPackets.Length == 0) {
				Write(demo.Reader.Data);
				return;
			}

			int changedPackets = 0;
			Write(demo.Header.Reader.ReadRemainingBits().bytes);
			
			foreach (PacketFrame frame in demo.Frames) {
				if (frame.Packet != closeCaptionPackets[changedPackets]) {
					Write(frame.Reader.ReadRemainingBits().bytes); // write frames that aren't changed
				} else {
					Packet p = (Packet)frame.Packet;
					BitStreamWriter bsw = new BitStreamWriter(frame.Reader.ByteLength);
					var last = p.MessageStream.Last().message;
					int len = last.Reader.AbsoluteStart - frame.Reader.AbsoluteStart + last.Reader.BitLength;
					bsw.WriteBits(frame.Reader.ReadBits(len), len);
					int msgSizeOffset = p.MessageStream.Reader.AbsoluteStart - frame.Reader.AbsoluteStart;
					int typeInfoLen = demo.DemoInfo.NetMsgTypeBits + demo.DemoInfo.UserMessageLengthBits + 8;
					bsw.RemoveBitsAtIndices(p.FilterForUserMessage<CloseCaption>()
						.Select(caption => (caption.Reader.AbsoluteStart - frame.Reader.AbsoluteStart - typeInfoLen, 
							caption.Reader.BitLength + typeInfoLen)));
					bsw.WriteUntilByteBoundary();
					bsw.EditIntAtIndex((bsw.BitLength - msgSizeOffset - 32) >> 3, msgSizeOffset, 32);
					Write(bsw.AsArray);
					
					// if we've edited all the packets, write the rest of the data in the demo
					if (++changedPackets == closeCaptionPackets.Length) {
						BitStreamReader tmp = demo.Reader;
						tmp.SkipBits(frame.Reader.AbsoluteStart + frame.Reader.BitLength);
						Write(tmp.ReadRemainingBits().bytes);
						break;
					}
				}
			}
		}


		public override void PostProcess(DemoParsingInfo infoObj) {}
	}
}
