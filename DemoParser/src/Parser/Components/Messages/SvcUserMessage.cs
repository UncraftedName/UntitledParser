using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Messages.UserMessages;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {

	// a frame for user messages
	public sealed class SvcUserMessage : DemoMessage {

		public UserMessageType MessageType;
		public UserMessage UserMessage;
		private bool _unimplemented;


		public SvcUserMessage(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		// In case of an error, we log the hex string and so does the "unknown" type. This speeds up implementations.
		protected override void Parse(ref BitStreamReader bsr) {
			byte b = bsr.ReadByte();
			MessageType = UserMessage.ByteToUserMessageType(DemoInfo, b);
			var uBsr = bsr.SplitAndSkip((int)bsr.ReadUInt(DemoInfo.UserMessageLengthBits));
			string? errorStr = null;

			switch (MessageType) {
				case UserMessageType.Unknown:
					errorStr = $"{GetType().Name}: no lookup list for this game";
					break;
				case UserMessageType.Invalid:
					errorStr = $"{GetType().Name}: bad message type {b}";
					break;
				default:
					if ((UserMessage = SvcUserMessageFactory.CreateUserMessage(DemoRef, MessageType, b)!) == null) {
						errorStr = $"{GetType().Name}: unimplemented message type '{MessageType}'";
						_unimplemented = true;
					} else if (UserMessage.ParseStream(uBsr) != 0) {
						errorStr = $"{GetType().Name}: {MessageType} probably didn't parse correctly";
					}
					break;
			}
			if (errorStr != null) {
				DemoRef.LogError($"{errorStr}; {uBsr.FromBeginning().ToHexString()}");
				UserMessage = new UnknownUserMessage(DemoRef, b); // this'll just print the hex string
				UserMessage.ParseStream(uBsr.FromBeginning());
			}
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			if (UserMessage is UnknownUserMessage)
				pw.Append(_unimplemented ? "(unimplemented) " : "(not parsed correctly) ");
			pw.Append($"{MessageType} ({UserMessage.Value})");
			if (UserMessage.MayContainData) {
				pw.FutureIndent++;
				pw.AppendLine();
				UserMessage.PrettyWrite(pw);
				pw.FutureIndent--;
			}
		}
	}
}
