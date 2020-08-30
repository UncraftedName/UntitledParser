using System;
using System.Collections.Generic;
using System.Linq;
using DemoParser.Parser.Components.Messages;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Abstract {
	
	/// <summary>
	/// A 'sub-packet' in the Packet or SignOn packets.
	/// </summary>
	public abstract class DemoMessage : DemoComponent {

		protected DemoMessage(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}

		#region SVC/Net message lists for different versions

		public static readonly IReadOnlyList<MessageType> OldProtocolMessageList = new[] {
			MessageType.NetNop,
			MessageType.NetDisconnect,
			MessageType.NetFile,
			MessageType.NetTick,
			MessageType.NetStringCmd,
			MessageType.NetSetConVar,
			MessageType.NetSignOnState,
			MessageType.SvcPrint,
			MessageType.SvcServerInfo,
			MessageType.SvcSendTable,
			MessageType.SvcClassInfo,
			MessageType.SvcSetPause,
			MessageType.SvcCreateStringTable,
			MessageType.SvcUpdateStringTable,
			MessageType.SvcVoiceInit,
			MessageType.SvcVoiceData,
			MessageType.Invalid, // would be svc_HLTV
			MessageType.SvcSounds,
			MessageType.SvcSetView,
			MessageType.SvcFixAngle,
			MessageType.SvcCrosshairAngle,
			MessageType.SvcBspDecal,
			MessageType.Invalid, // would be svc_TerrainMod
			MessageType.SvcUserMessage,
			MessageType.SvcEntityMessage,
			MessageType.SvcGameEvent,
			MessageType.SvcPacketEntities,
			MessageType.SvcTempEntities,
			MessageType.SvcPrefetch,
			MessageType.SvcMenu,
			MessageType.SvcGameEventList,
			MessageType.SvcGetCvarValue
		};

		public static readonly IReadOnlyList<MessageType> NewProtocolMessageList = new[] {
			MessageType.NetNop,
			MessageType.NetDisconnect,
			MessageType.NetFile,
			MessageType.NetSplitScreenUser,
			MessageType.NetTick,
			MessageType.NetStringCmd,
			MessageType.NetSetConVar,
			MessageType.NetSignOnState,
			MessageType.SvcServerInfo,
			MessageType.SvcSendTable,
			MessageType.SvcClassInfo,
			MessageType.SvcSetPause,
			MessageType.SvcCreateStringTable,
			MessageType.SvcUpdateStringTable,
			MessageType.SvcVoiceInit,
			MessageType.SvcVoiceData,
			MessageType.SvcPrint,
			MessageType.SvcSounds,
			MessageType.SvcSetView,
			MessageType.SvcFixAngle,
			MessageType.SvcCrosshairAngle,
			MessageType.SvcBspDecal,
			MessageType.SvcSplitScreen,
			MessageType.SvcUserMessage,
			MessageType.SvcEntityMessage,
			MessageType.SvcGameEvent,
			MessageType.SvcPacketEntities,
			MessageType.SvcTempEntities,
			MessageType.SvcPrefetch,
			MessageType.SvcMenu,
			MessageType.SvcGameEventList,
			MessageType.SvcGetCvarValue,
			MessageType.SvcCmdKeyValues,
			MessageType.SvcPaintmapData
		};

		public static readonly IReadOnlyList<MessageType> SteamPipeMessageList =
			OldProtocolMessageList.Concat(new [] {MessageType.SvcCmdKeyValues}).ToArray();

		#endregion


		public static MessageType ByteToSvcMessageType(byte b, DemoSettings demoSettings) {
			var tab = demoSettings.MessageTypes;
			if (tab == null)
				return MessageType.Unknown;
			else if (b > tab.Count)
				return MessageType.Invalid;
			else
				return tab[b];
		}


		public static byte MessageTypeToByte(MessageType m, DemoSettings demoSettings) {
			if (demoSettings.MessageTypesReverseLookup.TryGetValue(m, out int i))
				return (byte)i;
			throw new ArgumentException($"no message found for {m}");
		}
	}


	public static class MessageFactory {

		public static DemoMessage? CreateMessage(SourceDemo demoRef, BitStreamReader reader, MessageType messageType) {
			return messageType switch {
				MessageType.NetNop               => new NetNop(demoRef, reader),
				MessageType.NetDisconnect        => new NetDisconnect(demoRef, reader),
				MessageType.NetFile              => new NetFile(demoRef, reader),
				MessageType.NetSplitScreenUser   => new NetSplitScreenUser(demoRef, reader),
				MessageType.NetTick              => new NetTick(demoRef, reader),
				MessageType.NetStringCmd         => new NetStringCmd(demoRef, reader),
				MessageType.NetSetConVar         => new NetSetConVar(demoRef, reader),
				MessageType.NetSignOnState       => new NetSignOnState(demoRef, reader),
				MessageType.SvcServerInfo        => new SvcServerInfo(demoRef, reader),
				MessageType.SvcSendTable         => new SvcSendTable(demoRef, reader),
				MessageType.SvcClassInfo         => new SvcClassInfo(demoRef, reader),
				MessageType.SvcSetPause          => new SvcSetPause(demoRef, reader),
				MessageType.SvcCreateStringTable => new SvcCreateStringTable(demoRef, reader),
				MessageType.SvcUpdateStringTable => new SvcUpdateStringTable(demoRef, reader),
				MessageType.SvcVoiceInit         => new SvcVoiceInit(demoRef, reader),
				MessageType.SvcPrint             => new SvcPrint(demoRef, reader),
				MessageType.SvcSounds            => new SvcSounds(demoRef, reader),
				MessageType.SvcSetView           => new SvcSetView(demoRef, reader),
				MessageType.SvcFixAngle          => new SvcFixAngle(demoRef, reader),
				MessageType.SvcCrosshairAngle    => new SvcCrosshairAngle(demoRef, reader),
				MessageType.SvcBspDecal          => new SvcBspDecal(demoRef, reader),
				MessageType.SvcUserMessage       => new SvcUserMessage(demoRef, reader),
				MessageType.SvcEntityMessage     => new SvcEntityMessage(demoRef, reader),
				MessageType.SvcGameEvent         => new SvcGameEvent(demoRef, reader),
				MessageType.SvcPacketEntities    => new SvcPacketEntities(demoRef, reader),
				MessageType.SvcTempEntities      => new SvcTempEntities(demoRef, reader),
				MessageType.SvcPrefetch          => new SvcPrefetch(demoRef, reader),
				MessageType.SvcMenu              => new SvcMenu(demoRef, reader),
				MessageType.SvcGameEventList     => new SvcGameEventList(demoRef, reader),
				MessageType.SvcGetCvarValue      => new SvcGetCvarValue(demoRef, reader),
				MessageType.SvcCmdKeyValues      => new SvcCmdKeyValues(demoRef, reader),
				MessageType.SvcPaintmapData      => new SvcPaintMapData(demoRef, reader),
				MessageType.SvcSplitScreen       => new SvcSplitScreen(demoRef, reader),
				_ => null
			};
		}
	}
	
	
	public enum MessageType {
		Unknown,
		Invalid,
		
		NetNop,
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
		SvcUpdateStringTable,
		SvcVoiceInit,
		SvcVoiceData,
		SvcPrint,
		SvcSounds,
		SvcSetView,
		SvcFixAngle,
		SvcCrosshairAngle,
		SvcBspDecal,
		SvcSplitScreen,
		SvcUserMessage, // has a similar function to a packet frame
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
	}
	

	public interface IContainsMessageStream {
		MessageStream MessageStream {get;set;}
	}
}