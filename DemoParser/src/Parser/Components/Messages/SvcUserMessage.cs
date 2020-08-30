using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Messages.UserMessages;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	// similar to a packet frame, contains the type of user message
	public class SvcUserMessage : DemoMessage {

		public UserMessageType MessageType;
		public UserMessage UserMessage;
		

		public SvcUserMessage(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}


		/* Okay, this is pretty wacky. First I read a byte, and based off of that I try to determine the type of
		 * user message. If I don't have a lookup list for whatever game this is or the type seems bogus, I log an
		 * error. Otherwise, create the message instance, and if it's not empty, try to parse it. If parsing fails,
		 * log an error. Finally, if not all bits of the message are parsed, then it's likely that I did something
		 * wrong, (since it seems like the user messages use up all the bits in the message) so log an error.
		 */
		internal override void ParseStream(BitStreamReader bsr) {
			byte typeVal = bsr.ReadByte();
			MessageType = UserMessage.ByteToUserMessageType(DemoSettings, typeVal);
			uint messageLength = bsr.ReadBitsAsUInt(DemoSettings.UserMessageLengthBits);

			var uMessageReader = bsr.SubStream(messageLength);
			string? errorStr = null;
			
			switch (MessageType) {
				case UserMessageType.Unknown:
					errorStr = $"There is no SvcUserMessage list for this game, type {typeVal} was found";
					break;
				case UserMessageType.Invalid:
					errorStr = $"SvcUserMessage with value {typeVal} is invalid";
					break;
				default:
					UserMessage = SvcUserMessageFactory.CreateUserMessage(DemoRef, uMessageReader, MessageType)!;
					if (UserMessage == null) {
						errorStr = $"Unimplemented SvcUserMessage: {MessageType}";
					} else {
						try { // empty messages might still have 1-2 bytes, might need to do something 'bout that
							if (UserMessage.ParseOwnStream() != 0)
								errorStr = $"{GetType().Name} - {MessageType} ({typeVal}) didn't parse all bits";
						} catch (Exception e) {
							errorStr = $"{GetType().Name} - {MessageType} ({typeVal}) " + 
									   $"threw exception during parsing, message: {e.Message}";
						}
					}
					break;
			}

			#region error logging
			
			// if parsing fails, just convert to an unknown type - the byte array that it will print is still useful
			if (errorStr != null) {
				int rem = uMessageReader.BitsRemaining;
				DemoRef.LogError($"{errorStr}, ({rem} bit{(rem == 1 ? "" : "s")}) - " +
								 $"{uMessageReader.FromBeginning().ToHexString()}");
				UserMessage = new UnknownUserMessage(DemoRef, uMessageReader);
			}

			#endregion

			bsr.SkipBits(messageLength);
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			if (MessageType == UserMessageType.Unknown || MessageType == UserMessageType.Invalid) {
				iw.Append("Unknown type");
			} else {
				if (UserMessage is UnknownUserMessage)
					iw.Append("(unimplemented) ");
				iw.Append(MessageType.ToString());
				iw.Append($" ({UserMessage.UserMessageTypeToByte(DemoSettings, MessageType)})");
			}
			if (UserMessage.MayContainData) {
				iw.FutureIndent++;
				iw.AppendLine();
				UserMessage.AppendToWriter(iw);
				iw.FutureIndent--;
			}
		}
	}
}