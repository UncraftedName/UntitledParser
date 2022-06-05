using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Messages;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;
using static DemoParser.Parser.Components.Abstract.MessageType;

namespace DemoParser.Parser.Components {

	/// <summary>
	/// A special class to handle parsing an arbitrary amount of consecutive net/svc messages.
	/// </summary>
	public class MessageStream : DemoComponent, IEnumerable<DemoMessage> {

		public List<DemoMessage> Messages;

		public bool ParseSuccess;
		private MessageType _lastMsgType;

		public static implicit operator List<DemoMessage>(MessageStream m) => m.Messages;


		public MessageStream(SourceDemo? demoRef) : base(demoRef) {}


		// this starts on the int before the messages which says the size of the message stream in bytes
		protected override void Parse(ref BitStreamReader bsr) {
			BitStreamReader mBsr = bsr.ForkAndSkip((int)(bsr.ReadUInt() * 8));
			Messages = new List<DemoMessage>();
			byte messageValue;
			DemoMessage? demoMessage;
			do {
				messageValue = (byte)mBsr.ReadUInt(DemoInfo.NetMsgTypeBits);
				_lastMsgType = DemoMessage.ByteToSvcMessageType(messageValue, DemoInfo);
				demoMessage = MessageFactory.CreateMessage(DemoRef, _lastMsgType, messageValue);
				if (demoMessage == null)
					break;
				demoMessage.ParseStream(ref mBsr);
				if (mBsr.HasOverflowed)
					break;
				Messages.Add(demoMessage);
			} while (mBsr.BitsRemaining >= DemoInfo.NetMsgTypeBits);

			#region error logging

			if (demoMessage == null || mBsr.HasOverflowed) {
				DemoMessage? nonNopMsg = Messages.LastOrDefault(message => message.GetType() != typeof(NetNop));
				string errorStr = $"{GetType().Name}: {(nonNopMsg == null ? "first message," : $"last non-nop message is {nonNopMsg.GetType().Name},")} ";

				if (mBsr.HasOverflowed)
					errorStr += $"reader overflowed while parsing {_lastMsgType}";
				else if (!Enum.IsDefined(typeof(MessageType), _lastMsgType) || _lastMsgType == Unknown || _lastMsgType == Invalid)
					errorStr += $"got an unknown message value {messageValue}";
				else
					errorStr += $"got an unimplemented message type '{_lastMsgType}'";

				DemoRef.LogError(errorStr);
			} else {
				ParseSuccess = true;
			}

			#endregion
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			for (int i = 0; i < Messages.Count; i++) {
				var message = Messages[i];
				pw.Append($"message: {message.GetType().Name} ({message.Value})");
				if (message.MayContainData) {
					pw.FutureIndent++;
					pw.AppendLine();
					message.PrettyWrite(pw);
					pw.FutureIndent--;
				}
				if (i != Messages.Count - 1)
					pw.AppendLine();
			}
			if (!ParseSuccess)
				pw.Append($"\nmore messages remaining... type: {_lastMsgType}");
		}


		public IEnumerator<DemoMessage> GetEnumerator() => Messages.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Messages).GetEnumerator();
	}
}
