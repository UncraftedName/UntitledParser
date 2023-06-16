using System;
using System.Collections.Generic;
using System.Linq;
using DemoParser.Parser.Components.Messages;

namespace DemoParser.Parser.Components.Abstract {

	/// <summary>
	/// A 'sub-packet' in the Packet or SignOn packets.
	/// </summary>
	public abstract class DemoMessage : DemoComponent {

		public readonly byte Value; // for debugging so we can determine if this matches what we expect


		protected DemoMessage(SourceDemo? demoRef, byte value) : base(demoRef) {
			Value = value;
		}

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

		// l4d1 1.0 is missing the last 2 messages compared to NewProtocolMessageList
		// l4d1 steam is only missing the last one
		public static readonly IReadOnlyList<MessageType> L4D1OldMessageList = new[] {
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
			MessageType.SvcGetCvarValue
		};

		public static readonly IReadOnlyList<MessageType> L4D1SteamMessageList =
			L4D1OldMessageList.Concat(new[] { MessageType.SvcCmdKeyValues }).ToArray();

		public static readonly IReadOnlyList<MessageType> NewProtocolMessageList =
			L4D1SteamMessageList.Concat(new[] { MessageType.SvcPaintmapData }).ToArray();

		public static readonly IReadOnlyList<MessageType> SteamPipeMessageList =
            OldProtocolMessageList.Concat(new[] { MessageType.SvcCmdKeyValues }).ToArray();

        #endregion


        public static MessageType ByteToSvcMessageType(byte b, DemoInfo demoInfo) {
			var tab = demoInfo.MessageTypes;
			if (tab == null)
				return MessageType.Unknown;
			else if (b >= tab.Count)
				return MessageType.Invalid;
			else
				return tab[b];
		}


		public static byte MessageTypeToByte(MessageType m, DemoInfo demoInfo) {
			if (demoInfo.MessageTypesReverseLookup.TryGetValue(m, out int i))
				return (byte)i;
			throw new ArgumentException($"no message found for {m}");
		}
	}


	public static class MessageFactory {

		public static DemoMessage? CreateMessage(SourceDemo? dRef, MessageType messageType, byte val) {
			return messageType switch {
				MessageType.NetNop               => new NetNop              (dRef, val),
				MessageType.NetDisconnect        => new NetDisconnect       (dRef, val),
				MessageType.NetFile              => new NetFile             (dRef, val),
				MessageType.NetSplitScreenUser   => new NetSplitScreenUser  (dRef, val),
				MessageType.NetTick              => new NetTick             (dRef, val),
				MessageType.NetStringCmd         => new NetStringCmd        (dRef, val),
				MessageType.NetSetConVar         => new NetSetConVar        (dRef, val),
				MessageType.NetSignOnState       => new NetSignOnState      (dRef, val),
				MessageType.SvcServerInfo        => new SvcServerInfo       (dRef, val),
				MessageType.SvcSendTable         => new SvcSendTable        (dRef, val),
				MessageType.SvcClassInfo         => new SvcClassInfo        (dRef, val),
				MessageType.SvcSetPause          => new SvcSetPause         (dRef, val),
				MessageType.SvcCreateStringTable => new SvcCreateStringTable(dRef, val),
				MessageType.SvcUpdateStringTable => new SvcUpdateStringTable(dRef, val),
				MessageType.SvcVoiceInit         => new SvcVoiceInit        (dRef, val),
				MessageType.SvcPrint             => new SvcPrint            (dRef, val),
				MessageType.SvcSounds            => new SvcSounds           (dRef, val),
				MessageType.SvcSetView           => new SvcSetView          (dRef, val),
				MessageType.SvcFixAngle          => new SvcFixAngle         (dRef, val),
				MessageType.SvcCrosshairAngle    => new SvcCrosshairAngle   (dRef, val),
				MessageType.SvcBspDecal          => new SvcBspDecal         (dRef, val),
				MessageType.SvcUserMessage       => new SvcUserMessage      (dRef, val),
				MessageType.SvcEntityMessage     => new SvcEntityMessage    (dRef, val),
				MessageType.SvcGameEvent         => new SvcGameEvent        (dRef, val),
				MessageType.SvcPacketEntities    => new SvcPacketEntities   (dRef, val),
				MessageType.SvcTempEntities      => new SvcTempEntities     (dRef, val),
				MessageType.SvcPrefetch          => new SvcPrefetch         (dRef, val),
				MessageType.SvcMenu              => new SvcMenu             (dRef, val),
				MessageType.SvcGameEventList     => new SvcGameEventList    (dRef, val),
				MessageType.SvcGetCvarValue      => new SvcGetCvarValue     (dRef, val),
				MessageType.SvcCmdKeyValues      => new SvcCmdKeyValues     (dRef, val),
				MessageType.SvcPaintmapData      => new SvcPaintMapData     (dRef, val),
				MessageType.SvcSplitScreen       => new SvcSplitScreen      (dRef, val),
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
}
