using System;
using static DemoParser.Parser.HelperClasses.EntityStuff.DisplayType;

// yes i know everything is gonna have terrible names

// ReSharper disable CommentTypo
// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo
// ReSharper disable InconsistentNaming

// basically another toString() helper for ent props, aimed specifically at special cases with ints and enums/flags 
namespace DemoParser.Parser.HelperClasses.EntityStuff {
	
	internal static class EntityPropertyEnumManager {

		// I only enumerate through the enums that are actually implemented below.
		// Also, this can contain any special cases (only for int types).
		internal static DisplayType DetermineSpecialType(string propName, string tableName) {
			return propName switch { // special cases for prop names that are used for more than one entity
				"moveparent"    => Handle,
				"m_clrRender"   => Color,
				"m_lifeState"   => m_lifeState,
				"m_nRenderFX"   => m_nRenderFX,
				"m_nRenderMode" => m_nRenderMode,
				_ => // i should probably create a lookup dictionary for this
				(tableName switch { // flags, enums, and table-specific special cases
					"DT_PlayerState"           when propName == "deadflag"            => Bool,
					"DT_Portal_Player"         when propName == "m_pHeldObjectPortal" => Handle,
					"DT_Local"                 when propName == "m_audio.ent"         => Handle,
					"DT_CollisionProperty"     when propName == "m_usSolidFlags"      => DT_CollisionProperty__m_usSolidFlags,
					"DT_CollisionProperty"     when propName == "m_nSurroundType"     => DT_CollisionProperty__m_nSurroundType,
					"DT_CollisionProperty"     when propName == "m_nSolidType"        => DT_CollisionProperty__m_nSolidType,
					"DT_BaseEntity"            when propName == "movetype"            => DT_BaseEntity__movetype,
					"DT_BaseEntity"            when propName == "m_fEffects"          => DT_BaseEntity__m_fEffects,
					"DT_BaseEntity"            when propName == "movecollide"         => DT_BaseEntity__movecollide,
					"DT_BaseEntity"            when propName == "m_CollisionGroup"    => DT_BaseEntity__m_CollisionGroup,
					"DT_MaterialModifyControl" when propName == "m_nModifyMode"       => DT_MaterialModifyControl__m_nModifyMode,
					"DT_WeaponPortalgun"       when propName == "m_EffectState"       => DT_WeaponPortalgun__m_EffectState,
					"DT_RopeKeyframe"          when propName == "m_RopeFlags"         => DT_RopeKeyframe__m_RopeFlags,
					"DT_RopeKeyframe"          when propName == "m_fLockedPoints"     => DT_RopeKeyframe__m_fLockedPoints,
					"DT_CitadelEnergyCore"     when propName == "m_spawnflags"        => DT_CitadelEnergyCore__m_spawnflags,
					"DT_CitadelEnergyCore"     when propName == "m_nState"            => DT_CitadelEnergyCore__m_nState,
					"DT_Beam"                  when propName == "m_nBeamFlags"        => DT_Beam__m_nBeamFlags,
					"DT_Beam"                  when propName == "m_nBeamType"         => DT_Beam__m_nBeamType,
					"DT_Local"                 when propName == "m_iHideHUD"          => DT_Local__M_iHideHUD,
					"DT_Portal_Player"         when propName == "m_iPlayerSoundType"  => DT_Portal_Player__m_iPlayerSoundType,
					"DT_BasePlayer"            when propName == "m_fFlags"            => DT_BasePlayer__m_fFlags,
					"DT_EffectData"            when propName == "m_nDamageType"       => DT_EffectData__m_nDamageType,
					"DT_SlideshowDisplay"      when propName == "m_iCycleType"        => DT_SlideshowDisplay__m_iCycleType,
					"DT_BaseCombatWeapon"      when propName == "m_iState"            => DT_BaseCombatWeapon__m_iState,
					_ => Int
				})
			};
		}


		// I want all the flags to have some end and beginning, (such as parenthesis) just in case they're used in arrays
		internal static string CreateEnumPropStr(int val, DisplayType displayType) {
			return displayType switch {
				DT_CollisionProperty__m_usSolidFlags    => "(" + (SolidFlags_t)val + ")",
				DT_CollisionProperty__m_nSurroundType   => ((SurroundingBoundsType_t)val).ToString(),
				DT_CollisionProperty__m_nSolidType      => ((SolidType_t)val).ToString(),
				DT_BaseEntity__movetype                 => ((MoveType_t)val).ToString(),
				DT_BaseEntity__movecollide              => ((MoveCollide_t)val).ToString(),
				m_nRenderFX                             => ((RenderFx_t)val).ToString(),
				m_nRenderMode                           => ((RenderMode_t)val).ToString(),
				DT_BaseEntity__m_fEffects               => "(" + (EntityEffects_t)val + ")",
				m_lifeState                             => ((LifeState_t)val).ToString(),
				DT_BaseEntity__m_CollisionGroup         => ((Collision_Group_t)val).ToString(),
				DT_MaterialModifyControl__m_nModifyMode => ((MaterialModifyMode_t)val).ToString(),
				DT_WeaponPortalgun__m_EffectState       => ((PortalGunEffectState_t)val).ToString(),
				DT_RopeKeyframe__m_RopeFlags            => "(" + (RopeFlags_t)val + ")",
				DT_RopeKeyframe__m_fLockedPoints        => "(" + (RopeLockedPoints_t)val + ")",
				DT_CitadelEnergyCore__m_nState          => ((EnergyCoreState_t)val).ToString(),
				DT_CitadelEnergyCore__m_spawnflags      => "(" + (EnergyCoreSpawnFlags_t)val + ")",
				DT_Beam__m_nBeamFlags                   => "(" + (BeamFlags_t)val + ")",
				DT_Beam__m_nBeamType                    => ((BeamTypes_t)val).ToString(),
				DT_Local__M_iHideHUD                    => "(" + (HideHudFlags_t)val + ")",
				DT_Portal_Player__m_iPlayerSoundType    => ((PlayerSounds_t)val).ToString(),
				DT_BasePlayer__m_fFlags                 => "(" + (PlayerMfFlags_t)val + ")",
				DT_EffectData__m_nDamageType            => "(" + (DamageType_t)val + ")",
				DT_SlideshowDisplay__m_iCycleType       => ((SlideshowCycleTypes)val).ToString(),
				DT_BaseCombatWeapon__m_iState           => ((WeaponState_t)val).ToString(),
				_ => throw EntPropToStringHelper.E
			};
		}
	}
	
	// all flags should have a "None" or equivalent.
	// in theory these types can be different depending on the game, lets hope they aren't
	
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
		SOLID_NONE     = 0, // no solid model
		SOLID_BSP      = 1, // a BSP tree
		SOLID_BBOX     = 2, // an AABB
		SOLID_OBB      = 3, // an OBB (not implemented yet)
		SOLID_OBB_YAW  = 4, // an OBB, constrained so that it can only yaw
		SOLID_CUSTOM   = 5, // Always call into the entity for tests
		SOLID_VPHYSICS = 6, // solid vphysics object, get vcollide from the model and collide with that
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
	
	
	public enum MoveCollide_t { // edict->movecollide values
		MOVECOLLIDE_DEFAULT = 0,
		// These ones only work for MOVETYPE_FLY + MOVETYPE_FLYGRAVITY
		MOVECOLLIDE_FLY_BOUNCE, // bounces, reflects, based on elasticity of surface and object - applies friction (adjust velocity)
		MOVECOLLIDE_FLY_CUSTOM, // Touch() will modify the velocity however it likes
		MOVECOLLIDE_FLY_SLIDE,  // slides along surfaces (no bounce) - applies friction (adjusts velocity)
		MOVECOLLIDE_COUNT, // Number of different movecollides
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
		EF_NOINTERP	          = 0x008, // don't interpolate the next frame
		EF_NOSHADOW	          = 0x010, // Don't cast no shadow
		EF_NODRAW             = 0x020, // don't draw entity
		EF_NORECEIVESHADOW    = 0x040, // Don't receive no shadow
		EF_BONEMERGE_FASTCULL = 0x080, // For use with EF_BONEMERGE. If this is set, then it places this ent's origin at its
		                               // parent and uses the parent's bbox + the max extents of the aiment.
		                               // Otherwise, it sets up the parent's bones every frame to figure out where to place
		                               // the aiment, which is inefficient because it'll setup the parent's bones even if
		                               // the parent is not in the PVS.
		EF_ITEM_BLINK         = 0x100, // blink an item so that the user notices it.
		EF_PARENT_ANIMATES    = 0x200, // always assume that the parent entity is animating
	}


	public enum LifeState_t {
		LIFE_ALIVE       = 0,
		LIFE_DYING       = 1,
		LIFE_DEAD        = 2,
		LIFE_RESPAWNABLE = 3,
		LIFE_DISCARDBODY = 4
	}
	
	
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
		COLLISION_GROUP_NPC_SCRIPTED     // USed for NPCs in scripts that should not collide with each other
	}
	
	
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
		ROPE_NO_GRAVITY        = 1 << 8  // Disable gravity on this rope.
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
	public enum EnergyCoreSpawnFlags_t {
		None                       = 0,
		SF_ENERGYCORE_NO_PARTICLES = 1,
		SF_ENERGYCORE_START_ON     = 1 << 1
	}


	[Flags]
	public enum BeamFlags_t {
		None                = 0,
		FBEAM_STARTENTITY   = 0x00000001,
		FBEAM_ENDENTITY     = 0x00000002,
		FBEAM_FADEIN        = 0x00000004,
		FBEAM_FADEOUT       = 0x00000008,
		FBEAM_SINENOISE     = 0x00000010,
		FBEAM_SOLID         = 0x00000020,
		FBEAM_SHADEIN       = 0x00000040,
		FBEAM_SHADEOUT      = 0x00000080,
		FBEAM_ONLYNOISEONCE = 0x00000100, // Only calculate our noise once
		FBEAM_NOTILE        = 0x00000200,
		FBEAM_USE_HITBOXES  = 0x00000400, // Attachment indices represent hitbox indices instead when this is set.
		FBEAM_STARTVISIBLE  = 0x00000800, // Has this client actually seen this beam's start entity yet?
		FBEAM_ENDVISIBLE    = 0x00001000, // Has this client actually seen this beam's end entity yet?
		FBEAM_ISACTIVE      = 0x00002000,
		FBEAM_FOREVER       = 0x00004000,
		FBEAM_HALOBEAM      = 0x00008000, // When drawing a beam with a halo, don't ignore the segments and endwidth
		FBEAM_REVERSED      = 0x00010000,
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
		HIDEHUD_WEAPONSELECTION     = 1,       // Hide ammo count & weapon selection
		HIDEHUD_FLASHLIGHT          = 1 << 1,  
		HIDEHUD_ALL                 = 1 << 2,  
		HIDEHUD_HEALTH              = 1 << 3,  // Hide health & armor / suit battery
		HIDEHUD_PLAYERDEAD          = 1 << 4,  // Hide when local player's dead
		HIDEHUD_NEEDSUIT            = 1 << 5,  // Hide when the local player doesn't have the HEV suit
		HIDEHUD_MISCSTATUS          = 1 << 6,  // Hide miscellaneous status elements (trains, pickup history, death notices, etc)
		HIDEHUD_CHAT                = 1 << 7,  // Hide all communication elements (saytext, voice icon, etc)
		HIDEHUD_CROSSHAIR           = 1 << 8,  // Hide crosshairs
		HIDEHUD_VEHICLE_CROSSHAIR   = 1 << 9,  // Hide vehicle crosshair
		HIDEHUD_INVEHICLE           = 1 << 10, 
		HIDEHUD_BONUS_PROGRESS      = 1 << 11  // Hide bonus progress display (for bonus map challenges)
	}


	public enum PlayerSounds_t {
		PLAYER_SOUNDS_CITIZEN = 0,
		PLAYER_SOUNDS_COMBINESOLDIER,
		PLAYER_SOUNDS_METROPOLICE,
		PLAYER_SOUNDS_MAX, 
	}


	[Flags]
	public enum PlayerMfFlags_t { // const.h  line 97
		None            = 0,
		FL_ONGROUND     = 1,      // At rest / on the ground
		FL_DUCKING      = 1 << 1, // Player flag -- Player is fully crouched
		FL_WATERJUMP    = 1 << 2, // player jumping out of water
		FL_ONTRAIN      = 1 << 3, // Player is _controlling_ a train, so movement commands should be ignored on client during prediction.
		FL_INRAIN       = 1 << 4, // Indicates the entity is standing in rain
		FL_FROZEN       = 1 << 5, // Player is frozen for 3rd person camera
		FL_ATCONTROLS   = 1 << 6, // Player can't move, but keeps key inputs for controlling another entity
		FL_CLIENT       = 1 << 7, // Is a player
		FL_FAKECLIENT   = 1 << 8, // Fake client, simulated server side; don't send network messages to them
	}


	[Flags]
	public enum DamageType_t {
		DMG_GENERIC			        = 0,	   // generic damage was done
		DMG_CRUSH			        = 1,       // crushed by falling or moving object. 
		                                       // NOTE: It's assumed crush damage is occurring as a result of physics collision, so no extra physics force is generated by crush damage.
		                                       // DON'T use DMG_CRUSH when damaging entities unless it's the result of a physics collision. You probably want DMG_CLUB instead.
		DMG_BULLET			        = 1 << 1,  // shot
		DMG_SLASH			        = 1 << 2,  // cut, clawed, stabbed
		DMG_BURN			        = 1 << 3,  // heat burned
		DMG_VEHICLE			        = 1 << 4,  // hit by a vehicle
		DMG_FALL			        = 1 << 5,  // fell too far
		DMG_BLAST			        = 1 << 6,  // explosive blast damage
		DMG_CLUB			        = 1 << 7,  // crowbar, punch, headbutt
		DMG_SHOCK			        = 1 << 8,  // electric shock
		DMG_SONIC			        = 1 << 9,  // sound pulse shockwave
		DMG_ENERGYBEAM		        = 1 << 10, // laser or other high energy beam 
		DMG_PREVENT_PHYSICS_FORCE   = 1 << 11, // Prevent a physics force 
		DMG_NEVERGIB		        = 1 << 12, // with this bit OR'd in, no damage type will be able to gib victims upon death
		DMG_ALWAYSGIB		        = 1 << 13, // with this bit OR'd in, any damage type can be made to gib victims upon death.
		DMG_DROWN			        = 1 << 14, // Drowning
		DMG_PARALYZE		        = 1 << 15, // slows affected creature down
		DMG_NERVEGAS		        = 1 << 16, // nerve toxins, very bad
		DMG_POISON			        = 1 << 17, // blood poisoning - heals over time like drowning damage
		DMG_RADIATION		        = 1 << 18, // radiation exposure
		DMG_DROWNRECOVER	        = 1 << 19, // drowning recovery
		DMG_ACID			        = 1 << 20, // toxic chemicals or acid burns
		DMG_SLOWBURN		        = 1 << 21, // in an oven
		DMG_REMOVENORAGDOLL	        = 1 << 22, // with this bit OR'd in, no ragdoll will be created, and the target will be quietly removed.
		                                       // us, this to kill an entity that you've already got a server-side ragdoll for
					
		DMG_PHYSGUN			        = 1 << 23, // Hit by manipulator. Usually doesn't do any damage.
		DMG_PLASMA			        = 1 << 24, // Shot by Cremator
		DMG_AIRBOAT			        = 1 << 25, // Hit by the airboat's gun
		DMG_DISSOLVE		        = 1 << 26, // Dissolving!
		DMG_BLAST_SURFACE	        = 1 << 27, // A blast on the surface of water that cannot harm things underwater
		DMG_DIRECT			        = 1 << 28, 
		DMG_BUCKSHOT		        = 1 << 29  // not quite a bullet. Little, rounder, different.
	}
	
	
	public enum SlideshowCycleTypes {
		SLIDESHOW_CYCLE_RANDOM,
		SLIDESHOW_CYCLE_FORWARD,
		SLIDESHOW_CYCLE_BACKWARD,
	}


	public enum WeaponState_t {
		WEAPON_NOT_CARRIED          = 0, // Weapon is on the ground
		WEAPON_IS_CARRIED_BY_PLAYER	= 1, // This client is carrying this weapon.
		WEAPON_IS_ACTIVE            = 2, // This client is carrying this weapon and it's the currently held weapon
		
		WEAPON_IS_ONTARGET          = 0x40 // not sure if this belongs here
	}
}
