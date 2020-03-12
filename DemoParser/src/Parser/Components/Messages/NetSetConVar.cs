using System;
using System.Collections.Generic;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	public class NetSetConVar : DemoMessage {

		public List<(string, string)> ConVars;


		public NetSetConVar(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}


		internal override void ParseStream(BitStreamReader bsr) {
			//ConVars = new List<(CharArray, CharArray)>();
			byte count = bsr.ReadByte();
			ConVars = new List<(string, string)>(count);
			for (int i = 0; i < count; i++)
				ConVars.Add((bsr.ReadNullTerminatedString(), bsr.ReadNullTerminatedString()));
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			for (int i = 0; i < ConVars.Count; i++) {
				iw.Append($"{{name: {ConVars[i].Item1}, value: {ConVars[i].Item2}}}");
				if (i != ConVars.Count - 1)
					iw.AppendLine();
			}
		}
	}


	[Flags]
	public enum CVarFlags { // src_main/public/tier1/iconvar.h
		None 				= 0,
		Unregistered 		= 1, 	   // If this is set, don't add to linked list, etc.
		DevelopmentOnly		= 1 << 1,  // Hidden in released products. Flag is removed automatically if ALLOW_DEVELOPMENT_CVARS is defined.
		GameDll 			= 1 << 2,  // defined by the game DLL
		ClientDll			= 1 << 3,  // defined by the client DLL
		Hidden				= 1 << 4,  // Hidden. Doesn't appear in find or autocomplete. Like DevelopmentOnly, but can't be compiled out.
		Protected			= 1 << 5,  // It's a server cvar, but we don't send the data since it's a password, etc.  Sends 1 if it's not bland/zero, 0 otherwise as value
		Sponly				= 1 << 6,  // This cvar cannot be changed by clients connected to a multiplayer server.
		Archive				= 1 << 7,  // set to cause it to be saved to vars.rc
		Notify				= 1 << 8,  // notifies players when changed
		UserInfo			= 1 << 9,  // changes the client's info string
		PrintableOnly		= 1 << 10, // This cvar's string cannot contain unprintable characters ( e.g., used for player name etc ). 
		Unlogged			= 1 << 11, // If this is a FCVAR_SERVER, don't log changes to the log file / console if we are creating a log
		NeverAsString		= 1 << 12, // never try to print that cvar
		Replicated			= 1 << 13, // server setting enforced on clients -- might be called FCVAR_SERVER
		Cheat				= 1 << 14, // Only usable in singleplayer / debug / multiplayer & sv_cheats
		Demo				= 1 << 16, // record this cvar when starting a demo file
		DontRecord			= 1 << 17, // don't record these command in demofiles
		NotConnected		= 1 << 22, // cvar cannot be changed by a client that is connected to a server
		ArchiveXbox			= 1 << 24, // cvar written to config.cfg on the Xbox
		ServerCanExecute	= 1 << 28, // the server is allowed to execute this command on clients via ClientCommand/NET_StringCmd/CBaseClientState::ProcessStringCmd.
		ServerCannotQuery	= 1 << 29, // If this is set, then the server is not allowed to query this cvar's value (via IServerPluginHelpers::StartQueryCvarValue).
		ClientCmdCanExecute	= 1 << 30  // IVEngineClient::ClientCmd is allowed to execute this command. Note: IVEngineClient::ClientCmd_Unrestricted can run any client command.
	}
}