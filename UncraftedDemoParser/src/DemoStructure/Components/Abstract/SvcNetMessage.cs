namespace UncraftedDemoParser.DemoStructure.Components.Abstract {
	
	// just for organization, doesn't have any additional functionality (for now)
	public abstract class SvcNetMessage : DemoPacket {
		
		protected SvcNetMessage(byte[] data, SourceDemo demoRef, int tick) : base(data, demoRef, tick) {}
	}


	public enum SvcMessageType {
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
		SvcUserMessage,
		SvcEntityMessage,
		SvcGameEvent,
		SvcPacketEntities,
		SvcTempEntities,
		SvcPrefetch,
		SvcMenu,
		SvcGameEventList,
		SvcGetCvadValue,
		SvcCmdKeyValues,
		SvcPaintmapData,
		//SvcEncryptedData,     only used in CS:GO
		//SvcHltvData,
		//SvcBroadcaseCommand,
		//NetPlayerAvatarData
	}
}