using ConsoleApp.GenericArgProcessing;
using DemoParser.Parser;
using DemoParser.Parser.Components;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.Components.Packets;
using DemoParser.Parser.Components.Packets.StringTableEntryTypes;
using DemoParser.Parser.EntityStuff;
using DemoParser.Parser.GameState;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ConsoleApp.DemoArgProcessing.Options {

	public class OptOobCheck : DemoOption<DirectoryInfo> {

		public static readonly ImmutableArray<string> DefaultAliases = new[] {"--oob-check", "-O"}.ToImmutableArray();

		private static DirectoryInfo ModDirValidator(string s) {
			try {
				DirectoryInfo d = new(s);
				if (!d.Exists)
					throw new ArgProcessUserException("the given folder does not exist");
				return d;
			} catch (Exception e) {
				throw new ArgProcessUserException($"the given folder is not a valid directory ({e.Message})");
			}
		}

		public OptOobCheck() : base(
			DefaultAliases,
			Arity.ZeroOrOne,
			"Find ticks where the player camera was out of bounds. The given folder can be the game folder, mod folder, or any folder that has maps.",
			"game_dir",
			ModDirValidator,
			new DirectoryInfo(AppContext.BaseDirectory)) {}

		protected override void AfterParse(DemoParsingSetupInfo setupObj, DirectoryInfo d, bool isDefault) {
			setupObj.ExecutableOptions++;
		}

		private class Persist {
			public bool AnyDemosOob = false;
			public bool AnyDemosFailed = false;
			public bool AnyHeaderFailed = false;
			public bool AnyDemosSucceeded = false;
			public SourceMapCache MapCache = new(5);
		}

		private Persist? _persistState = null;
		private Persist PersistState => _persistState ??= new Persist();

		private void ReportTickRangeOobStatus(int start, int end, bool oob) {
			Utils.WriteColor(
				String.Format(
					"[{0}-{1}]: {2}\n",
					start,
					end,
					oob ? "OOB" : "Inbounds"
				),
				oob ? ConsoleColor.Red : ConsoleColor.Green
			);
			PersistState.AnyDemosOob |= oob;
		}

		protected override void Process(DemoParsingInfo infoObj, DirectoryInfo d, bool isDefault) {
			infoObj.PrintOptionMessage("looking for OOB ticks");

			SourceDemo dem = infoObj.CurrentDemo;
			WorldState worldState = new(PersistState.MapCache, d.FullName);

			worldState.TryProcessHeader(dem.Header, out string? err);
			if (err is not null) {
				Utils.Warning($"Failed to process demo header: {err}\n");
				PersistState.AnyDemosFailed = true;
				PersistState.AnyHeaderFailed = true;
				return;
			}

			int? rangeTickStart = null;
			int? lastSampleTick = null;
			int? prevTick = null; // for demos with non-consecutive ticks
			bool rangeOob = false;

			foreach (PacketFrame frame in dem.Frames) {

				// feed all frames to the world state
				worldState.TryProcessFrame(frame, out err);
				if (err is not null) {
					Utils.Warning($"Failed on tick {frame.Tick}: {err}\n");
					PersistState.AnyDemosFailed = true;
					return;
				}

				// for reporting purposes only report changes on Packet packets and non-negative ticks
				if (frame.Type != PacketType.Packet || frame.Tick < 0 || frame.Tick == lastSampleTick)
					continue;

				bool tickOob = worldState.PointOutsideWorldPortalRules(frame.Tick, out bool playerExists, out err);
				if (err is not null) {
					Utils.Warning($"Failed on tick {frame.Tick}: {err}\n");
					PersistState.AnyDemosFailed = true;
					return;
				}

				if (!playerExists)
					continue; // player entity hasn't been initialized yet

				lastSampleTick = frame.Tick;
				if (rangeTickStart.HasValue && tickOob != rangeOob) {
					// report old range
					ReportTickRangeOobStatus(rangeTickStart.Value, prevTick!.Value, rangeOob);
					rangeTickStart = frame.Tick;
				}
				rangeTickStart ??= frame.Tick;
				rangeOob = tickOob;
				prevTick = frame.Tick;
			}

			if (rangeTickStart.HasValue && prevTick.HasValue)
				ReportTickRangeOobStatus(rangeTickStart.Value, prevTick.Value, rangeOob);

			PersistState.AnyDemosSucceeded = true;
		}

		protected override void PostProcess(DemoParsingInfo infoObj, DirectoryInfo d, bool isDefault) {
			Console.Write("\n\n");
			if (PersistState.AnyDemosFailed)
				Utils.Warning("One or more demos failed to parse!\n");

			if (PersistState.AnyDemosOob)
				Utils.Warning("One or more demos had OOB ticks!\n");
			else if (PersistState.AnyDemosSucceeded)
				Utils.WriteColor("All demos are inbounds!\n", ConsoleColor.Green);

			if (PersistState.AnyHeaderFailed && !PersistState.AnyDemosSucceeded) {
				Utils.WriteColor(
					$"\nFailed to find any map files in '{d.FullName}', " +
					$"try using '{DefaultAliases[0]} [GAME_DIRECTORY]'\n",
					ConsoleColor.Yellow
				);
			}
		}

		// tracks player/portal state as we iterate through ticks in the demo
		private ref struct WorldState {
			private SourceMapCache mapCacheRef;
			public string? LastMapPath {get; private set;} = null;
			private SourceMap? curMap = null;
			private SourceMap.ReadContext mapReadCtx = new();
			private readonly string baseDir;
			// have the eyes/world/map changed since we last checked if a point was oob?
			private bool stateDirty = true;
			private bool lastOobCheckResult = false;

			const string PLAYER_CLASS_NAME_PORTAL = "CPortal_Player";
			const string PLAYER_CLASS_NAME_HL2 = "CHL2_Player";
			const string PORTAL_CLASS_NAME = "CProp_Portal";

			private struct PlayerState {
				public Vector3 Origin = new(0);
				public Vector3 ViewOffset = new(0);
				public Vector3 Mins = new(0);
				public Vector3 Maxs = new(0);
				public EHandle PortalEnvHandle = new (DemoInfo.NullEHandle);
				public PlayerState() {}
			}

			private struct PortalState{
				public Vector3 Origin = new(0);
				public Vector3 Angles = new(0);
				public EHandle Linked = new (DemoInfo.NullEHandle);
				public bool Activated = false;
				public PortalState() {}
			}

			private PlayerState playerBaseline = new();
			private PortalState portalBaseline = new();

			private readonly Dictionary<int, PlayerState> playerEnts = new();
			private readonly Dictionary<int, PortalState> portalEnts = new();

			public WorldState(SourceMapCache cache, string baseDir) {
				this.mapCacheRef = cache;
				this.baseDir = baseDir;
			}

			// helper for C_Portal_Player::CalcPortalView
			private ref struct EyeCalcContext {
				private ref readonly PlayerState player;
				private ref readonly PortalState entry;
				private ref readonly PortalState exit;
				private Vector3 entryF, entryR, entryU;
				private readonly float entryPlaneDist;

				private const float PORTAL_HALF_WIDTH = 32f;
				private const float PORTAL_HALF_HEIGHT = 54f;
				private const float PORTAL_HOLE_DEPTH = 500f;

				public EyeCalcContext(ref readonly PlayerState player, ref readonly PortalState entry, ref readonly PortalState exit) {
					this.player = ref player;
					this.entry = ref entry;
					this.exit = ref exit;
					AngleVectors(in entry.Angles, out entryF, out entryR, out entryU);
					entryPlaneDist = Vector3.Dot(entryF, entry.Origin);
				}

				private static void AngleVectors(ref readonly Vector3 ang, out Vector3 forward, out Vector3 right, out Vector3 up) {
					(float sp, float cp) = MathF.SinCos(ang[0] * MathF.PI / 180f);
					(float sy, float cy) = MathF.SinCos(ang[1] * MathF.PI / 180f);
					(float sr, float cr) = MathF.SinCos(ang[2] * MathF.PI / 180f);
					forward = new(cp*cy, cp*sy, -sp);
					right = new(-sr*sp*cy+cr*sy, -sr*sp*sy-cr*cy, -sr*cp);
					up = new(cr*sp*cy+sr*sy, cr*sp*sy-sr*cy, cr*cp);
				}

				public readonly bool EyeBehindPortal() {
					return Vector3.Dot(entryF, player.Origin + player.ViewOffset) - entryPlaneDist < 0f;
				}

				// AABB/OBB intersection test
				public readonly bool PlayerInPortalHole() {

					// CPortalSimulator::MoveTo, creates the outwards facing planes: forward, backward, up, down, left, right
					Span<Vector3> holePlaneNorms = stackalloc Vector3[6];
					Span<float> holePlaneDists = stackalloc float[6];

					holePlaneNorms[0] = entryF;
					holePlaneDists[0] = entryPlaneDist - 0.5f;
					holePlaneNorms[1] = -entryF;
					holePlaneDists[1] = -entryPlaneDist + PORTAL_HOLE_DEPTH;
					holePlaneNorms[2] = entryU;
					holePlaneDists[2] = Vector3.Dot(entry.Origin + entryU * (PORTAL_HALF_HEIGHT * .98f), entryU);
					holePlaneNorms[3] = -entryU;
					holePlaneDists[3] = -Vector3.Dot(entry.Origin - entryU * (PORTAL_HALF_HEIGHT * .98f), entryU);
					holePlaneNorms[4] = -entryR;
					holePlaneDists[4] = -Vector3.Dot(entry.Origin - entryR * (PORTAL_HALF_HEIGHT * .98f), entryR);
					holePlaneNorms[5] = entryR;
					holePlaneDists[5] = Vector3.Dot(entry.Origin + entryR * (PORTAL_HALF_HEIGHT * .98f), entryR);

					Vector3 mins = player.Origin + player.Mins;
					Vector3 maxs = player.Origin + player.Maxs;

					// BoxOnPlaneSide
					for (int i = 0; i < 6; i++) {
						int holePlaneSign = 0;
						for (int j = 0; j < 3; j++)
							if (holePlaneNorms[i][j] < 0)
								holePlaneSign |= (byte)(1 << j);

						ref readonly Vector3 p = ref holePlaneNorms[i];
						float d1, d2;
						switch (holePlaneSign) {
							case 0:
								d1 = p[0] * maxs[0] + p[1] * maxs[1] + p[2] * maxs[2];
								d2 = p[0] * mins[0] + p[1] * mins[1] + p[2] * mins[2];
								break;
							case 1:
								d1 = p[0] * mins[0] + p[1] * maxs[1] + p[2] * maxs[2];
								d2 = p[0] * maxs[0] + p[1] * mins[1] + p[2] * mins[2];
								break;
							case 2:
								d1 = p[0] * maxs[0] + p[1] * mins[1] + p[2] * maxs[2];
								d2 = p[0] * mins[0] + p[1] * maxs[1] + p[2] * mins[2];
								break;
							case 3:
								d1 = p[0] * mins[0] + p[1] * mins[1] + p[2] * maxs[2];
								d2 = p[0] * maxs[0] + p[1] * maxs[1] + p[2] * mins[2];
								break;
							case 4:
								d1 = p[0] * maxs[0] + p[1] * maxs[1] + p[2] * mins[2];
								d2 = p[0] * mins[0] + p[1] * mins[1] + p[2] * maxs[2];
								break;
							case 5:
								d1 = p[0] * mins[0] + p[1] * maxs[1] + p[2] * mins[2];
								d2 = p[0] * maxs[0] + p[1] * mins[1] + p[2] * maxs[2];
								break;
							case 6:
								d1 = p[0] * maxs[0] + p[1] * mins[1] + p[2] * mins[2];
								d2 = p[0] * mins[0] + p[1] * maxs[1] + p[2] * maxs[2];
								break;
							case 7:
								d1 = p[0] * mins[0] + p[1] * mins[1] + p[2] * mins[2];
								d2 = p[0] * maxs[0] + p[1] * maxs[1] + p[2] * maxs[2];
								break;
							default:
								Debug.Assert(false);
								d1 = d2 = 0f;
								break;
						}

						bool inFront = d1 >= holePlaneDists[i];
						bool behind = d2 < holePlaneDists[i];

						if (inFront && !behind)
							return false;
					}

					return true;
				}

				// hot nonsensical garbage
				public readonly bool QuadrantMeme() {
					if (entryF.Z >= -0.01f)
						return false;
					float originDist = Vector3.Dot(entryF, player.Origin) - entryPlaneDist;
					if (originDist <= 0f)
						return false;
					Vector3 playerEye = player.Origin + player.ViewOffset;
					float eyeDist = Vector3.Dot(entryF, playerEye) - entryPlaneDist;
					float invTotalDist = 1f / (originDist - eyeDist);
					Vector3 planeIntersection = ((playerEye * originDist) - (player.Origin * eyeDist)) * invTotalDist;
					Vector3 intersectionTest = planeIntersection - entry.Origin;
					return Vector3.Dot(intersectionTest, entryR) <= PORTAL_HALF_WIDTH
						&& Vector3.Dot(intersectionTest, entryU) <= PORTAL_HALF_HEIGHT;
				}

				// very condensed version of a portal transform that doesn't require explicit matrix multiplications
				public readonly Vector3 TransformPoint(ref readonly Vector3 pt) {
					AngleVectors(in exit.Angles, out Vector3 exitF, out Vector3 exitR, out Vector3 exitU);
					Vector3 delta = pt - entry.Origin;
					return exit.Origin
						- Vector3.Dot(delta, entryF) * exitF
						- Vector3.Dot(delta, entryR) * exitR
						+ Vector3.Dot(delta, entryU) * exitU;
				}
			}

			private void TryUpdateMap(string baseDir, string modDir, string mapName, out string? err) {
				playerBaseline = new();
				portalBaseline = new();
				playerEnts.Clear();
				portalEnts.Clear();
				stateDirty = true;
				mapReadCtx = new();
				curMap = null;

				LastMapPath = SourceMapCache.TryFindMapFileChecked(baseDir, modDir, mapName, out err);
				if (err is not null)
					return;
				Debug.Assert(LastMapPath != null);
				curMap = mapCacheRef.TryGetMapObj(LastMapPath, out err);
				if (err is not null)
					return;
				mapReadCtx = new (curMap!);
			}

			// return true on player state change
			private static bool UpdatePlayerEntityState(ref PlayerState playerState, IReadOnlyList<(int propIndex, EntityProperty prop)> properties) {
				bool anyChange = false;
				foreach ((int _, EntityProperty prop) in properties) {
					switch (prop) {
						case SingleEntProp<Vector3> vec3Prop when vec3Prop.Name == "m_vecOrigin":
							playerState.Origin = vec3Prop.Value;
							anyChange = true;
							break;
						case SingleEntProp<float> floatProp:
							switch (floatProp.Name) {
								case "m_vecViewOffset[0]":
									playerState.ViewOffset[0] = floatProp.Value;
									anyChange = true;
									break;
								case "m_vecViewOffset[1]":
									playerState.ViewOffset[1] = floatProp.Value;
									anyChange = true;
									break;
								case "m_vecViewOffset[2]":
									playerState.ViewOffset[2] = floatProp.Value;
									anyChange = true;
									break;
							}
							break;
						case SingleEntProp<int> intProp when intProp.Name == "m_hPortalEnvironment":
							playerState.PortalEnvHandle = (EHandle)intProp.Value;
							anyChange = true;
							break;
							
					}
				}
				return anyChange;
			}

			// returns true on portal state change
			private static bool UpdatePortalEntityState(ref PortalState portalState, IReadOnlyList<(int propIndex, EntityProperty prop)> properties) {
				bool anyChange = false;
				foreach ((int _, EntityProperty prop) in properties) {
					switch (prop) {
						case SingleEntProp<Vector3> vec3Prop:
							switch (vec3Prop.Name) {
								case "m_vecOrigin":
									portalState.Origin = vec3Prop.Value;
									anyChange = true;
									break;
								case "m_angRotation":
									portalState.Angles = vec3Prop.Value;
									anyChange = true;
									break;
							}
							break;
						case SingleEntProp<int> intProp:
							switch (intProp.Name) {
								case "m_hLinkedPortal":
									portalState.Linked = (EHandle)intProp.Value;
									anyChange = true;
									break;
								case "m_bActivated":
									portalState.Activated = intProp.Value != 0;
									anyChange = true;
									break;
							}
							break;
					}
				}
				return anyChange;
			}

			// returns true on entity change
			private bool ProcessEntityUpdate(EntityUpdate update) {
				bool anyChange = false;
				switch (update) {
					case Delta delta:
						switch (delta.ServerClass!.ClassName) {
							case PLAYER_CLASS_NAME_PORTAL:
							case PLAYER_CLASS_NAME_HL2: {
									ref PlayerState ent = ref CollectionsMarshal.GetValueRefOrAddDefault(playerEnts, delta.EntIndex, out bool exists);
									if (!exists || delta is EnterPvs) {
										// if the entity doesn't exist during a delta, create it anyways
										ent = playerBaseline;
										anyChange = true;
									}
									anyChange |= UpdatePlayerEntityState(ref ent, delta.Props);
									break;
								}
							case PORTAL_CLASS_NAME: { 
									ref PortalState ent = ref CollectionsMarshal.GetValueRefOrAddDefault(portalEnts, delta.EntIndex, out bool exists);
									if (!exists || delta is EnterPvs) {
										ent = portalBaseline;
										anyChange = true;
									}
									anyChange |= UpdatePortalEntityState(ref ent, delta.Props);
									break;
								}
						}
						break;
					case LeavePvs leavePvs:
						anyChange |= playerEnts.Remove(leavePvs.Index);
						anyChange |= portalEnts.Remove(leavePvs.Index);
						break;
				}
				return anyChange;
			}

			private void TryProcessBaseline(InstanceBaseline baseline, out string? err) {
				if (baseline.ServerClassRef is null || baseline.Properties is null) {
					err = "InstanceBaseline was not fully parsed";
					return;
				} else {
					err = null;
					// here we don't need to keep track of changes since we're just updating the baseline, not actual entities
					switch (baseline.ServerClassRef.ClassName) {
						case PLAYER_CLASS_NAME_PORTAL:
						case PLAYER_CLASS_NAME_HL2:
							UpdatePlayerEntityState(ref playerBaseline, baseline.Properties);
							break;
						case PORTAL_CLASS_NAME:
							UpdatePortalEntityState(ref portalBaseline, baseline.Properties);
							break;
					}
				}
			}

			private void TryProcessPacketEntities(SvcPacketEntities svcPacketEntities, out string? err) {
				if (svcPacketEntities.Updates is null) {
					err = "Entity parsing failed for this demo";
					return;
				}
				bool anyChange = false;
				foreach (EntityUpdate update in svcPacketEntities.Updates)
					anyChange |= ProcessEntityUpdate(update);
				stateDirty |= anyChange;
				err = null;
			}

			private void TryProcessStringTables(StringTables stringTables, out string? err) {
				foreach (StringTable table in stringTables.Tables) {
					if (table.Name != TableNames.InstanceBaseLine)
						continue;
					if (table.TableEntries is null) {
						err = "String table entries was null";
						return;
					}
					foreach (StringTableEntry entry in table.TableEntries) {
						if (entry?.EntryData is InstanceBaseline baseline) {
							TryProcessBaseline(baseline, out err);
							if (err is not null)
								return;
						}
					}
				}
				err = null;
			}

			private void TryProcessCreateStringTable(SvcCreateStringTable msg, out string? err) {
				if (msg.TableName != TableNames.InstanceBaseLine) {
					err = null;
					return;
				}
				if (msg.TableUpdates?.TableUpdates is not {} updates) {
					err = "SvcCreateStringTable message was not parsed correctly";
					return;
				}
				foreach (TableUpdate? update in updates) {
					if (update?.Entry?.EntryData is InstanceBaseline baseline) {
						TryProcessBaseline(baseline, out err);
						if (err is not null)
							return;
					}
				}
				err = null;
			}

			private void TryProcessPacket(Packet packet, out string? err) {
				err = null;
				foreach (DemoMessage msg in packet.MessageStream) {
					switch (msg) {
						case SvcServerInfo svcServerInfo:
							TryUpdateMap(baseDir, svcServerInfo.GameDir, svcServerInfo.MapName, out err);
							break;
						case SvcPacketEntities svcPacketEntities:
							TryProcessPacketEntities(svcPacketEntities, out err);
							break;
						case SvcCreateStringTable svcCreateStringTable:
							TryProcessCreateStringTable(svcCreateStringTable, out err);
							break;
					}
					if (err is not null)
						return;
				}
			}

			public void TryProcessHeader(DemoHeader header, out string? err) {
				TryUpdateMap(baseDir, header.GameDirectory, header.MapName, out err);
			}

			public void TryProcessFrame(PacketFrame frame, out string? err) {
				if (frame.Packet is null) {
					err = "DemoPacket was null";
					return;
				}
				switch (frame.Packet) {
					case Packet packet:
						TryProcessPacket(packet, out err);
						return;
					case StringTables stringTables:
						TryProcessStringTables(stringTables, out err);
						return;
				}
				err = null;
			}

			// for debugging
			private enum CalcPortalViewResult {
				NOT_IN_PORTAL,
				INF_SENS_TICK,
				SG_PORTAL_CLOSED,
				EYE_IN_FRONT_OF_PORTAL,
				PLAYER_IN_PORTAL_HOLE,
				QUADRANT_MEME,
				NO_PORTAL_TRANSFORM,
			}

			private readonly CalcPortalViewResult CalcPortalView(ref readonly PlayerState player, out Vector3 eyePos, out string? err) {

				// no portal environment, easy
				eyePos = player.Origin + player.ViewOffset;
				if (player.PortalEnvHandle == DemoInfo.NullEHandle) {
					err = null;
					return CalcPortalViewResult.NOT_IN_PORTAL;
				}
				// inf sens tick(s)
				if (player.Origin == new Vector3(16384f)) {
					err = null;
					return CalcPortalViewResult.INF_SENS_TICK;
				}
				
				// grab portals

				ref PortalState portal = ref CollectionsMarshal.GetValueRefOrNullRef(portalEnts, player.PortalEnvHandle.EntIndex);
				if (Unsafe.IsNullRef(ref portal)) {
					err = $"CalcPortalView: could not find portal entity at index {player.PortalEnvHandle.EntIndex}";
					return CalcPortalViewResult.SG_PORTAL_CLOSED;
				}
				if (!portal.Activated) {
					err = null;
					return CalcPortalViewResult.SG_PORTAL_CLOSED;
				}
				ref PortalState linked = ref CollectionsMarshal.GetValueRefOrNullRef(portalEnts, portal.Linked.EntIndex);
				if (Unsafe.IsNullRef(ref linked)) {
					/*
					 * If you have SG and pass through a fizzler you will still have m_hPortalEnvironment
					 * set, but the portals won't link to each other in the demo. This is fine since we
					 * don't care about closed portals anyways.
					 */
					err = null;
					return CalcPortalViewResult.SG_PORTAL_CLOSED;
				}

				// C_Portal_Player::CalcPortalView

				err = null;
				EyeCalcContext ctx = new(in player, in portal, in linked);
				if (!ctx.EyeBehindPortal())
					return CalcPortalViewResult.EYE_IN_FRONT_OF_PORTAL;

				if (ctx.PlayerInPortalHole()) {
					eyePos = ctx.TransformPoint(in eyePos);
					return CalcPortalViewResult.PLAYER_IN_PORTAL_HOLE;
				} else if (ctx.QuadrantMeme()) {
					eyePos = ctx.TransformPoint(in eyePos);
					return CalcPortalViewResult.QUADRANT_MEME;
				}

				return CalcPortalViewResult.NO_PORTAL_TRANSFORM;
			}

			/*
			 * Determines if a point is OOB using the Portal speedrun.com inbounds rules. Does not throw.
			 * - playerExists returns false if the player entity has not been parsed in the demo yet
			 * - err is not null in case of error (and the return boolean is meaningless)
			 */
			public bool PointOutsideWorldPortalRules(int tick, out bool playerExists, out string? err) {
				if (mapReadCtx.IsEmpty) {
					playerExists = false;
					err = null;
					return false;
				}
				if (stateDirty) {
					// if we wanted to track OOB detection for other players, we'd need to start here
					ref PlayerState player = ref CollectionsMarshal.GetValueRefOrNullRef(playerEnts, 1);
					if (Unsafe.IsNullRef(ref player)) {
						playerExists = false;
						err = null;
						return false;
					}
					playerExists = true;
					if (player.Origin == new Vector3(16384f)) {
						// inf sens tick(s)
						lastOobCheckResult = false;
					} else {
						CalcPortalViewResult res = CalcPortalView(in player, out Vector3 eyes, out err);
						// uncomment for debugging each tick :)
						// Console.WriteLine($"[{tick}] CalcPortalView result: {res}");
						if (err is not null)
							return false;
						try {
							lastOobCheckResult = mapReadCtx.PointOutsideWorldPortalRules(in eyes);
						} catch (IndexOutOfRangeException) {
							err = "PointOutsideWorld read invalid memory";
							return false;
						}
					}
					stateDirty = false;
				}
				playerExists = true;
				err = null;
				return lastOobCheckResult;
			}
		}

		private class SourceMap {

			[StructLayout(LayoutKind.Sequential, Pack = 1)]
			public struct DLump {
				public Int32 pos;
				public Int32 len;
				public Int32 version;
				public Int32 ident;
			}

			[StructLayout(LayoutKind.Sequential, Pack = 1)]
			public unsafe struct DLeaf {
				public Int32 contents;
				public Int16 cluster;
				private fixed byte _pad0[18];
				public UInt16 firstBrush;
				public UInt16 numBrushes;
				private fixed byte _pad1[4];
			}

			[StructLayout(LayoutKind.Sequential, Pack = 1)]
			public unsafe struct DNode {
				public Int32 plane;
				public fixed Int32 children[2];
				private fixed byte _pad0[20];
			}

			[StructLayout(LayoutKind.Sequential, Pack = 1)]
			public struct DPlane {
				public Vector3 normal;
				public Single dist;
				public Int32 type;
			}

			[StructLayout(LayoutKind.Sequential, Pack = 1)]
			public struct DLeafBrush {
				public UInt16 brush;
			}

			[StructLayout(LayoutKind.Sequential, Pack = 1)]
			public struct DBrush {
				public Int32 firstSide;
				public Int32 numSides;
				public Int32 contents;
				public const UInt32 MASK_PLAYERSOLID_BRUSHONLY = 0x1400b;
			}

			[StructLayout(LayoutKind.Sequential, Pack = 1)]
			public struct DBrushSide {
				public UInt16 plane;
				public Int16 texInfo;
				public Int16 dispInfo;
				public Int16 bevel;
			}

			enum LumpType : int {
				Leafs,
				Nodes,
				Planes,
				LeafBrushes,
				Brushes,
				BrushSides,
				COUNT,
			}

			private struct ExpectedLumpInfo {
				public int index;
				public int version;
				public Type langType;
			}

			private static readonly IDictionary<LumpType, ExpectedLumpInfo> LumpInfosSource2007 = new Dictionary<LumpType, ExpectedLumpInfo> {
				[LumpType.Leafs] = new ExpectedLumpInfo{index=10, version=1, langType=typeof(DLeaf)},
				[LumpType.Nodes] = new ExpectedLumpInfo{index=5, version=0, langType=typeof(DNode)},
				[LumpType.Planes] = new ExpectedLumpInfo{index=1, version=0, langType=typeof(DPlane)},
				[LumpType.LeafBrushes] = new ExpectedLumpInfo{index=17, version=0, langType=typeof(DLeafBrush)},
				[LumpType.Brushes] = new ExpectedLumpInfo{index=18, version=0, langType=typeof(DBrush)},
				[LumpType.BrushSides] = new ExpectedLumpInfo{index=19, version=0, langType=typeof(DBrushSide)},
			};

			private const uint BSP_HEADER_IDENT = ('P' << 24) + ('S' << 16) + ('B' << 8) + 'V';
			private const uint BSP_VERSION = 20;

			private static ReadOnlySpan<T> BsrToSpan<T>(ref readonly BitStreamReader bsr) where T : struct {
				ReadOnlySpan<byte> byteSpan = new ReadOnlySpan<byte>(bsr.Data).Slice(bsr.AbsoluteStart / 8, bsr.BitLength / 8);
				return MemoryMarshal.Cast<byte, T>(byteSpan);
			}

			// there are LumpTypes.COUNT of these
			private readonly BitStreamReader[] lumpReaders;

			private SourceMap(BitStreamReader[] lumpReaders) => this.lumpReaders = lumpReaders;

			public static SourceMap? TryParse(string mapPath, out string? err) {
				try {
					byte[] bytes = File.ReadAllBytes(mapPath);
					SourceMap? mapObj = TryParse(new BitStreamReader(bytes), out err);
					if (err is not null)
						err = $"failed to parse '{mapPath}': {err}";
					return mapObj;
				} catch (Exception e) {
					err = $"failed to parse '{mapPath}': {e.Message}";
					return null;
				}
			}

			public static SourceMap? TryParse(BitStreamReader bsr, out string? err) {

				// read header & lump headers

				IDictionary<LumpType, ExpectedLumpInfo> lumpInfoMap = LumpInfosSource2007;
				int maxReqLumpVal = lumpInfoMap.Max(x => x.Value.index) + 1;

				uint bspIdent = bsr.ReadUInt();
				uint bspVersion = bsr.ReadUInt();
				BitStreamReader bsrLumps;
				unsafe {
					bsrLumps = bsr.ForkAndSkip(sizeof(DLump) * maxReqLumpVal * 8);
				}

				if (bspIdent != BSP_HEADER_IDENT || bspVersion != BSP_VERSION || bsr.HasOverflowed) {
					err = "bad BSP header";
					return null;
				}

				// init lumps

				bsr = bsr.FromBeginning();
				ReadOnlySpan<DLump> lumpHeaderSpan = BsrToSpan<DLump>(ref bsrLumps);
				BitStreamReader[] lumpReaders = new BitStreamReader[(int)LumpType.COUNT];

				for (int i = 0 ; i < (int)LumpType.COUNT; i++) {
					ExpectedLumpInfo expectedLumpInfo = lumpInfoMap[(LumpType)i];
					ref readonly DLump lumpHeader = ref lumpHeaderSpan[expectedLumpInfo.index];
					if (lumpHeader.version != expectedLumpInfo.version) {
						err = $"{(LumpType)i} lump had version {lumpHeader.version} (expected {expectedLumpInfo.version})";
						return null;
					}
					if (bsr.BitLength / 8 < lumpHeader.pos + lumpHeader.len) {
						err = $"{(LumpType)i} lump is in an invalid place in the map file";
						return null;
					}
					if (lumpHeader.len % Marshal.SizeOf(expectedLumpInfo.langType) != 0) {
						err = $"{(LumpType)i} lump had an unexpected number of bytes in the map file";
						return null;
					}
					lumpReaders[i] = bsr.Fork(lumpHeader.pos * 8, lumpHeader.len * 8);
					Debug.Assert(!lumpReaders[i].HasOverflowed); // should be caught in the above check
				}

				err = null;
				return new SourceMap(lumpReaders);
			}

			// a context for checking multiple points with minimal overhead (needed to create ReadOnlySpan instances)
			public readonly ref struct ReadContext {
				public readonly ReadOnlySpan<DLeaf> leafs = ReadOnlySpan<DLeaf>.Empty;
				public readonly ReadOnlySpan<DNode> nodes = ReadOnlySpan<DNode>.Empty;
				public readonly ReadOnlySpan<DPlane> planes = ReadOnlySpan<DPlane>.Empty;
				public readonly ReadOnlySpan<DLeafBrush> leafBrushes = ReadOnlySpan<DLeafBrush>.Empty;
				public readonly ReadOnlySpan<DBrush> brushes = ReadOnlySpan<DBrush>.Empty;
				public readonly ReadOnlySpan<DBrushSide> brushSides = ReadOnlySpan<DBrushSide>.Empty;

				public ReadContext() {}

				public ReadContext(SourceMap mapObj) {
					leafs = BsrToSpan<DLeaf>(ref mapObj.lumpReaders[(int)LumpType.Leafs]);
					nodes = BsrToSpan<DNode>(ref mapObj.lumpReaders[(int)LumpType.Nodes]);
					planes = BsrToSpan<DPlane>(ref mapObj.lumpReaders[(int)LumpType.Planes]);
					leafBrushes = BsrToSpan<DLeafBrush>(ref mapObj.lumpReaders[(int)LumpType.LeafBrushes]);
					brushes = BsrToSpan<DBrush>(ref mapObj.lumpReaders[(int)LumpType.Brushes]);
					brushSides = BsrToSpan<DBrushSide>(ref mapObj.lumpReaders[(int)LumpType.BrushSides]);
				}

				public readonly bool IsEmpty => nodes.IsEmpty;

				// all methods may throw IndexOutOfRangeException

				public readonly int GetLeaf(ref readonly Vector3 v) {
					// CM_PointLeafnum_r
					Int32 n = 0;
					while (n >= 0) {
						ref readonly DNode node = ref nodes[n];
						ref readonly DPlane plane = ref planes[node.plane];
						float d = plane.type < 3 ? v[plane.type] - plane.dist : Vector3.Dot(plane.normal, v) - plane.dist;
						unsafe { n = d < 0 ? node.children[1] : node.children[0]; }
					}
					return -1 - n;
				}

				public readonly bool PointInBrush(ref readonly Vector3 v, int brushIdx) {
					ref readonly DBrush brush = ref brushes[brushIdx];
					if ((brush.contents & DBrush.MASK_PLAYERSOLID_BRUSHONLY) == 0)
						return false;
					if (brush.numSides <= 0)
						return false;
					// CM_TestBoxInBrush
					for (int j = 0; j < brush.numSides; j++) {
						ref readonly DBrushSide brushSide = ref brushSides[brush.firstSide + j];
						if (brushSide.bevel != 0)
							continue;
						ref readonly DPlane plane = ref planes[brushSide.plane];
						if (Vector3.Dot(v, plane.normal) - plane.dist > 0)
							return false;
					}
					return true;
				}

				public readonly bool PointOutsideWorldPortalRules(ref readonly Vector3 v) {
					// CEngineTrace::PointOutsideWorld
					int leafIdx = GetLeaf(in v);
					ref readonly DLeaf leaf = ref leafs[leafIdx];
					if (leaf.cluster != -1)
						return false; // entire leaf is inbounds

					// CM_TestInLeaf - check for brushes in leaf

					for (int i = 0; i < leaf.numBrushes; i++)
						if (PointInBrush(in v, leafBrushes[leaf.firstBrush + i].brush))
							return false;

					return true;

					/*
					 * Game code also checks for displacements in the leaf for tracing purposes,
					 * but we probably don't need to do that since I *think* brushes will always
					 * enclose inbounds space (thanks bill).
					 */
				}
			}
		}

		// stores a few maps in memory and reuses them across demos
		private class SourceMapCache {

			private class CacheEntry {
				public string path;
				public int lastTouched;
				public SourceMap mapObj;
			}

			// list of maps sorted by most recently touched (first is most recent)
			private readonly CacheEntry[] entries;

			// keep the cache size pretty small (<10)
			public SourceMapCache(int cacheSize) {
				entries = new CacheEntry[cacheSize];
			}

			public static string? TryFindMapFileChecked(string baseDir, string modName, string mapName, out string? err) {
				try {
					string? mapPath = TryFindMapFile(baseDir, modName, mapName);
					if (mapPath != null) {
						err = null;
						return mapPath;
					}
				} catch (Exception) {}
				err = $"failed to find map file '{mapName}' in '{baseDir}'";
				return null;
			}

			// may throw all sorts of funny exceptions
			public static string? TryFindMapFile(string baseDir, string modName, string mapName) {
				if (!mapName.EndsWith(".bsp"))
					mapName += ".bsp";
				// try to be as generous as we reasonably can
				string path = Path.Combine(baseDir, mapName);
				if (File.Exists(path))
					return Path.GetFullPath(path); // base dir is map folder
				path = Path.Combine([baseDir, "maps", mapName]);
				if (File.Exists(path))
					return Path.GetFullPath(path); // base dir is mod folder
				path = Path.Combine([baseDir, modName, "maps", mapName]);
				if (File.Exists(path))
					return Path.GetFullPath(path); // base dir is game folder
				return null;
			}

			// call with a result from TryFindMapFile, should not throw
			public SourceMap? TryGetMapObj(string mapPath, out string? err) {
				int curMax = entries[0]?.lastTouched ?? -1;
				int matchIdx = Array.FindIndex(entries, x => x?.path == mapPath);
				if (matchIdx == -1) {
					SourceMap? mapObj = SourceMap.TryParse(mapPath, out err);
					if (err is not null)
						return null;
					entries[^1] = new CacheEntry{path = mapPath, lastTouched = curMax + 1, mapObj = mapObj!};
				} else if (matchIdx != 0) {
					CacheEntry entry = entries[matchIdx];
					entry.lastTouched = curMax + 1;
				}
				// re-sort since the cache size should be pretty small
				if (matchIdx != 0)
					Array.Sort(entries, (a, b) => b == null ? -1 : (a == null ? 1 : -a.lastTouched.CompareTo(b.lastTouched)));
				Debug.Assert(entries[0].path == mapPath);
				err = null;
				return entries[0].mapObj;
			}
		}
	}
}
