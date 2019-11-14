using System;
using System.Text;
using UncraftedDemoParser.Utils;

// stealing code xd
// https://github.com/NeKzor/sdp.js/blob/master/src/types/NetMessages.js

namespace UncraftedDemoParser.Parser.Components.Abstract {
	
	// just for organization, doesn't have any additional functionality (for now)
	public abstract class SvcNetMessage : DemoPacket {

		protected SvcNetMessage(byte[] data, SourceDemo demoRef, int tick) : base(data, demoRef, tick) {}


		protected override void ParseBytes() {
			ParseBytes(new BitFieldReader(Bytes));
		}


		// all svc/net messages will use the field reader
		protected abstract void ParseBytes(BitFieldReader bfr);


		protected virtual void PopulatedBuilder(StringBuilder builder) {} // todo make abstract once all tostrings are implemented


		public sealed override string ToString() {
			StringBuilder builder = new StringBuilder();
			PopulatedBuilder(builder);
			if (builder.Length == 0)
				return $"\t\t{base.ToString()}";
			return builder.ToString();
		}
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
		Unknown = Int32.MinValue
	}
}