using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Messages.UserMessages;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	// similar to a packet frame, contains the type of user message
	public class SvcUserMessageFrame : DemoMessage {

		public UserMessageType UserMessageType;
		public SvcUserMessage SvcUserMessage;
		

		public SvcUserMessageFrame(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}


		internal override void ParseStream(BitStreamReader bsr) {
			byte typeVal = bsr.ReadByte();
			UserMessageType = SvcUserMessage.ByteToUserMessageType(DemoRef, typeVal);
			uint messageLength = bsr.ReadBitsAsUInt(DemoRef.DemoSettings.UserMessageLengthBits);
			// since i pass in a substream, i don't want to call SetLocalStreamEnd() in any of the user messages
			SvcUserMessage = SvcUserMessageFactory.CreateUserMessage(
				DemoRef, bsr.SubStream(messageLength),UserMessageType);
			// this is pretty wacky and probs could be improved - I want to log an error if the user message is the incorrect size
			// (i'm pretty sure this is an indicator that i'm parsing the wrong type of user message)
			// if the message is unknown, then i've already logged this
			bool correctSize = SvcUserMessage is UnknownSvcUserMessage;
			if (!correctSize) {
				try {
					correctSize = SvcUserMessage.ParseOwnStream() == 0; // if there are no bits remaining after parsing, then the size is correct
				} catch (Exception) {
					correctSize = false; // catch exception if it went oob, clearly the wrong size
				}
			}

			if (!correctSize) { // if parsing fails, just convert to an unknown type - the byte array that it will print is still much more useful
				string s = $"{GetType().Name}, {UserMessageType} ({typeVal}) parsed incorrectly, ({SvcUserMessage.Reader.BitLength / 8} bytes)";
				DemoRef.AddError($"{s} - {SvcUserMessage.Reader.ToHexString()}");
				SvcUserMessage = new UnknownSvcUserMessage(DemoRef, SvcUserMessage.Reader);
				SvcUserMessage.ParseOwnStream();
			}

			bsr.SkipBits(messageLength);
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.Append(Enum.IsDefined(typeof(UserMessageType), UserMessageType)
				? UserMessageType.ToString()
				: "Unknown");
			iw.Append($" ({SvcUserMessage.UserMessageTypeToByte(DemoRef, UserMessageType)})");
			if (SvcUserMessage.MayContainData) {
				iw.AddIndent();
				iw.AppendLine();
				SvcUserMessage.AppendToWriter(iw);
				iw.SubIndent();
			}
		}
	}
}