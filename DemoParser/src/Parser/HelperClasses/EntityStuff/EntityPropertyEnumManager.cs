using System;
using System.Collections.Generic;
using DemoParser.Parser.Components.Messages.UserMessages;
using DemoParser.Utils;
using static DemoParser.Parser.HelperClasses.EntityStuff.DisplayType;
using static DemoParser.Parser.HelperClasses.EntityStuff.PropEnums;
using static DemoParser.Parser.HelperClasses.EntityStuff.PropEnums.Collision_Group_t;
using static DemoParser.Parser.HelperClasses.EntityStuff.PropEnums.PlayerMfFlags_t;

// yes i know everything is gonna have terrible names

// ReSharper disable CommentTypo
// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo
// ReSharper disable InconsistentNaming

// basically another toString() helper for ent props, aimed specifically at special cases with ints and enums/flags
namespace DemoParser.Parser.HelperClasses.EntityStuff {

	internal static class EntityPropertyEnumManager {


		private static readonly Dictionary<string, DisplayType> GlobalTableNames = new Dictionary<string, DisplayType> {
			["m_chAreaBits"]       = AreaBits,
			["m_chAreaPortalBits"] = AreaBits
		};

		private static readonly Dictionary<string, DisplayType> GlobalPropNames = new Dictionary<string, DisplayType> {
			["moveparent"]    = Handle,
			["m_clrRender"]   = Color,
			["m_lifeState"]   = m_lifeState,
			["m_nRenderFX"]   = m_nRenderFX,
			["m_nRenderMode"] = m_nRenderMode
		};

		private static readonly Dictionary<(string tableName, string propName), DisplayType> TableSpecificPropNames =
			new Dictionary<(string, string), DisplayType>
			{
				[("DT_PlayerState", "deadflag")]                  = Bool,
				[("DT_Portal_Player", "m_pHeldObjectPortal")]     = Handle,
				[("DT_Local", "m_audio.ent")]                     = Handle,
				[("DT_CollisionProperty", "m_usSolidFlags")]      = DT_CollisionProperty__m_usSolidFlags,
				[("DT_CollisionProperty", "m_nSurroundType")]     = DT_CollisionProperty__m_nSurroundType,
				[("DT_CollisionProperty", "m_nSolidType")]        = DT_CollisionProperty__m_nSolidType,
				[("DT_BaseEntity", "movetype")]                   = DT_BaseEntity__movetype,
				[("DT_BaseEntity", "m_fEffects")]                 = DT_BaseEntity__m_fEffects,
				[("DT_BaseEntity", "movecollide")]                = DT_BaseEntity__movecollide,
				[("DT_BaseEntity", "m_CollisionGroup")]           = DT_BaseEntity__m_CollisionGroup,
				[("DT_MaterialModifyControl", "m_nModifyMode")]   = DT_MaterialModifyControl__m_nModifyMode,
				[("DT_WeaponPortalgun", "m_EffectState")]         = DT_WeaponPortalgun__m_EffectState,
				[("DT_RopeKeyframe", "m_RopeFlags")]              = DT_RopeKeyframe__m_RopeFlags,
				[("DT_RopeKeyframe", "m_fLockedPoints")]          = DT_RopeKeyframe__m_fLockedPoints,
				[("DT_CitadelEnergyCore", "m_spawnflags")]        = DT_CitadelEnergyCore__m_spawnflags,
				[("DT_CitadelEnergyCore", "m_nState")]            = DT_CitadelEnergyCore__m_nState,
				[("DT_Beam", "m_nBeamFlags")]                     = DT_Beam__m_nBeamFlags,
				[("DT_Beam", "m_nBeamType")]                      = DT_Beam__m_nBeamType,
				[("DT_Local", "m_iHideHUD")]                      = DT_Local__M_iHideHUD,
				[("DT_Portal_Player", "m_iPlayerSoundType")]      = DT_Portal_Player__m_iPlayerSoundType,
				[("DT_BasePlayer", "m_fFlags")]                   = DT_BasePlayer__m_fFlags,
				[("DT_EffectData", "m_nDamageType")]              = DT_EffectData__m_nDamageType,
				[("DT_SlideshowDisplay", "m_iCycleType")]         = DT_SlideshowDisplay__m_iCycleType,
				[("DT_BaseCombatWeapon", "m_iState")]             = DT_BaseCombatWeapon__m_iState,
				[("DT_LightGlow", "m_spawnflags")]                = DT_LightGlow__m_spawnflags,
				[("DT_SteamJet", "m_spawnflags")]                 = DT_SteamJet__m_spawnflags,
				[("DT_FuncSmokeVolume", "m_spawnflags")]          = DT_FuncSmokeVolume__m_spawnflags,
				[("DT_Env_Lightrail_Endpoint", "m_spawnflags")]   = DT_Env_Lightrail_Endpoint__m_spawnflags,
				[("DT_Env_Lightrail_Endpoint", "m_nState")]       = DT_Env_Lightrail_Endpoint__m_nState,
				[("DT_SteamJet", "m_nType")]                      = DT_SteamJet__m_nType,
				[("DT_TEExplosion", "m_nFlags")]                  = DT_TEExplosion__m_nFlags,
				[("DT_TEShatterSurface", "m_nSurfaceType")]       = DT_BreakableSurface__m_nSurfaceType,
				[("DT_BreakableSurface", "m_nSurfaceType")]       = DT_BreakableSurface__m_nSurfaceType,
				[("DT_Func_Dust", "m_DustFlags")]                 = DT_Func_Dust__m_DustFlags,
				[("DT_VGuiScreen", "m_fScreenFlags")]             = DT_VGuiScreen__m_fScreenFlags,
				[("DT_EntityDissolve", "m_nDissolveType")]        = DT_EntityDissolve__m_nDissolveType,
				[("DT_PoseController", "m_nFModType")]            = DT_PoseController__m_nFModType,
				[("DT_Precipitation", "m_nPrecipType")]           = DT_Precipitation__m_nPrecipType,
				[("DT_CPropJeepEpisodic", "m_iRadarContactType")] = DT_CPropJeepEpisodic__m_iRadarContactType,
				[("DT_BaseTrigger", "m_spawnflags")]              = DT_BaseTrigger__m_spawnflags,
				[("DT_Sun", "m_clrOverlay")]                      = Color,
				[("DT_PropPaintBomb", "m_nPaintPowerType")]       = DT_PropPaintBomb__m_nPaintPowerType
			};


		internal static bool DetermineIntSpecialType(string tableName, string propName, out DisplayType displayType) {
			return GlobalTableNames.TryGetValue(tableName, out displayType)
				   || GlobalPropNames.TryGetValue(propName, out displayType)
				   || TableSpecificPropNames.TryGetValue((tableName, propName), out displayType);
		}


		// I want all the flags to have some end and beginning, (such as parenthesis) in case they're used in arrays
		internal static string CreateEnumPropStr(int val, DisplayType displayType) {
			return displayType switch {
				DT_CollisionProperty__m_usSolidFlags      => $"({(SolidFlags_t)val})",
				DT_CollisionProperty__m_nSurroundType     => ((SurroundingBoundsType_t)val).ToString(),
				DT_CollisionProperty__m_nSolidType        => ((SolidType_t)val).ToString(),
				DT_BaseEntity__movetype                   => ((MoveType_t)val).ToString(),
				DT_BaseEntity__movecollide                => ((MoveCollide_t)val).ToString(),
				m_nRenderFX                               => ((RenderFx_t)val).ToString(),
				m_nRenderMode                             => ((RenderMode_t)val).ToString(),
				DT_BaseEntity__m_fEffects                 => $"({(EntityEffects_t)val})",
				m_lifeState                               => ((LifeState_t)val).ToString(),
				DT_MaterialModifyControl__m_nModifyMode   => ((MaterialModifyMode_t)val).ToString(),
				DT_WeaponPortalgun__m_EffectState         => ((PortalGunEffectState_t)val).ToString(),
				DT_RopeKeyframe__m_RopeFlags              => $"({(RopeFlags_t)val})",
				DT_RopeKeyframe__m_fLockedPoints          => $"({(RopeLockedPoints_t)val})",
				DT_CitadelEnergyCore__m_nState            => ((EnergyCoreState_t)val).ToString(),
				DT_CitadelEnergyCore__m_spawnflags        => $"({(SF_EnergyCore)val})",
				DT_Beam__m_nBeamFlags                     => $"({(BeamFlags_t)val})",
				DT_Beam__m_nBeamType                      => ((BeamTypes_t)val).ToString(),
				DT_Local__M_iHideHUD                      => $"({(HideHudFlags_t)val})",
				DT_Portal_Player__m_iPlayerSoundType      => ((PlayerSounds_t)val).ToString(),
				DT_EffectData__m_nDamageType              => $"({(DamageType_t)val})",
				DT_SlideshowDisplay__m_iCycleType         => ((SlideshowCycleTypes)val).ToString(),
				DT_BaseCombatWeapon__m_iState             => ((WeaponState_t)val).ToString(),
				DT_LightGlow__m_spawnflags                => $"({(SF_LightGlow)val})",
				DT_SteamJet__m_spawnflags                 => $"({(SF_SteamJet)val})",
				DT_FuncSmokeVolume__m_spawnflags          => $"({(SF_FuncSmokeVolume)val})",
				DT_Env_Lightrail_Endpoint__m_spawnflags   => $"({(SF_EnvLightrailEndpoint)val})",
				DT_Env_Lightrail_Endpoint__m_nState       => ((EnvLightRailEndpointState)val).ToString(),
				DT_SteamJet__m_nType                      => ((SteamJetType)val).ToString(),
				DT_TEExplosion__m_nFlags                  => $"({(TE_ExplosionFlags)val})",
				DT_BreakableSurface__m_nSurfaceType       => ((ShatterSurface_t)val).ToString(),
				DT_Func_Dust__m_DustFlags                 => $"({(DustFlags)val})",
				DT_VGuiScreen__m_fScreenFlags             => ((VguiScreenFlags)val).ToString(),
				DT_EntityDissolve__m_nDissolveType        => ((EntityDissolveType)val).ToString(),
				DT_PoseController__m_nFModType            => ((PoseController_FModType_t)val).ToString(),
				DT_Precipitation__m_nPrecipType           => ((PrecipitationType_t)val).ToString(),
				DT_CPropJeepEpisodic__m_iRadarContactType => ((EpisodicRadarContactType)val).ToString(),
				DT_BaseTrigger__m_spawnflags              => $"({(SF_BaseTrigger)val})",
				DT_PropPaintBomb__m_nPaintPowerType       => ((PaintType)val).ToString(),
				_ => throw EntPropToStringHelper.E
			};
		}
	}

	// all enums should have a "None" or equivalent unless the abstract flag checker is used

	public static class PropEnums {

		[Flags]
		public enum SolidFlags_t {
			None                        = 0,
			FSOLID_CUSTOMRAYTEST        = 0x0001, // Ignore solid type + always call into the entity for ray tests
			FSOLID_CUSTOMBOXTEST        = 0x0002, // Ignore solid type + always call into the entity for swept box tests
			FSOLID_NOT_SOLID            = 0x0004, // Are we currently not solid?
			FSOLID_TRIGGER              = 0x0008, // This is something may be collideable but fires touch functions
			                                      // even when it's not collideable (when the FSOLID_NOT_SOLID flag is set)
			FSOLID_NOT_STANDABLE        = 0x0010, // You can't stand on this
			FSOLID_VOLUME_CONTENTS      = 0x0020, // Contains volumetric contents (like water)
			FSOLID_FORCE_WORLD_ALIGNED  = 0x0040, // Forces the collision rep to be world-aligned even if it's SOLID_BSP or SOLID_VPHYSICS
			FSOLID_USE_TRIGGER_BOUNDS   = 0x0080, // Uses a special trigger bounds separate from the normal OBB
			FSOLID_ROOT_PARENT_ALIGNED  = 0x0100, // Collisions are defined in root parent's local coordinate space
			FSOLID_TRIGGER_TOUCH_DEBRIS = 0x0200, // This trigger will touch debris objects
		}


		public enum SurroundingBoundsType_t { // Specifies how to compute the surrounding box
			USE_OBB_COLLISION_BOUNDS = 0,
			USE_BEST_COLLISION_BOUNDS, // Always use the best bounds (most expensive)
			USE_HITBOXES,
			USE_SPECIFIED_BOUNDS,
			USE_GAME_CODE,
			USE_ROTATION_EXPANDED_BOUNDS,
			USE_COLLISION_BOUNDS_NEVER_VPHYSICS,
		}


		public enum SolidType_t { // Solid type basically describes how the bounding volume of the object is represented
			SOLID_NONE     = 0,   // no solid model
			SOLID_BSP      = 1,   // a BSP tree
			SOLID_BBOX     = 2,   // an AABB
			SOLID_OBB      = 3,   // an OBB (not implemented yet)
			SOLID_OBB_YAW  = 4,   // an OBB, constrained so that it can only yaw
			SOLID_CUSTOM   = 5,   // Always call into the entity for tests
			SOLID_VPHYSICS = 6,   // solid vphysics object, get vcollide from the model and collide with that
			SOLID_LAST,
		}


		public enum MoveType_t { // edict->movetype values
			MOVETYPE_NONE = 0,   // never moves
			MOVETYPE_ISOMETRIC,  // For players -- in TF2 commander view, etc.
			MOVETYPE_WALK,       // Player only - moving on the ground
			MOVETYPE_STEP,       // gravity, special edge handling -- monsters use this
			MOVETYPE_FLY,        // No gravity, but still collides with stuff
			MOVETYPE_FLYGRAVITY, // flies through the air + is affected by gravity
			MOVETYPE_VPHYSICS,   // uses VPHYSICS for simulation
			MOVETYPE_PUSH,       // no clip to world, push and crush
			MOVETYPE_NOCLIP,     // No gravity, no collisions, still do velocity/avelocity
			MOVETYPE_LADDER,     // Used by players only when going onto a ladder
			MOVETYPE_OBSERVER,   // Observer movement, depends on player's observer mode
			MOVETYPE_CUSTOM,     // Allows the entity to describe its own physics
		}


		public enum MoveCollide_t {  // edict->movecollide values
			MOVECOLLIDE_DEFAULT = 0, // These ones only work for MOVETYPE_FLY + MOVETYPE_FLYGRAVITY
			MOVECOLLIDE_FLY_BOUNCE,  // bounces, reflects, based on elasticity of surface and object - applies friction (adjust velocity)
			MOVECOLLIDE_FLY_CUSTOM,  // Touch() will modify the velocity however it likes
			MOVECOLLIDE_FLY_SLIDE,   // slides along surfaces (no bounce) - applies friction (adjusts velocity)
			MOVECOLLIDE_COUNT,       // Number of different movecollides
		}


		public enum RenderFx_t {
			kRenderFxNone = 0,
			kRenderFxPulseSlow,
			kRenderFxPulseFast,
			kRenderFxPulseSlowWide,
			kRenderFxPulseFastWide,
			kRenderFxFadeSlow,
			kRenderFxFadeFast,
			kRenderFxSolidSlow,
			kRenderFxSolidFast,
			kRenderFxStrobeSlow,
			kRenderFxStrobeFast,
			kRenderFxStrobeFaster,
			kRenderFxFlickerSlow,
			kRenderFxFlickerFast,
			kRenderFxNoDissipation,
			kRenderFxDistort,       // Distort/scale/translate flicker
			kRenderFxHologram,      // kRenderFxDistort + distance fade
			kRenderFxExplode,       // Scale up really big!
			kRenderFxGlowShell,     // Glowing Shell
			kRenderFxClampMinScale, // Keep this sprite from getting very small (SPRITES only!)
			kRenderFxEnvRain,       // for environmental rendermode, make rain
			kRenderFxEnvSnow,       //  "        "            "    , make snow
			kRenderFxSpotlight,     // TEST CODE for experimental spotlight
			kRenderFxRagdoll,       // HACKHACK: TEST CODE for signalling death of a ragdoll character
			kRenderFxPulseFastWider,
			kRenderFxMax
		}


		public enum RenderMode_t {
			kRenderNormal,             // src
			kRenderTransColor,         // c*a+dest*(1-a)
			kRenderTransTexture,       // src*a+dest*(1-a)
			kRenderGlow,               // src*a+dest -- No Z buffer checks -- Fixed size in screen space
			kRenderTransAlpha,         // src*srca+dest*(1-srca)
			kRenderTransAdd,           // src*a+dest
			kRenderEnvironmental,      // not drawn, used for environmental effects
			kRenderTransAddFrameBlend, // use a fractional frame value to blend between animation frames
			kRenderTransAlphaAdd,      // src + dest*(1-a)
			kRenderWorldGlow,          // Same as kRenderGlow but not fixed size in screen space
			kRenderNone,               // Don't render.
		}


		[Flags]
		public enum EntityEffects_t {
			None                  = 0,
			EF_BONEMERGE          = 0x001, // Performs bone merge on client side
			EF_BRIGHTLIGHT        = 0x002, // DLIGHT centered at entity origin
			EF_DIMLIGHT           = 0x004, // player flashlight
			EF_NOINTERP           = 0x008, // don't interpolate the next frame
			EF_NOSHADOW           = 0x010, // Don't cast no shadow
			EF_NODRAW             = 0x020, // don't draw entity
			EF_NORECEIVESHADOW    = 0x040, // Don't receive no shadow
			EF_BONEMERGE_FASTCULL = 0x080, // For use with EF_BONEMERGE. If this is set, then it places this ent's origin at its
			                               // parent and uses the parent's bbox + the max extents of the aiment.
			                               // Otherwise, it sets up the parent's bones every frame to figure out where to place
			                               // the aiment, which is inefficient because it'll setup the parent's bones even if
			                               // the parent is not in the PVS.
			EF_ITEM_BLINK         = 0x100, // blink an item so that the user notices it.
			EF_PARENT_ANIMATES    = 0x200, // always assume that the parent entity is animating
			// new
			EF_MARKED_FOR_FAST_REFLECTION = 0x400,  // marks an entity for reflection rendering when using $reflectonlymarkedentities material variable
			EF_NOSHADOWDEPTH              = 0x800,  // Indicates this entity does not render into any shadow depthmap
			EF_SHADOWDEPTH_NOCACHE        = 0x1000, // Indicates this entity cannot be cached in shadow depthmap and should render every frame
			EF_NOFLASHLIGHT               = 0x2000,
			EF_NOCSM                      = 0x4000, // Indicates this entity does not render into the cascade shadow depthmap
		}


		public enum LifeState_t {
			LIFE_ALIVE       = 0,
			LIFE_DYING       = 1,
			LIFE_DEAD        = 2,
			LIFE_RESPAWNABLE = 3,
			LIFE_DISCARDBODY = 4
		}


		// different for different versions, see below
		public enum Collision_Group_t {
			COLLISION_GROUP_NONE = 0,
			COLLISION_GROUP_DEBRIS,             // Collides with nothing but world and static stuff
			COLLISION_GROUP_DEBRIS_TRIGGER,     // Same as debris, but hits triggers
			COLLISION_GROUP_INTERACTIVE_DEBRIS, // Collides with everything except other interactive debris or debris
			COLLISION_GROUP_INTERACTIVE,        // Collides with everything except interactive debris or debris
			COLLISION_GROUP_PLAYER,
			COLLISION_GROUP_BREAKABLE_GLASS,
			COLLISION_GROUP_VEHICLE,
			COLLISION_GROUP_PLAYER_MOVEMENT, // For HL2, same as Collision_Group_Player, for
			                                 // TF2, this filters out other players and CBaseObjects
			COLLISION_GROUP_NPC,             // Generic NPC group
			COLLISION_GROUP_IN_VEHICLE,      // for any entity inside a vehicle
			COLLISION_GROUP_WEAPON,          // for any weapons that need collision detection
			COLLISION_GROUP_VEHICLE_CLIP,    // vehicle clip brush to restrict vehicle movement
			COLLISION_GROUP_PROJECTILE,      // Projectiles!
			COLLISION_GROUP_DOOR_BLOCKER,    // Blocks entities not permitted to get near moving doors
			COLLISION_GROUP_PASSABLE_DOOR,   // Doors that the player shouldn't collide with
			COLLISION_GROUP_DISSOLVING,      // Things that are dissolving are in this group
			COLLISION_GROUP_PUSHAWAY,        // Nonsolid on client and server, pushaway in player code
			COLLISION_GROUP_NPC_ACTOR,       // Used so NPCs in scripts ignore the player.
			COLLISION_GROUP_NPC_SCRIPTED,    // USed for NPCs in scripts that should not collide with each other
			COLLISION_GROUP_PZ_CLIP,

			// portal 2 only? VVV
			COLLISION_GROUP_CAMERA_SOLID,    // Solid only to the camera's test trace
			COLLISION_GROUP_PLACEMENT_SOLID, // Solid only to the placement tool's test trace
			COLLISION_GROUP_PLAYER_HELD,     // Held objects that shouldn't collide with players
			COLLISION_GROUP_WEIGHTED_CUBE,   // Cubes need a collision group that acts roughly like COLLISION_GROUP_NONE but doesn't collide with debris or interactive
			// portal 2 only? ^^^

			COLLISION_GROUP_DEBRIS_BLOCK_PROJECTILE, // Only collides with bullets
		}

		#region collision group enum lists for different versions

		public static readonly IReadOnlyList<Collision_Group_t> CollisionGroupListOldDemoProtocol = new[] {
			COLLISION_GROUP_NONE,
			COLLISION_GROUP_DEBRIS,
			COLLISION_GROUP_DEBRIS_TRIGGER,
			COLLISION_GROUP_INTERACTIVE_DEBRIS,
			COLLISION_GROUP_INTERACTIVE,
			COLLISION_GROUP_PLAYER,
			COLLISION_GROUP_BREAKABLE_GLASS,
			COLLISION_GROUP_VEHICLE,
			COLLISION_GROUP_PLAYER_MOVEMENT,
			COLLISION_GROUP_NPC,
			COLLISION_GROUP_IN_VEHICLE,
			COLLISION_GROUP_WEAPON,
			COLLISION_GROUP_VEHICLE_CLIP,
			COLLISION_GROUP_PROJECTILE,
			COLLISION_GROUP_DOOR_BLOCKER,
			COLLISION_GROUP_PASSABLE_DOOR,
			COLLISION_GROUP_DISSOLVING,
			COLLISION_GROUP_PUSHAWAY,
			COLLISION_GROUP_NPC_ACTOR,
			COLLISION_GROUP_NPC_SCRIPTED
		};

		public static readonly IReadOnlyList<Collision_Group_t> CollisionGroupListNewDemoProcol = new[] {
			COLLISION_GROUP_NONE,
			COLLISION_GROUP_DEBRIS,
			COLLISION_GROUP_DEBRIS_TRIGGER,
			COLLISION_GROUP_INTERACTIVE_DEBRIS,
			COLLISION_GROUP_INTERACTIVE,
			COLLISION_GROUP_PLAYER,
			COLLISION_GROUP_BREAKABLE_GLASS,
			COLLISION_GROUP_VEHICLE,
			COLLISION_GROUP_PLAYER_MOVEMENT,
			COLLISION_GROUP_NPC,
			COLLISION_GROUP_IN_VEHICLE,
			COLLISION_GROUP_WEAPON,
			COLLISION_GROUP_VEHICLE_CLIP,
			COLLISION_GROUP_PROJECTILE,
			COLLISION_GROUP_DOOR_BLOCKER,
			COLLISION_GROUP_PASSABLE_DOOR,
			COLLISION_GROUP_DISSOLVING,
			COLLISION_GROUP_PUSHAWAY,
			COLLISION_GROUP_NPC_ACTOR,
			COLLISION_GROUP_NPC_SCRIPTED,
			COLLISION_GROUP_PZ_CLIP,
			COLLISION_GROUP_DEBRIS_BLOCK_PROJECTILE
		};

		public static readonly IReadOnlyList<Collision_Group_t> CollisionGroupListPortal2 = new[] {
			COLLISION_GROUP_NONE,
			COLLISION_GROUP_DEBRIS,
			COLLISION_GROUP_DEBRIS_TRIGGER,
			COLLISION_GROUP_INTERACTIVE_DEBRIS,
			COLLISION_GROUP_INTERACTIVE,
			COLLISION_GROUP_PLAYER,
			COLLISION_GROUP_BREAKABLE_GLASS,
			COLLISION_GROUP_VEHICLE,
			COLLISION_GROUP_PLAYER_MOVEMENT,
			COLLISION_GROUP_NPC,
			COLLISION_GROUP_IN_VEHICLE,
			COLLISION_GROUP_WEAPON,
			COLLISION_GROUP_VEHICLE_CLIP,
			COLLISION_GROUP_PROJECTILE,
			COLLISION_GROUP_DOOR_BLOCKER,
			COLLISION_GROUP_PASSABLE_DOOR,
			COLLISION_GROUP_DISSOLVING,
			COLLISION_GROUP_PUSHAWAY,
			COLLISION_GROUP_NPC_ACTOR,
			COLLISION_GROUP_NPC_SCRIPTED,
			COLLISION_GROUP_PZ_CLIP,
			COLLISION_GROUP_CAMERA_SOLID,
			COLLISION_GROUP_PLACEMENT_SOLID,
			COLLISION_GROUP_PLAYER_HELD,
			COLLISION_GROUP_WEIGHTED_CUBE,
			COLLISION_GROUP_DEBRIS_BLOCK_PROJECTILE
		};

		#endregion


		public enum MaterialModifyMode_t {
			MATERIAL_MODIFY_MODE_NONE          = 0,
			MATERIAL_MODIFY_MODE_SETVAR        = 1,
			MATERIAL_MODIFY_MODE_ANIM_SEQUENCE = 2,
			MATERIAL_MODIFY_MODE_FLOAT_LERP    = 3,
		}


		public enum PortalGunEffectState_t {
			EFFECT_NONE,
			EFFECT_READY,
			EFFECT_HOLDING,
		}


		[Flags]
		public enum RopeFlags_t {
			None                   = 0,
			ROPE_RESIZE            = 1,      // Try to keep the rope dangling the same amount
			                                 // even as the rope length changes.
			ROPE_BARBED            = 1 << 1, // Hack option to draw like a barbed wire.
			ROPE_COLLIDE           = 1 << 2, // Collide with the world?
			ROPE_SIMULATE          = 1 << 3, // Is the rope valid?
			ROPE_BREAKABLE         = 1 << 4, // Can the endpoints detach?
			ROPE_NO_WIND           = 1 << 5, // No wind simulation on this rope.
			ROPE_INITIAL_HANG      = 1 << 6, // By default, ropes will simulate for a bit internally when they
			                                 // are created so they sag, but dynamically created ropes for things
			                                 // like harpoons don't want this.
			ROPE_PLAYER_WPN_ATTACH = 1 << 7, // If this flag is set, then the second attachment must be a player.
			                                 // The rope will attach to "buff_attach" on the player's active weapon.
			                                 // (This is a flag because it requires special code on the client to
			                                 // find the weapon).
			ROPE_NO_GRAVITY        = 1 << 8, // Disable gravity on this rope.
			// new
			ROPE_TONGUE_ATTACH     = 1 << 9  // Use the special rules unique to a tongue rope, without cutting open the class to inherit one function.
		}


		[Flags]
		public enum RopeLockedPoints_t {
			None                      = 0,
			ROPE_LOCK_START_POINT     = 0x1,
			ROPE_LOCK_END_POINT       = 0x2,
			ROPE_LOCK_START_DIRECTION = 0x4,
			ROPE_LOCK_END_DIRECTION   = 0x8,
		}


		public enum EnergyCoreState_t {
			ENERGYCORE_STATE_OFF,
			ENERGYCORE_STATE_CHARGING,
			ENERGYCORE_STATE_DISCHARGING,
		}


		[Flags]
		public enum SF_EnergyCore {
			None                       = 0,
			SF_ENERGYCORE_NO_PARTICLES = 1,
			SF_ENERGYCORE_START_ON     = 1 << 1
		}


		[Flags]
		public enum BeamFlags_t {
			None                = 0,
			FBEAM_STARTENTITY   = 0x00001,
			FBEAM_ENDENTITY     = 0x00002,
			FBEAM_FADEIN        = 0x00004,
			FBEAM_FADEOUT       = 0x00008,
			FBEAM_SINENOISE     = 0x00010,
			FBEAM_SOLID         = 0x00020,
			FBEAM_SHADEIN       = 0x00040,
			FBEAM_SHADEOUT      = 0x00080,
			FBEAM_ONLYNOISEONCE = 0x00100, // Only calculate our noise once
			FBEAM_NOTILE        = 0x00200,
			FBEAM_USE_HITBOXES  = 0x00400, // Attachment indices represent hitbox indices instead when this is set.
			FBEAM_STARTVISIBLE  = 0x00800, // Has this client actually seen this beam's start entity yet?
			FBEAM_ENDVISIBLE    = 0x01000, // Has this client actually seen this beam's end entity yet?
			FBEAM_ISACTIVE      = 0x02000,
			FBEAM_FOREVER       = 0x04000,
			FBEAM_HALOBEAM      = 0x08000, // When drawing a beam with a halo, don't ignore the segments and endwidth
			FBEAM_REVERSED      = 0x10000,
		}


		public enum BeamTypes_t {
			BEAM_POINTS = 0,
			BEAM_ENTPOINT,
			BEAM_ENTS,
			BEAM_HOSE,
			BEAM_SPLINE,
			BEAM_LASER
		}


		[Flags]
		public enum HideHudFlags_t {
			None                        = 0,
			HIDEHUD_WEAPONSELECTION     = 1, // Hide ammo count & weapon selection
			HIDEHUD_FLASHLIGHT          = 1 << 1,
			HIDEHUD_ALL                 = 1 << 2,
			HIDEHUD_HEALTH              = 1 << 3, // Hide health & armor / suit battery
			HIDEHUD_PLAYERDEAD          = 1 << 4, // Hide when local player's dead
			HIDEHUD_NEEDSUIT            = 1 << 5, // Hide when the local player doesn't have the HEV suit
			HIDEHUD_MISCSTATUS          = 1 << 6,  // Hide miscellaneous status elements (trains, pickup history, death notices, etc)
			HIDEHUD_CHAT                = 1 << 7,  // Hide all communication elements (saytext, voice icon, etc)
			HIDEHUD_CROSSHAIR           = 1 << 8,  // Hide crosshairs
			HIDEHUD_VEHICLE_CROSSHAIR   = 1 << 9,  // Hide vehicle crosshair
			HIDEHUD_INVEHICLE           = 1 << 10,
			HIDEHUD_BONUS_PROGRESS      = 1 << 11, // Hide bonus progress display (for bonus map challenges)

			// cs only?

			HIDEHUD_RADAR               = 1 << 12, // Hides the radar in CS1.5
			HIDEHUD_MINISCOREBOARD      = 1 << 13  // Hides the miniscoreboard in CS1.5
		}


		public enum PlayerSounds_t {
			PLAYER_SOUNDS_CITIZEN = 0,
			PLAYER_SOUNDS_COMBINESOLDIER,
			PLAYER_SOUNDS_METROPOLICE,
			PLAYER_SOUNDS_MAX,
		}


		// different enums for different versions, see below. these are also flags.
		public enum PlayerMfFlags_t : long { // const.h  line 97
			FL_ONGROUND,              // At rest / on the ground
			FL_DUCKING,               // Player flag -- Player is fully crouched
			FL_WATERJUMP,             // player jumping out of water
			FL_ONTRAIN,               // Player is _controlling_ a train, so movement commands should be ignored on client during prediction.
			FL_INRAIN,                // Indicates the entity is standing in rain
			FL_FROZEN,                // Player is frozen for 3rd person camera
			FL_ATCONTROLS,            // Player can't move, but keeps key inputs for controlling another entity
			FL_CLIENT,                // Is a player
			FL_FAKECLIENT,            // Fake client, simulated server side; don't send network messages to them
			FL_INWATER,               // In water
			FL_FLY,                   // Changes the SV_Movestep() behavior to not need to be on ground
			FL_SWIM,                  // Changes the SV_Movestep() behavior to not need to be on ground (but stay in water)
			FL_CONVEYOR,
			FL_NPC,
			FL_GODMODE,
			FL_NOTARGET,
			FL_AIMTARGET,             // set if the crosshair needs to aim onto the entity
			FL_PARTIALGROUND,         // not all corners are valid
			FL_STATICPROP,            // Eetsa static prop!
			FL_GRAPHED,               // worldgraph has this ent listed as something that blocks a connection
			FL_GRENADE,
			FL_STEPMOVEMENT,          // Changes the SV_Movestep() behavior to not do any processing
			FL_DONTTOUCH,             // Doesn't generate touch functions, generates Untouch() for anything it was touching when this flag was set
			FL_BASEVELOCITY,          // Base velocity has been applied this frame (used to convert base velocity into momentum)
			FL_WORLDBRUSH,            // Not moveable/removeable brush entity (really part of the world, but represented as an entity for transparency or something)
			FL_OBJECT,                // Terrible name. This is an object that NPCs should see. Missiles, for example.
			FL_KILLME,                // This entity is marked for death -- will be freed by game DLL
			FL_ONFIRE,                // You know...
			FL_DISSOLVING,            // We're dissolving!
			FL_TRANSRAGDOLL,          // In the process of turning into a client side ragdoll.
			FL_UNBLOCKABLE_BY_PLAYER, // pusher that can't be blocked by the player
			// new demo protocol only
			FL_ANIMDUCKING,
			FL_AFFECTED_BY_PAINT,
			FL_UNPAINTABLE,           // Unpaintable entities!
			FL_FREEZING,              // We're becoming frozen!
		}

		#region player flag lists for different versions

		public class PlayerMfFlagsOldDemoProtocol : AbstractFlagChecker<PlayerMfFlags_t> {
			public override bool HasFlag(long val, PlayerMfFlags_t flag) {
				return flag switch {
					FL_ONGROUND              => CheckBit(val, 00),
					FL_DUCKING               => CheckBit(val, 01),
					FL_WATERJUMP             => CheckBit(val, 02),
					FL_ONTRAIN               => CheckBit(val, 03),
					FL_INRAIN                => CheckBit(val, 04),
					FL_FROZEN                => CheckBit(val, 05),
					FL_ATCONTROLS            => CheckBit(val, 06),
					FL_CLIENT                => CheckBit(val, 07),
					FL_FAKECLIENT            => CheckBit(val, 08),
					// non-player specific
					FL_INWATER               => CheckBit(val, 09),
					FL_FLY                   => CheckBit(val, 10),
					FL_SWIM                  => CheckBit(val, 11),
					FL_CONVEYOR              => CheckBit(val, 12),
					FL_NPC                   => CheckBit(val, 13),
					FL_GODMODE               => CheckBit(val, 14),
					FL_NOTARGET              => CheckBit(val, 15),
					FL_AIMTARGET             => CheckBit(val, 16),
					FL_PARTIALGROUND         => CheckBit(val, 17),
					FL_STATICPROP            => CheckBit(val, 18),
					FL_GRAPHED               => CheckBit(val, 19),
					FL_GRENADE               => CheckBit(val, 20),
					FL_STEPMOVEMENT          => CheckBit(val, 21),
					FL_DONTTOUCH             => CheckBit(val, 22),
					FL_BASEVELOCITY          => CheckBit(val, 23),
					FL_WORLDBRUSH            => CheckBit(val, 24),
					FL_OBJECT                => CheckBit(val, 25),
					FL_KILLME                => CheckBit(val, 26),
					FL_ONFIRE                => CheckBit(val, 27),
					FL_DISSOLVING            => CheckBit(val, 28),
					FL_TRANSRAGDOLL          => CheckBit(val, 29),
					FL_UNBLOCKABLE_BY_PLAYER => CheckBit(val, 30),
					_ => false
				};
			}
		}

		public class PlayerMfFlagsNewDemoProtocol : AbstractFlagChecker<PlayerMfFlags_t> {
			public override bool HasFlag(long val, PlayerMfFlags_t flag) {
				return flag switch {
					FL_ONGROUND              => CheckBit(val, 00),
					FL_DUCKING               => CheckBit(val, 01),
					FL_ANIMDUCKING           => CheckBit(val, 02),
					FL_WATERJUMP             => CheckBit(val, 03),
					FL_ONTRAIN               => CheckBit(val, 04),
					FL_INRAIN                => CheckBit(val, 05),
					FL_FROZEN                => CheckBit(val, 06),
					FL_ATCONTROLS            => CheckBit(val, 07),
					FL_CLIENT                => CheckBit(val, 08),
					FL_FAKECLIENT            => CheckBit(val, 09),
					// non-player specific
					FL_INWATER               => CheckBit(val, 10),
					FL_FLY                   => CheckBit(val, 11),
					FL_SWIM                  => CheckBit(val, 12),
					FL_CONVEYOR              => CheckBit(val, 13),
					FL_NPC                   => CheckBit(val, 14),
					FL_GODMODE               => CheckBit(val, 15),
					FL_NOTARGET              => CheckBit(val, 16),
					FL_AIMTARGET             => CheckBit(val, 17),
					FL_PARTIALGROUND         => CheckBit(val, 18),
					FL_STATICPROP            => CheckBit(val, 19),
					FL_GRAPHED               => CheckBit(val, 20),
					FL_GRENADE               => CheckBit(val, 21),
					FL_STEPMOVEMENT          => CheckBit(val, 22),
					FL_DONTTOUCH             => CheckBit(val, 23),
					FL_BASEVELOCITY          => CheckBit(val, 24),
					FL_WORLDBRUSH            => CheckBit(val, 25),
					FL_OBJECT                => CheckBit(val, 26),
					FL_KILLME                => CheckBit(val, 27),
					FL_ONFIRE                => CheckBit(val, 28),
					FL_DISSOLVING            => CheckBit(val, 29),
					FL_TRANSRAGDOLL          => CheckBit(val, 30),
					FL_UNBLOCKABLE_BY_PLAYER => CheckBit(val, 31),
					FL_FREEZING              => CheckBit(val, 32),
					_ => false
				};
			}
		}

		// this is from cstrike but it's off for p2
		/*public class PlayerMfFlagsPortal2 : AbstractFlagChecker<PlayerMfFlags_t> {
			public override bool HasFlag(long val, PlayerMfFlags_t flag) {
				return flag switch {
					FL_ONGROUND              => CheckBit(val, 00),
					FL_DUCKING               => CheckBit(val, 01),
					FL_ANIMDUCKING           => CheckBit(val, 02),
					FL_WATERJUMP             => CheckBit(val, 03),
					FL_ONTRAIN               => CheckBit(val, 04),
					FL_INRAIN                => CheckBit(val, 05),
					FL_FROZEN                => CheckBit(val, 06),
					FL_ATCONTROLS            => CheckBit(val, 07),
					FL_CLIENT                => CheckBit(val, 08),
					FL_FAKECLIENT            => CheckBit(val, 09),
					// non-player specific
					FL_INWATER               => CheckBit(val, 10),
					FL_FLY                   => CheckBit(val, 11),
					FL_SWIM                  => CheckBit(val, 12),
					FL_CONVEYOR              => CheckBit(val, 13),
					FL_NPC                   => CheckBit(val, 14),
					FL_GODMODE               => CheckBit(val, 15),
					FL_NOTARGET              => CheckBit(val, 16),
					FL_AIMTARGET             => CheckBit(val, 17),
					FL_PARTIALGROUND         => CheckBit(val, 18),
					FL_STATICPROP            => CheckBit(val, 19),
					FL_AFFECTED_BY_PAINT     => CheckBit(val, 20),
					FL_GRENADE               => CheckBit(val, 21),
					FL_STEPMOVEMENT          => CheckBit(val, 22),
					FL_DONTTOUCH             => CheckBit(val, 23),
					FL_BASEVELOCITY          => CheckBit(val, 24),
					FL_WORLDBRUSH            => CheckBit(val, 25),
					FL_OBJECT                => CheckBit(val, 26),
					FL_KILLME                => CheckBit(val, 27),
					FL_ONFIRE                => CheckBit(val, 28),
					FL_DISSOLVING            => CheckBit(val, 29),
					FL_TRANSRAGDOLL          => CheckBit(val, 30),
					FL_UNBLOCKABLE_BY_PLAYER => CheckBit(val, 31),
					FL_UNPAINTABLE           => CheckBit(val, 32),
					_ => false
				};
			}
		}*/

		public class PlayerMfFlagsPortal2 : AbstractFlagChecker<PlayerMfFlags_t> {
			public override bool HasFlag(long val, PlayerMfFlags_t flag) {
				return flag switch {
					FL_ONGROUND              => CheckBit(val, 00),
					FL_DUCKING               => CheckBit(val, 01),
					FL_WATERJUMP             => CheckBit(val, 02),
					FL_ONTRAIN               => CheckBit(val, 03),
					FL_INRAIN                => CheckBit(val, 04),
					FL_FROZEN                => CheckBit(val, 05),
					FL_ATCONTROLS            => CheckBit(val, 06),
					FL_CLIENT                => CheckBit(val, 07),
					FL_FAKECLIENT            => CheckBit(val, 08),
					FL_INWATER               => CheckBit(val, 09),
					// non-player specific
					FL_FLY                   => CheckBit(val, 10),
					FL_SWIM                  => CheckBit(val, 11),
					FL_CONVEYOR              => CheckBit(val, 12),
					FL_NPC                   => CheckBit(val, 13),
					FL_GODMODE               => CheckBit(val, 14),
					FL_NOTARGET              => CheckBit(val, 15),
					FL_AIMTARGET             => CheckBit(val, 16),
					FL_PARTIALGROUND         => CheckBit(val, 17),
					FL_STATICPROP            => CheckBit(val, 18),
					FL_GRAPHED               => CheckBit(val, 19),
					FL_GRENADE               => CheckBit(val, 20),
					FL_STEPMOVEMENT          => CheckBit(val, 21),
					FL_DONTTOUCH             => CheckBit(val, 22),
					FL_BASEVELOCITY          => CheckBit(val, 23),
					FL_WORLDBRUSH            => CheckBit(val, 24),
					FL_OBJECT                => CheckBit(val, 25),
					FL_KILLME                => CheckBit(val, 26),
					FL_ONFIRE                => CheckBit(val, 27),
					FL_DISSOLVING            => CheckBit(val, 28),
					FL_TRANSRAGDOLL          => CheckBit(val, 29),
					FL_UNBLOCKABLE_BY_PLAYER => CheckBit(val, 30),
					FL_FREEZING              => CheckBit(val, 31),
					_ => false
				};
			}
		}

		#endregion


		[Flags]
		public enum DamageType_t {
			DMG_GENERIC               = 0, // generic damage was done
			DMG_CRUSH                 = 1, // crushed by falling or moving object.
			// NOTE: It's assumed crush damage is occurring as a result of physics collision, so no extra physics force is generated by crush damage.
			// DON'T use DMG_CRUSH when damaging entities unless it's the result of a physics collision. You probably want DMG_CLUB instead.
			DMG_BULLET                = 1 << 1,  // shot
			DMG_SLASH                 = 1 << 2,  // cut, clawed, stabbed
			DMG_BURN                  = 1 << 3,  // heat burned
			DMG_VEHICLE               = 1 << 4,  // hit by a vehicle
			DMG_FALL                  = 1 << 5,  // fell too far
			DMG_BLAST                 = 1 << 6,  // explosive blast damage
			DMG_CLUB                  = 1 << 7,  // crowbar, punch, headbutt
			DMG_SHOCK                 = 1 << 8,  // electric shock
			DMG_SONIC                 = 1 << 9,  // sound pulse shockwave
			DMG_ENERGYBEAM            = 1 << 10, // laser or other high energy beam
			DMG_PREVENT_PHYSICS_FORCE = 1 << 11, // Prevent a physics force
			DMG_NEVERGIB              = 1 << 12, // with this bit OR'd in, no damage type will be able to gib victims upon death
			DMG_ALWAYSGIB             = 1 << 13, // with this bit OR'd in, any damage type can be made to gib victims upon death.
			DMG_DROWN                 = 1 << 14, // Drowning
			DMG_PARALYZE              = 1 << 15, // slows affected creature down
			DMG_NERVEGAS              = 1 << 16, // nerve toxins, very bad
			DMG_POISON                = 1 << 17, // blood poisoning - heals over time like drowning damage
			DMG_RADIATION             = 1 << 18, // radiation exposure
			DMG_DROWNRECOVER          = 1 << 19, // drowning recovery
			DMG_ACID                  = 1 << 20, // toxic chemicals or acid burns
			DMG_SLOWBURN              = 1 << 21, // in an oven
			DMG_REMOVENORAGDOLL       = 1 << 22, // with this bit OR'd in, no ragdoll will be created, and the target will be quietly removed.
			                                     // us, this to kill an entity that you've already got a server-side ragdoll for

			DMG_PHYSGUN               = 1 << 23, // Hit by manipulator. Usually doesn't do any damage.
			DMG_PLASMA                = 1 << 24, // Shot by Cremator
			DMG_AIRBOAT               = 1 << 25, // Hit by the airboat's gun
			DMG_DISSOLVE              = 1 << 26, // Dissolving!
			DMG_BLAST_SURFACE         = 1 << 27, // A blast on the surface of water that cannot harm things underwater
			DMG_DIRECT                = 1 << 28,
			DMG_BUCKSHOT              = 1 << 29 // not quite a bullet. Little, rounder, different.
		}


		public enum SlideshowCycleTypes {
			SLIDESHOW_CYCLE_RANDOM,
			SLIDESHOW_CYCLE_FORWARD,
			SLIDESHOW_CYCLE_BACKWARD,
		}


		public enum WeaponState_t {
			WEAPON_NOT_CARRIED          = 0, // Weapon is on the ground
			WEAPON_IS_CARRIED_BY_PLAYER = 1, // This client is carrying this weapon.
			WEAPON_IS_ACTIVE            = 2, // This client is carrying this weapon and it's the currently held weapon

			WEAPON_IS_ONTARGET          = 0x40 // not sure if this belongs here
		}


		[Flags]
		public enum SF_LightGlow {
			None                     = 0,
			SF_LIGHTGLOW_DIRECTIONAL = 1,
			SF_MODULATE_BY_DIRECTION = 1 << 1
		}


		[Flags]
		public enum SF_BaseTrigger {
			None                                    = 0,
			SF_TRIGGER_ALLOW_CLIENTS                = 0x01,   // Players can fire this trigger
			SF_TRIGGER_ALLOW_NPCS                   = 0x02,   // NPCS can fire this trigger
			SF_TRIGGER_ALLOW_PUSHABLES              = 0x04,   // Pushables can fire this trigger
			SF_TRIGGER_ALLOW_PHYSICS                = 0x08,   // Physics objects can fire this trigger
			SF_TRIGGER_ONLY_PLAYER_ALLY_NPCS        = 0x10,   // *if* NPCs can fire this trigger, this flag means only player allies do so
			SF_TRIGGER_ONLY_CLIENTS_IN_VEHICLES     = 0x20,   // *if* Players can fire this trigger, this flag means only players inside vehicles can
			SF_TRIGGER_ALLOW_ALL                    = 0x40,   // Everything can fire this trigger EXCEPT DEBRIS!
			SF_TRIG_PUSH_ONCE                       = 0x80, // trigger_push removes itself after firing once
			SF_TRIG_PUSH_AFFECT_PLAYER_ON_LADDER    = 0x100,  // if pushed object is player on a ladder, then this disengages them from the ladder (HL2only)
			SF_TRIGGER_ONLY_CLIENTS_OUT_OF_VEHICLES = 0x200,  // *if* Players can fire this trigger, this flag means only players outside vehicles can
			SF_TRIG_TOUCH_DEBRIS                    = 0x400,  // Will touch physics debris objects
			SF_TRIGGER_ONLY_NPCS_IN_VEHICLES        = 0X800,  // *if* NPCs can fire this trigger, only NPCs in vehicles do so (respects player ally flag too)
			// new
			SF_TRIGGER_PUSH_USE_MASS                = 0x1000  // Correctly account for an entity's mass (CTriggerPush::Touch used to assume 100Kg)
		}


		[Flags]
		public enum SF_FuncSmokeVolume {
			None        = 0,
			SF_EMISSIVE = 1
		}


		[Flags]
		public enum SF_SteamJet {
			None        = 0,
			SF_EMISSIVE = 1
		}


		[Flags]
		public enum SF_EnvLightrailEndpoint {
			None                      = 0,
			SF_ENDPOINT_START_SMALLFX = 1,
			SF_ENDPOINT_START_LARGEFX = 2
		}


		public enum EnvLightRailEndpointState {
			ENDPOINT_STATE_OFF,      //No FX displayed
			ENDPOINT_STATE_SMALLFX,  //Just the small particle trail is displayed and a faint glow
			ENDPOINT_STATE_CHARGING, //Ramp up over a certain amount of time to the large bright glow
			ENDPOINT_STATE_LARGEFX,  //Shows a particle trail and a large bright glow
		}


		public enum SteamJetType {
			STEAM_NORMAL   = 0,
			STEAM_HEATWAVE = 1
		}


		[Flags]
		public enum TE_ExplosionFlags {
			TE_EXPLFLAG_NONE            = 0x0,   // all flags clear makes default Half-Life explosion
			TE_EXPLFLAG_NOADDITIVE      = 0x1,   // sprite will be drawn opaque (ensure that the sprite you send is a non-additive sprite)
			TE_EXPLFLAG_NODLIGHTS       = 0x2,   // do not render dynamic lights
			TE_EXPLFLAG_NOSOUND         = 0x4,   // do not play client explosion sound
			TE_EXPLFLAG_NOPARTICLES     = 0x8,   // do not draw particles
			TE_EXPLFLAG_DRAWALPHA       = 0x10,  // sprite will be drawn alpha
			TE_EXPLFLAG_ROTATE          = 0x20,  // rotate the sprite randomly
			TE_EXPLFLAG_NOFIREBALL      = 0x40,  // do not draw a fireball
			TE_EXPLFLAG_NOFIREBALLSMOKE = 0x80,  // do not draw smoke with the fireball
			// new
			TE_EXPLFLAG_ICE             = 0x100, // do ice effects
			TE_EXPLFLAG_SCALEPARTICLES  = 0x200
		}


		public enum ShatterSurface_t {
			// Note: This much match with the client entity
			SHATTERSURFACE_GLASS = 0,
			SHATTERSURFACE_TILE  = 1,
		}


		[Flags]
		public enum DustFlags {
			None                 = 0,
			DUSTFLAGS_ON         = 1 << 0, // emit particles..
			DUSTFLAGS_SCALEMOTES = 1 << 1, // scale to keep the same size on screen
			DUSTFLAGS_FROZEN     = 1 << 2, // just emit m_SpawnRate # of particles and freeze
		}


		[Flags]
		public enum VguiScreenFlags {
			None                              = 0,
			VGUI_SCREEN_ACTIVE                = 0x1,
			VGUI_SCREEN_VISIBLE_TO_TEAMMATES  = 0x2,
			VGUI_SCREEN_ATTACHED_TO_VIEWMODEL = 0x4,
			VGUI_SCREEN_TRANSPARENT           = 0x8,
			VGUI_SCREEN_ONLY_USABLE_BY_OWNER  = 0x10,
		}


		public enum EntityDissolveType {
			ENTITY_DISSOLVE_NORMAL = 0,
			ENTITY_DISSOLVE_ELECTRICAL,
			ENTITY_DISSOLVE_ELECTRICAL_LIGHT,
			ENTITY_DISSOLVE_CORE,
		}


		public enum PoseController_FModType_t {
			POSECONTROLLER_FMODTYPE_NONE = 0,
			POSECONTROLLER_FMODTYPE_SINE,
			POSECONTROLLER_FMODTYPE_SQUARE,
			POSECONTROLLER_FMODTYPE_TRIANGLE,
			POSECONTROLLER_FMODTYPE_SAWTOOTH,
			POSECONTROLLER_FMODTYPE_NOISE,
		}


		public enum PrecipitationType_t {
			PRECIPITATION_TYPE_RAIN = 0,
			PRECIPITATION_TYPE_SNOW,
			PRECIPITATION_TYPE_ASH,
			PRECIPITATION_TYPE_SNOWFALL,
			// new
			PRECIPITATION_TYPE_PARTICLERAIN,
			PRECIPITATION_TYPE_PARTICLEASH,
			PRECIPITATION_TYPE_PARTICLERAINSTORM,
			PRECIPITATION_TYPE_PARTICLESNOW,
		}


		public enum EpisodicRadarContactType {
			RADAR_CONTACT_NONE = -1,
			RADAR_CONTACT_GENERIC = 0,
			RADAR_CONTACT_MAGNUSSEN_RDU,
			RADAR_CONTACT_DOG,
			RADAR_CONTACT_ALLY_INSTALLATION,
			RADAR_CONTACT_ENEMY,       // 'regular' sized enemy (Hunter)
			RADAR_CONTACT_LARGE_ENEMY, // Large enemy (Strider)
		}
	}
}
