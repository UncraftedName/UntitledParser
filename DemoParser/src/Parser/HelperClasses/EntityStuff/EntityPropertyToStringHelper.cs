using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using DemoParser.Utils;

namespace DemoParser.Parser.HelperClasses.EntityStuff {

	internal static class EntPropToStringHelper {

		internal static readonly Exception E =
			new ArgumentException("bro a property doesn't have an implemented display type wtf");
		
		private static readonly Regex HandleMatcher = new Regex("^m_h[A-Z]", RegexOptions.Compiled);
		private static readonly Regex BoolMatcher = new Regex("^(m_)?b[A-Z]", RegexOptions.Compiled);


		internal static DisplayType IdentifyTypeForInt(string name, SendTableProp propInfo) {
			if (propInfo.NumBits == 32 && name.Contains("color", StringComparison.OrdinalIgnoreCase))
				return DisplayType.Color;
			string[] split = name.Split('.');
			
			// check for flags and special cases first
			DisplayType tmp = EntityPropertyEnumManager.DetermineSpecialType(propInfo.Name, propInfo.TableRef.Name);
			if (tmp != DisplayType.Int) // default return from above method
				return tmp;
			
			if (propInfo.NumBits == 21 && split.Any(s => HandleMatcher.IsMatch(s))) // num bits depends on max edict bits 
				return DisplayType.Handle;

			// bool check is kinda dumb, maybe I shouldn't bother tbh
			if (propInfo.NumBits == 1) {
				if (split.Any(s => BoolMatcher.IsMatch(s))
				|| name.Contains("Has")
				|| name.Contains("Is")
				|| name.Contains("disable", StringComparison.OrdinalIgnoreCase)
				|| name.Contains("enable", StringComparison.OrdinalIgnoreCase))
					return DisplayType.Bool;
			}
			
			return DisplayType.Int;
		}


		internal static DisplayType IdentifyTypeForFloat(string name) {
			return DisplayType.Float;
		}


		internal static DisplayType IdentifyTypeForVec3(string name) {
			string[] split = name.Split('.');
			return split.Any(s => s.StartsWith("m_ang")) ? DisplayType.Angles : DisplayType.Vector3;
		}


		internal static DisplayType IdentifyTypeForVec2(string name) {
			return DisplayType.Vector2;
		}


		internal static DisplayType IdentifyTypeForString(string name) {
			return DisplayType.String;
		}


		internal static string CreateIntPropStr(int val, DisplayType displayType) {
			switch (displayType) {
				case DisplayType.Int:
					return val.ToString();
				case DisplayType.Bool:
					return (val != 0).ToString();
				case DisplayType.Color:
					return $"0x{val:X8}";
				case DisplayType.Handle:
					return val.IsNullEHandle() 
						? "null"
						: $"(index: {val & (DemoSettings.MaxEdicts - 1)}, serial: {val >> DemoSettings.MaxEdictBits})";
				default:
					return EntityPropertyEnumManager.CreateEnumPropStr(val, displayType); // must be last
			}
		}


		internal static string CreateFloatPropStr(float val, DisplayType displayType) {
			return val.ToString(CultureInfo.InvariantCulture);
		}


		internal static string CreateVec3PropStr(in Vector3 val, DisplayType displayType) {
			switch (displayType) {
				case DisplayType.Vector3:
					return val.ToString();
				case DisplayType.Angles:
					return $"<{val.X}°, {val.Y}°, {val.Z}°>"; // i want full precision
				default:
					throw E;
			}
		}


		internal static string CreateVec2PropStr(in Vector2 val, DisplayType displayType) {
			return val.ToString();
		}


		internal static string CreateStrPropStr(string val, DisplayType displayType) {
			return val;
		}
	}


	// todo CBaseTrigger.m_spawnflags
	
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	internal enum DisplayType { // don't rely on order here
		Int,
		Float,
		Vector2,
		Vector3,
		// Index, <- this might be worth looking into to display the textures used and so forth
		Bool,
		String,
		Angles,
		Handle,
		Color,
		// flags and enums
		DT_EffectData__m_fFlags,
		DT_BaseGrenade__m_fFlags,
		DT_BasePlayer__m_fFlags,
		DT_Env_Lightrail_Endpoint__m_spawnflags,
		DT_FireSmoke__m_nFlags,
		DT_Func_Dust__m_DustFlags,
		DT_FuncSmokeVolume__m_spawnflags,
		DT_LightGlow__m_spawnflags,
		DT_Plasma__m_nFlags,
		DT_RopeKeyframe__m_RopeFlags,
		DT_RopeKeyframe__m_fLockedPoints,
		DT_SteamJet__m_spawnflags,
		DT_BaseBeam__m_nFlags,
		DT_TEBreakModel__m_nFlags,
		DT_TEExplosion__m_nFlags,
		DT_TEPhysicsProp__m_nFlags,
		DT_VGuiScreen__m_fScreenFlags,
		DT_CollisionProperty__m_usSolidFlags,
		DT_CollisionProperty__m_nSolidType,
		DT_BaseEntity__movetype,
		DT_BaseEntity__movecollide,
		m_nRenderFX,
		m_nRenderMode,
		DT_BaseEntity__m_fEffects,
		DT_BaseEntity__m_CollisionGroup,
		DT_Beam__m_nBeamFlags,
		DT_Beam__m_nBeamType,
		DT_BreakableSurface__m_nSurfaceType,
		DT_EntityDissolve__m_nDissolveType,
		DT_EnvScreenEffect__m_nType,
		DT_Portal_Player__m_iPlayerSoundType,
		DT_PoseController__m_nFModType,
		DT_Precipitation__m_nPrecipType,
		DT_SlideshowDisplay__m_iCycleType,
		DT_SteamJet__m_nType,
		DT_TEExplosion__m_chMaterialType,
		DT_TEFootprintDecal__m_chMaterialType,
		DT_TEGaussExplosion__m_nType,
		DT_TEImpact__m_iType,
		DT_TEMuzzleFlash__m_nType,
		DT_TEShatterSurface__m_nSurfaceType,
		DT_CollisionProperty__m_nSurroundType,
		DT_LocalWeaponData__m_iPrimaryAmmoType,
		DT_LocalWeaponData__m_iSecondaryAmmoType,
		DT_EffectData__m_nDamageType,
		m_lifeState,
		DT_MaterialModifyControl__m_nModifyMode,
		DT_WeaponPortalgun__m_EffectState,
		DT_CitadelEnergyCore__m_spawnflags,
		DT_CitadelEnergyCore__m_nState,
		DT_Local__M_iHideHUD,
		DT_BaseCombatWeapon__m_iState,
	}
}
