using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;
using static DemoParser.Parser.Components.Abstract.MessageType;

namespace DemoParser.Parser.Components {
	
	/// <summary>
	/// A special class to handle parsing an arbitrary amount of consecutive net/svc messages.
	/// </summary>
	public class MessageStream : DemoComponent, IEnumerable<(MessageType messageType, DemoMessage message)> {
		
		public List<(MessageType messageType, DemoMessage? message)> Messages;
		
		public static implicit operator List<(MessageType messageType, DemoMessage? message)>(MessageStream m) => m.Messages;
		
		
		public MessageStream(SourceDemo? demoRef) : base(demoRef) {}
		
		
		// this starts on the int before the messages which says the size of the message stream in bytes
		protected override void Parse(ref BitStreamReader bsr) {
			uint messagesByteLength = bsr.ReadUInt();
			BitStreamReader messageBsr = bsr.SplitAndSkip(messagesByteLength << 3);
			Messages = new List<(MessageType, DemoMessage?)>();
			byte messageValue = 0;
			MessageType messageType = Unknown;
			Exception? e = null;
			try {
				do {
					messageValue = (byte)messageBsr.ReadBitsAsUInt(DemoInfo.NetMsgTypeBits);
					messageType = DemoMessage.ByteToSvcMessageType(messageValue, DemoInfo);
					DemoMessage? demoMessage = MessageFactory.CreateMessage(DemoRef,messageType);
					demoMessage?.ParseStream(ref messageBsr);
					Messages.Add((messageType, demoMessage));
				} while (Messages[^1].message != null && messageBsr.BitsRemaining >= DemoInfo.NetMsgTypeBits);
			} catch (Exception ex) {
				e = ex;
				Debug.WriteLine(e);
				// if the stream goes out of bounds, that's not a big deal since the messages are skipped over at the end anyway
				(MessageType, DemoMessage?) pair = (messageType, null);
				Messages.Add(pair);
			}
			
			#region error logging
			
			MessageType lastType = Messages[^1].messageType;
			DemoMessage? lastMessage = Messages[^1].message;
			
			if (e != null
				|| !Enum.IsDefined(typeof(MessageType), lastType)
				|| lastType == Unknown
				|| lastType == Invalid
				|| lastMessage == null) 
			{
				var lastNonNopMessage = Messages.FindLast(tuple => tuple.messageType != NetNop && tuple.message != null).messageType;
				lastNonNopMessage = lastNonNopMessage == NetNop ? Unknown : lastNonNopMessage;
				string errorStr = "error while parsing message stream, " +
								  $"{(Messages.Count > 1 ? $"last non-nop message: {lastNonNopMessage}," : "first message,")} ";
				errorStr += $"{messageBsr.BitsRemaining} bit{(messageBsr.BitsRemaining == 1 ? "" : "s")} left to read, ";
				if (e != null) {
					errorStr += $"exception when parsing {lastType}";
					errorStr += $"\n\texception: {e.Message}";
				} else if (!Enum.IsDefined(typeof(MessageType), lastType) || lastType == Unknown || lastType == Invalid) {
					errorStr += $"unknown message value: {messageValue}";
				} else {
					errorStr += $"unimplemented message type - {Messages[^1].messageType}";
				}

				DemoRef.LogError(errorStr);
			}
			
			#endregion
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter iw) {
			int i = 0;
			while (i < Messages?.Count && Messages[i].message != null) {
				iw.Append($"message: {Messages[i].messageType} " +
						  $"({DemoMessage.MessageTypeToByte(Messages[i].messageType, DemoInfo)})");
				if (Messages[i].message.MayContainData) {
					iw.FutureIndent++;
					iw.AppendLine();
					Messages[i].message.PrettyWrite(iw);
					iw.FutureIndent--;
				}
				if (i != Messages.Count - 1)
					iw.AppendLine();
				i++;
			}
			if (i < Messages?.Count) {
				iw.Append("more messages remaining... ");
				iw.Append(Enum.IsDefined(typeof(MessageType), Messages[i].messageType)
					? $"type: {Messages[i].messageType}"
					: $"unknown type: {Messages[i].messageType}");
			}
		}
		

		public IEnumerator<(MessageType, DemoMessage)> GetEnumerator() {
			return Messages.GetEnumerator();
		}
		

		IEnumerator IEnumerable.GetEnumerator() {
			return ((IEnumerable)Messages).GetEnumerator();
		}
	}
}