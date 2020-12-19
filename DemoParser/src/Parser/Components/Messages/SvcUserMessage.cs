using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Messages.UserMessages;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	// similar to a packet frame, contains the type of user message
	public sealed class SvcUserMessage : DemoMessage {

		public UserMessageType MessageType;
		public UserMessage UserMessage;
		private bool _unimplemented = false;
		

		public SvcUserMessage(SourceDemo? demoRef) : base(demoRef) {}


		/* Okay, this is pretty wacky. First I read a byte, and based off of that I try to determine the type of
		 * user message. If I don't have a lookup list for whatever game this is or the type seems bogus, I log an
		 * error. Otherwise, create the message instance, and if it's not empty, try to parse it. If parsing fails,
		 * log an error. Finally, if not all bits of the message are parsed, then it's likely that I did something
		 * wrong, (since it seems like the user messages use up all the bits in the message) so log an error.
		 */
		protected override void Parse(ref BitStreamReader bsr) {
			byte typeVal = bsr.ReadByte();
			MessageType = UserMessage.ByteToUserMessageType(DemoSettings, typeVal);
			uint messageLength = bsr.ReadBitsAsUInt(DemoSettings.UserMessageLengthBits);
			string? errorStr = null;

			var uMessageReader = bsr.SplitAndSkip(messageLength);
			
			switch (MessageType) {
				case UserMessageType.Unknown:
					errorStr = $"There is no SvcUserMessage list for this game, type {typeVal} was found";
					break;
				case UserMessageType.Invalid:
					errorStr = $"SvcUserMessage with value {typeVal} is invalid";
					break;
				default:
					UserMessage = SvcUserMessageFactory.CreateUserMessage(DemoRef, MessageType)!;
					if (UserMessage == null) {
						errorStr = $"Unimplemented SvcUserMessage: {MessageType}";
						_unimplemented = true;
					} else {
						try { // empty messages might still have 1-2 bytes, might need to do something 'bout that
							if (UserMessage.ParseStream(uMessageReader) != 0)
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
				UserMessage = new UnknownUserMessage(DemoRef);
				UserMessage.ParseStream(uMessageReader);
			}

			#endregion
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IIndentedWriter iw) {
			if (MessageType == UserMessageType.Unknown || MessageType == UserMessageType.Invalid) {
				iw.Append("Unknown type");
			} else {
				if (UserMessage is UnknownUserMessage)
					iw.Append(_unimplemented ?  "(unimplemented) " : "(not parsed properly) ");
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