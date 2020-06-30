using System;
using DemoParser.Parser.Components.Messages;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Abstract {
	
	/// <summary>
	/// A 'sub-packet' in the Packet or SignOn packets.
	/// </summary>
	public abstract class DemoMessage : DemoComponent {

		protected DemoMessage(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		public static MessageType ByteToSvcMessageType(byte b, DemoSettings demoSettings) {
			if (!demoSettings.NewDemoProtocol) {
				switch (b) {
					case 3:
						return MessageType.NetTick;
					case 4:
						return MessageType.NetStringCmd;
					case 5:
						return MessageType.NetSetConVar;
					case 6:
						return MessageType.NetSignOnState;
					case 7:
						return MessageType.SvcPrint;
					case 16:
						if (demoSettings.Game == SourceGame.PORTAL_1_STEAMPIPE)
							return MessageType.NetSignOnState;
						return MessageType.Unknown;
					case 22:
					case 33:
						return MessageType.Unknown;
				}
			}
			return (MessageType)b;
		}


		public static byte MessageTypeToByte(MessageType messageType, DemoSettings demoSettings) {
			if (!demoSettings.NewDemoProtocol) {
				switch (messageType) {
					case MessageType.NetTick:
						return 3;
					case MessageType.NetStringCmd:
						return 4;
					case MessageType.NetSetConVar:
						return 5;
					case MessageType.NetSignOnState:
						return demoSettings.Game == SourceGame.PORTAL_1_STEAMPIPE ? (byte)16 : (byte)6;
					case MessageType.SvcPrint:
						return 7;
					case MessageType.NetSplitScreenUser:
					case MessageType.SvcSplitScreen:
					case MessageType.SvcPaintmapData:
						throw new ArgumentException($"unknown svc message type: {messageType}");
				}
			}
			return (byte)messageType;
		}
	}


	public static class MessageFactory {

		public static DemoMessage CreateMessage(SourceDemo demoRef, BitStreamReader reader, MessageType messageType) {
			return messageType switch {
				MessageType.NetNop                => new NetNop(demoRef, reader),
				MessageType.NetDisconnect         => new NetDisconnect(demoRef, reader),
				MessageType.NetFile               => new NetFile(demoRef, reader),
				MessageType.NetSplitScreenUser    => new NetSplitScreenUser(demoRef, reader),
				MessageType.NetTick               => new NetTick(demoRef, reader),
				MessageType.NetStringCmd          => new NetStringCmd(demoRef, reader),
				MessageType.NetSetConVar          => new NetSetConVar(demoRef, reader),
				MessageType.NetSignOnState        => new NetSignOnState(demoRef, reader),
				MessageType.SvcServerInfo         => new SvcServerInfo(demoRef, reader),
				MessageType.SvcSendTable          => new SvcSendTable(demoRef, reader),
				MessageType.SvcClassInfo          => new SvcClassInfo(demoRef, reader),
				MessageType.SvcSetPause           => new SvcSetPause(demoRef, reader),
				MessageType.SvcCreateStringTable  => new SvcCreateStringTable(demoRef, reader),
				MessageType.SvcUpdateStringTables => new SvcUpdateStringTables(demoRef, reader),
				MessageType.SvcVoiceInit          => new SvcVoiceInit(demoRef, reader),
				MessageType.SvcPrint              => new SvcPrint(demoRef, reader),
				MessageType.SvcSounds             => new SvcSounds(demoRef, reader),
				MessageType.SvcSetView            => new SvcSetView(demoRef, reader),
				MessageType.SvcFixAngle           => new SvcFixAngle(demoRef, reader),
				MessageType.SvcCrosshairAngle     => new SvcCrosshairAngle(demoRef, reader),
				MessageType.SvcBspDecal           => new SvcBspDecal(demoRef, reader),
				MessageType.SvcUserMessageFrame   => new SvcUserMessageFrame(demoRef, reader),
				MessageType.SvcEntityMessage      => new SvcEntityMessage(demoRef, reader),
				MessageType.SvcGameEvent          => new SvcGameEvent(demoRef, reader),
				MessageType.SvcPacketEntities     => new SvcPacketEntities(demoRef, reader),
				MessageType.SvcTempEntities       => new SvcTempEntities(demoRef, reader),
				MessageType.SvcPrefetch           => new SvcPrefetch(demoRef, reader),
				MessageType.SvcMenu               => new SvcMenu(demoRef, reader),
				MessageType.SvcGameEventList      => new SvcGameEventList(demoRef, reader),
				MessageType.SvcGetCvarValue       => new SvcGetCvarValue(demoRef, reader),
				MessageType.SvcCmdKeyValues       => new SvcCmdKeyValues(demoRef, reader),
				MessageType.SvcPaintmapData       => new SvcPaintMapData(demoRef, reader),
				_ => null
			};
		}
	}
	
	
	public enum MessageType : uint {
		NetNop = 0,
		NetDisconnect,
		NetFile,
		NetSplitScreenUser,
		NetTick,
		NetStringCmd,
		NetSetConVar,
		NetSignOnState,
		SvcServerInfo,
		SvcSendTable,
		SvcClassInfo,
		SvcSetPause,
		SvcCreateStringTable,
		SvcUpdateStringTables,
		SvcVoiceInit,
		SvcVoiceData,
		SvcPrint,
		SvcSounds,
		SvcSetView,
		SvcFixAngle,
		SvcCrosshairAngle,
		SvcBspDecal,
		SvcSplitScreen,
		SvcUserMessageFrame, // I don't think this is called a frame anywhere (in source code), but it has a similar functionality to a packet frame
		SvcEntityMessage,
		SvcGameEvent,
		SvcPacketEntities,
		SvcTempEntities,
		SvcPrefetch,
		SvcMenu,
		SvcGameEventList,
		SvcGetCvarValue,
		SvcCmdKeyValues,
		SvcPaintmapData,
		// SvcEncryptedData,     only used in CS:GO
		// SvcHltvData,
		// SvcBroadcaseCommand,
		// NetPlayerAvatarData
		Unknown = uint.MaxValue
	}
}