using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;
using static DemoParser.Parser.HelperClasses.EntityStuff.PropToStringHelper;
using static DemoParser.Parser.HelperClasses.EntityStuff.SendPropEnums;

namespace DemoParser.Parser.HelperClasses.EntityStuff {
	
	/* Every type of entity property gets a 'display type' which determines how the ToString() representation will be
	 * displayed. Actually determining this type is not the fastest thing in the world considering that every
	 * EntityUpdate ToString() needs to use it. So I have a lookup which has the type if it has already been determined
	 * for this property based on the FlattenedProp it references. Those fProps should be unique and initialized only
	 * once for every type of prop that appears in the demo, so the lookup is a dict that uses reference comparisons
	 * like java's IdentityHashMap.
	 *
	 * The reason I use a struct here is because every entity update has a list of these properties, and a typical
	 * load is several dozen per tick just from animation timers and such in the demo (this can increase to several
	 * hundreds of prop deltas when there's a lot of activity). By avoiding a lot of heap allocations we get a
	 * significant speed increase, at the cost of maintainability of course :p.
	 */
	
	[StructLayout(LayoutKind.Explicit, Size = 40)]
	public struct EntityProperty : IAppendable {
		[FieldOffset(0)] public int Offset;
		[FieldOffset(4)] public int BitLength;
		[FieldOffset(8)] public readonly FlattenedProp PropInfo;
		// objects must be separate from values otherwise you'll get a TypeLoadException
		[FieldOffset(16)] private readonly string _strVal;
		[FieldOffset(16)] private readonly List<int> _intArrVal;
		[FieldOffset(16)] private readonly List<float> _floatArrVal;
		[FieldOffset(16)] private readonly List<Vector2> _vec2ArrVal;
		[FieldOffset(16)] private readonly List<Vector3> _vec3ArrVal;
		[FieldOffset(16)] private readonly List<string> _strArrVal;
		[FieldOffset(24)] private readonly int _intVal;
		[FieldOffset(24)] private readonly float _floatVal;
		[FieldOffset(24)] private readonly Vector2 _vec2Val;
		[FieldOffset(24)] private readonly Vector3 _vec3Val;
		[FieldOffset(36)] public readonly PType PropType;
		
		public int AsInt               {get {if (PropType == PType.Int)      return _intVal;      throw InvTypeE;}}
		public float AsFloat           {get {if (PropType == PType.Float)    return _floatVal;    throw InvTypeE;}}
		public Vector2 AsVec2          {get {if (PropType == PType.Vec2)     return _vec2Val;     throw InvTypeE;}}
		public Vector3 AsVec3          {get {if (PropType == PType.Vec3)     return _vec3Val;     throw InvTypeE;}}
		public string AsStr            {get {if (PropType == PType.Str)      return _strVal;      throw InvTypeE;}}
		public List<int> AsIntArr      {get {if (PropType == PType.IntArr)   return _intArrVal;   throw InvTypeE;}}
		public List<float> AsFloatArr  {get {if (PropType == PType.FloatArr) return _floatArrVal; throw InvTypeE;}}
		public List<Vector2> AsVec2Arr {get {if (PropType == PType.Vec2Arr)  return _vec2ArrVal;  throw InvTypeE;}}
		public List<Vector3> AsVec3Arr {get {if (PropType == PType.Vec3Arr)  return _vec3ArrVal;  throw InvTypeE;}}
		public List<string> AsStrArr   {get {if (PropType == PType.StrArr)   return _strArrVal;   throw InvTypeE;}}

		public DemoSettings DemoSettings => PropInfo.DemoSettings;
		public string Name => PropInfo.Name;
		public bool IsArrayProp => PropType >= PType.IntArr;

		private static readonly Exception InvTypeE = new InvalidOperationException("ent prop is wrong PropType");
		private static readonly Exception BadTypeE = new ArgumentException("invalid ent prop PropType");

		private static readonly IDictionary<FlattenedProp, DisplayType> DisplayLookup =
			new Dictionary<FlattenedProp, DisplayType>(1000, new ParserUtils.ReferenceComparer<FlattenedProp>());


		private EntityProperty(FlattenedProp fProp, int offset, int bitLen) : this() {
			PropInfo = fProp;
			Offset = offset;
			BitLength = bitLen;
		}
		
		public EntityProperty(FlattenedProp fProp) : this(fProp, -1, -1) {
			PropType = PType.Unparsed;
		}

		public EntityProperty(FlattenedProp fProp, int offset, int bitLen, int val) : this(fProp, offset, bitLen) {
			_intVal = val;
			PropType = PType.Int;
		}
		
		public EntityProperty(FlattenedProp fProp, int offset, int bitLen, float val) : this(fProp, offset, bitLen) {
			_floatVal = val;
			PropType = PType.Float;
		}
		
		public EntityProperty(FlattenedProp fProp, int offset, int bitLen, ref Vector2 val) : this(fProp, offset, bitLen) {
			_vec2Val = val;
			PropType = PType.Vec2;
		}
		
		public EntityProperty(FlattenedProp fProp, int offset, int bitLen, ref Vector3 val) : this(fProp, offset, bitLen) {
			_vec3Val = val;
			PropType = PType.Vec3;
		}
		
		public EntityProperty(FlattenedProp fProp, int offset, int bitLen, string val) : this(fProp, offset, bitLen) {
			_strVal = val;
			PropType = PType.Str;
		}
		
		public EntityProperty(FlattenedProp fProp, int offset, int bitLen, List<int> val) : this(fProp, offset, bitLen) {
			_intArrVal = val;
			PropType = PType.IntArr;
		}
		
		public EntityProperty(FlattenedProp fProp, int offset, int bitLen, List<float> val) : this(fProp, offset, bitLen) {
			_floatArrVal = val;
			PropType = PType.FloatArr;
		}
		
		public EntityProperty(FlattenedProp fProp, int offset, int bitLen, List<Vector2> val) : this(fProp, offset, bitLen) {
			_vec2ArrVal = val;
			PropType = PType.Vec2Arr;
		}
		
		public EntityProperty(FlattenedProp fProp, int offset, int bitLen, List<Vector3> val) : this(fProp, offset, bitLen) {
			_vec3ArrVal = val;
			PropType = PType.Vec3Arr;
		}
		
		public EntityProperty(FlattenedProp fProp, int offset, int bitLen, List<string> val) : this(fProp, offset, bitLen) {
			_strArrVal = val;
			PropType = PType.StrArr;
		}
		
		
		public void CopyPropertyTo(ref EntityProperty other) {
			if (PropType != other.PropType)
				throw new ArgumentException("attempted to copy an entity property to a different type");
			if (PropType == PType.Unparsed)
				throw new InvalidOperationException("unparsed property attempted to be copied to another property");
			if (IsArrayProp) {
				other.Offset = Offset;
				other.BitLength = BitLength;
				switch (PropType) {
					case PType.IntArr:
						for (int i = 0; i < _intArrVal.Count; i++) {
							if (i >= other._intArrVal.Count)
								other._intArrVal.Add(_intArrVal[i]);
							else
								other._intArrVal[i] = _intArrVal[i];
						}
						break;
					case PType.FloatArr:
						for (int i = 0; i < _floatArrVal.Count; i++) {
							if (i >= other._floatArrVal.Count)
								other._floatArrVal.Add(_floatArrVal[i]);
							else
								other._floatArrVal[i] = _floatArrVal[i];
						}
						break;
					case PType.Vec2Arr:
						for (int i = 0; i < _vec2ArrVal.Count; i++) {
							if (i >= other._vec2ArrVal.Count)
								other._vec2ArrVal.Add(_vec2ArrVal[i]);
							else
								other._vec2ArrVal[i] = _vec2ArrVal[i];
						}
						break;
					case PType.Vec3Arr:
						for (int i = 0; i < _vec3ArrVal.Count; i++) {
							if (i >= other._vec3ArrVal.Count)
								other._vec3ArrVal.Add(_vec3ArrVal[i]);
							else
								other._vec3ArrVal[i] = _vec3ArrVal[i];
						}
						break;
					case PType.StrArr:
						for (int i = 0; i < _strArrVal.Count; i++) {
							if (i >= other._strArrVal.Count)
								other._strArrVal.Add(_strArrVal[i]);
							else
								other._strArrVal[i] = _strArrVal[i];
						}
						break;
					default:
						throw BadTypeE;
				}
			} else {
				other = this;
			}
		}
		
		
		public void AppendToWriter(IndentedWriter iw) {
			int tmp = iw.LastLineLength;
			iw.Append(PropInfo.TypeString());
			iw.PadLastLine(tmp + 12, ' ');
			iw.Append($"{Name}: ");
			iw.Append(PropToString());
		}


		public override string ToString() {
			return AppendableClass.AppendHelper(this);
		}


		private string PropToString() {
			DisplayType dispType;
			if (PropType == PType.Unparsed) {
				return "UNPARSED_DATA";
			} else if (!DisplayLookup.TryGetValue(PropInfo, out dispType)) { 
				dispType = PropType switch {
					PType.Int      => IdentifyTypeForInt(Name, PropInfo.PropInfo),
					PType.Float    => IdentifyTypeForFloat(Name),
					PType.Vec2     => IdentifyTypeForVec2(Name),
					PType.Vec3     => IdentifyTypeForVec3(Name),
					PType.Str      => IdentifyTypeForString(Name),
					PType.IntArr   => IdentifyTypeForInt(Name, PropInfo.ArrayElementPropInfo!),
					PType.FloatArr => IdentifyTypeForFloat(Name),
					PType.Vec2Arr  => IdentifyTypeForVec2(Name),
					PType.Vec3Arr  => IdentifyTypeForVec3(Name),
					PType.StrArr   => IdentifyTypeForString(Name),
					_ => throw BadTypeE
				};
				DisplayLookup[PropInfo] = dispType;
			}
			DemoSettings ds = DemoSettings;
			return PropType switch {
				PType.Int      => CreateIntPropStr(_intVal, dispType, ds),
				PType.Float    => CreateFloatPropStr(_floatVal, dispType),
				PType.Vec2     => CreateVec2PropStr(in _vec2Val, dispType),
				PType.Vec3     => CreateVec3PropStr(in _vec3Val, dispType),
				PType.Str      => CreateStrPropStr(_strVal, dispType),
				PType.IntArr   => _intArrVal.Select(i => CreateIntPropStr(i, dispType, ds)).SequenceToString(),
				PType.FloatArr => _floatArrVal.Select(f => CreateFloatPropStr(f, dispType)).SequenceToString(),
				PType.Vec2Arr  => _vec2ArrVal.Select(v2 => CreateVec2PropStr(v2, dispType)).SequenceToString(),
				PType.Vec3Arr  => _vec3ArrVal.Select(v3 => CreateVec3PropStr(v3, dispType)).SequenceToString(),
				PType.StrArr   => _strArrVal.Select(str => CreateStrPropStr(str, dispType)).SequenceToString(),
				_ => throw BadTypeE
			};
		}


		public enum PType : byte { // order matters
			Unparsed,
			Int, Float, Vec2, Vec3, Str,
			IntArr, FloatArr, Vec2Arr, Vec3Arr, StrArr
		}
	}
	
	/******************************************************************************/

	public static class EntPropFactory {
		
		// this right here is the real juice, it's how prop info is decoded
		public static List<(int propIndex, EntityProperty prop)> ReadEntProps(
			this BitStreamReader bsr,
			IReadOnlyList<FlattenedProp> fProps,
			SourceDemo demoRef)
		{
			var props = new List<(int propIndex, EntityProperty prop)>();
			int i = -1;
			if (demoRef.DemoSettings.NewDemoProtocol) {
				bool newWay = bsr.ReadBool();
				while ((i = bsr.ReadFieldIndex(i, newWay)) != -1)
					props.Add((i, CreateAndReadProp(fProps[i], bsr)));
			} else {
				while (bsr.ReadBool()) {
					i += (int)bsr.ReadUBitVar() + 1;
					props.Add((i, CreateAndReadProp(fProps[i], bsr)));
				}
			}
			return props;
		}
		
		
		// all of this fun jazz can be found in src_main/engine/dt_encode.cpp, a summary with comments is at the very end
		private static EntityProperty CreateAndReadProp(FlattenedProp fProp, BitStreamReader bsr) {
			
			const string exceptionMsg = "an impossible entity PropType has appeared while creating/reading props ";
			try {
				int offset = bsr.AbsoluteBitIndex;
				switch (fProp.PropInfo.SendPropType) {
					case SendPropType.Int:
						int i = bsr.DecodeInt(fProp.PropInfo);
						return new EntityProperty(fProp, offset, bsr.AbsoluteBitIndex - offset, i);
					case SendPropType.Float:
						float f = bsr.DecodeFloat(fProp.PropInfo);
						return new EntityProperty(fProp, offset, bsr.AbsoluteBitIndex - offset, f);
					case SendPropType.Vector3:
						bsr.DecodeVector3(fProp.PropInfo, out Vector3 v3);
						return new EntityProperty(fProp, offset, bsr.AbsoluteBitIndex - offset, ref v3);
					case SendPropType.Vector2:
						bsr.DecodeVector2(fProp.PropInfo, out Vector2 v2);
						return new EntityProperty(fProp, offset, bsr.AbsoluteBitIndex - offset, ref v2);
					case SendPropType.String:
						string s = bsr.DecodeString();
						return new EntityProperty(fProp, offset, bsr.AbsoluteBitIndex - offset, s);
					case SendPropType.Array:
						switch (fProp.ArrayElementPropInfo!.SendPropType) {
							case SendPropType.Int: {
								List<int> tmp = bsr.DecodeIntArr(fProp);
								return new EntityProperty(fProp, offset, bsr.AbsoluteBitIndex - offset, tmp);
							} case SendPropType.Float: {
								List<float> tmp = bsr.DecodeFloatArr(fProp);
								return new EntityProperty(fProp, offset, bsr.AbsoluteBitIndex - offset, tmp);
							} case SendPropType.Vector2: {
								List<Vector2> tmp = bsr.DecodeVector2Arr(fProp);
								return new EntityProperty(fProp, offset, bsr.AbsoluteBitIndex - offset, tmp);
							} case SendPropType.Vector3: {
								List<Vector3> tmp = bsr.DecodeVector3Arr(fProp);
								return new EntityProperty(fProp, offset, bsr.AbsoluteBitIndex - offset, tmp);
							} case SendPropType.String: {
								List<string> tmp = bsr.DecodeStringArr(fProp);
								return new EntityProperty(fProp, offset, bsr.AbsoluteBitIndex - offset, tmp);
							} default:
								throw new ArgumentException(exceptionMsg, nameof(fProp.PropInfo.SendPropType));
						}
				}
				throw new ArgumentException(exceptionMsg, nameof(fProp.PropInfo.SendPropType));
			} catch (ArgumentOutOfRangeException) { // catch errors during parsing
				return new EntityProperty(fProp);
			}
		}
	}
}
