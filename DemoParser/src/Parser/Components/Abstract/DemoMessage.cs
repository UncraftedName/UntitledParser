using System;
using System.Collections.Generic;
using System.Linq;
using DemoParser.Parser.Components.Messages;

namespace DemoParser.Parser.Components.Abstract {
	
	/// <summary>
	/// A 'sub-packet' in the Packet or SignOn packets.
	/// </summary>
	public abstract class DemoMessage : DemoComponent {

		protected DemoMessage(SourceDemo? demoRef) : base(demoRef) {}

		#region SVC/Net message lists for different versions

		public static readonly IReadOnlyList<MessageType> OeMessageList = new[] {
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
			MessageType.SvcTerrainMod,
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
		
		// same as oe but without terrain mod
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

		public static DemoMessage? CreateMessage(SourceDemo? demoRef, MessageType messageType) {
			return messageType switch {
				MessageType.NetNop               => new NetNop(demoRef),
				MessageType.NetDisconnect        => new NetDisconnect(demoRef),
				MessageType.NetFile              => new NetFile(demoRef),
				MessageType.NetSplitScreenUser   => new NetSplitScreenUser(demoRef),
				MessageType.NetTick              => new NetTick(demoRef),
				MessageType.NetStringCmd         => new NetStringCmd(demoRef),
				MessageType.NetSetConVar         => new NetSetConVar(demoRef),
				MessageType.NetSignOnState       => new NetSignOnState(demoRef),
				MessageType.SvcServerInfo        => new SvcServerInfo(demoRef),
				MessageType.SvcSendTable         => new SvcSendTable(demoRef),
				MessageType.SvcClassInfo         => new SvcClassInfo(demoRef),
				MessageType.SvcSetPause          => new SvcSetPause(demoRef),
				MessageType.SvcCreateStringTable => new SvcCreateStringTable(demoRef),
				MessageType.SvcUpdateStringTable => new SvcUpdateStringTable(demoRef),
				MessageType.SvcVoiceInit         => new SvcVoiceInit(demoRef),
				MessageType.SvcPrint             => new SvcPrint(demoRef),
				MessageType.SvcSounds            => new SvcSounds(demoRef),
				MessageType.SvcSetView           => new SvcSetView(demoRef),
				MessageType.SvcFixAngle          => new SvcFixAngle(demoRef),
				MessageType.SvcCrosshairAngle    => new SvcCrosshairAngle(demoRef),
				MessageType.SvcBspDecal          => new SvcBspDecal(demoRef),
				MessageType.SvcUserMessage       => new SvcUserMessage(demoRef),
				MessageType.SvcEntityMessage     => new SvcEntityMessage(demoRef),
				MessageType.SvcGameEvent         => new SvcGameEvent(demoRef),
				MessageType.SvcPacketEntities    => new SvcPacketEntities(demoRef),
				MessageType.SvcTempEntities      => new SvcTempEntities(demoRef),
				MessageType.SvcPrefetch          => new SvcPrefetch(demoRef),
				MessageType.SvcMenu              => new SvcMenu(demoRef),
				MessageType.SvcGameEventList     => new SvcGameEventList(demoRef),
				MessageType.SvcGetCvarValue      => new SvcGetCvarValue(demoRef),
				MessageType.SvcCmdKeyValues      => new SvcCmdKeyValues(demoRef),
				MessageType.SvcPaintmapData      => new SvcPaintMapData(demoRef),
				MessageType.SvcSplitScreen       => new SvcSplitScreen(demoRef),
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
		SvcTerrainMod
	}
	

	public interface IContainsMessageStream {
		MessageStream MessageStream {get;set;}
	}
}