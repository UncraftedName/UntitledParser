using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;
using static DemoParser.Parser.HelperClasses.EntityStuff.SendPropEnums;

namespace DemoParser.Parser.HelperClasses.EntityStuff {
	
	/* Every type of entity property gets a 'display type' which determines how the ToString() representation will be
	 * displayed. Actually determining this type is not the fastest thing in the world considering that every
	 * EntityUpdate ToString() needs to use it. So I have a lookup which says if the type has already been determined
	 * for this property based on the FlattenedProp it references. Those fProps should be unique and initialized only
	 * once for every type of prop that appears in the demo, so the lookup is a dict that uses reference comparisons
	 * like java's IdentityHashMap.
	 */
	public abstract class EntityProperty : Appendable {
		private class ReferenceComparer : IEqualityComparer<FlattenedProp> {
			public bool Equals(FlattenedProp a, FlattenedProp b) => ReferenceEquals(a, b);
			public int GetHashCode(FlattenedProp obj) => RuntimeHelpers.GetHashCode(obj);
		}

		private static readonly IDictionary<FlattenedProp, DisplayType> DisplayLookup =
			new Dictionary<FlattenedProp, DisplayType>(1000, new ReferenceComparer());


		public readonly FlattenedProp PropInfo;
		public DemoSettings DemoSettings => PropInfo.DemoSettings;

		public string Name => PropInfo.Name;
		// To be able to find the props later, technically might not
		// work with arrays but I probably won't worry about that for now.
		public int Offset;
		public int BitLength;

		private DisplayType _thisDisplayType;

		private protected DisplayType ThisDisplayType {
			get {
				if (_thisDisplayType == DisplayType.NOT_SET)
					if (!DisplayLookup.TryGetValue(PropInfo, out _thisDisplayType))
						ThisDisplayType = DetermineDisplayType();
				return _thisDisplayType;
			}
			private set {
				DisplayLookup[PropInfo] = value;
				_thisDisplayType = value;
			}
		}


		// internal cuz I want to keep the DisplayType enum internal
		private protected EntityProperty(FlattenedProp propInfo, int offset, int bitLength) {
			PropInfo = propInfo;
			Offset = offset;
			BitLength = bitLength;
			_thisDisplayType = DisplayType.NOT_SET;
		}


		public override void AppendToWriter(IndentedWriter iw) {
			int tmp = iw.LastLineLength;
			iw.Append(PropInfo.TypeString());
			iw.PadLastLine(tmp + 12, ' ');
			iw.Append($"{PropInfo.Name}: ");
			iw.Append(PropToString());
		}


		public abstract EntityProperty CopyProperty();

		public abstract void CopyPropertyTo(EntityProperty other);

		protected abstract string PropToString();

		private protected abstract DisplayType DetermineDisplayType();
	}
	
	/******************************************************************************/

	public class IntEntProp : EntityProperty {

		public int Value;
		
		public IntEntProp(FlattenedProp propInfo, int value, int offset, int bitLength) 
			: base(propInfo, offset, bitLength) 
		{
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new IntEntProp(PropInfo, Value, Offset, BitLength);
		}

		public override void CopyPropertyTo(EntityProperty other) {
			var casted = (IntEntProp)other;
			casted.Value = Value;
			casted.Offset = Offset;
			casted.BitLength = BitLength;
		}
		
		private protected override DisplayType DetermineDisplayType() {
			return EntPropToStringHelper.IdentifyTypeForInt(PropInfo.Name, PropInfo.Prop);
		}


		protected override string PropToString() {
			return EntPropToStringHelper.CreateIntPropStr(Value, ThisDisplayType, DemoSettings);
		}
	}
	
	/******************************************************************************/
	
	public class FloatEntProp : EntityProperty {

		public float Value;
		
		public FloatEntProp(FlattenedProp propInfo, float value, int offset, int bitLength) : base(propInfo, offset, bitLength) {
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new FloatEntProp(PropInfo, Value, Offset, BitLength);
		}

		public override void CopyPropertyTo(EntityProperty other) {
			var casted = (FloatEntProp)other;
			casted.Value = Value;
			casted.Offset = Offset;
			casted.BitLength = BitLength;
		}

		private protected override DisplayType DetermineDisplayType() {
			return EntPropToStringHelper.IdentifyTypeForFloat(PropInfo.Name);
		}


		protected override string PropToString() {
			return EntPropToStringHelper.CreateFloatPropStr(Value, ThisDisplayType);
		}
	}
	
	/******************************************************************************/

	public class Vec3EntProp : EntityProperty {

		public Vector3 Value;
		
		public Vec3EntProp(FlattenedProp propInfo, ref Vector3 value, int offset, int bitLength) : base(propInfo, offset, bitLength) {
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new Vec3EntProp(PropInfo, ref Value, Offset, BitLength);
		}

		public override void CopyPropertyTo(EntityProperty other) {
			var casted = (Vec3EntProp)other;
			casted.Value = Value;
			casted.Offset = Offset;
			casted.BitLength = BitLength;
		}

		private protected override DisplayType DetermineDisplayType() {
			return EntPropToStringHelper.IdentifyTypeForVec3(PropInfo.Name);
		}


		protected override string PropToString() {
			return EntPropToStringHelper.CreateVec3PropStr(in Value, ThisDisplayType);
		}
	}
	
	/******************************************************************************/
	
	public class Vec2EntProp : EntityProperty {

		public Vector2 Value;
		
		public Vec2EntProp(FlattenedProp propInfo, ref Vector2 value, int offset, int bitLength) : base(propInfo, offset, bitLength) {
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new Vec2EntProp(PropInfo, ref Value, Offset, BitLength);
		}

		public override void CopyPropertyTo(EntityProperty other) {
			var casted = (Vec2EntProp)other;
			casted.Value = Value;
			casted.Offset = Offset;
			casted.BitLength = BitLength;
		}
		
		private protected override DisplayType DetermineDisplayType() {
			return EntPropToStringHelper.IdentifyTypeForVec2(PropInfo.Name);
		}


		protected override string PropToString() {
			return EntPropToStringHelper.CreateVec2PropStr(in Value, ThisDisplayType);
		}
	}
	
	/******************************************************************************/
	
	public class StringEntProp : EntityProperty {

		public string Value;
		
		public StringEntProp(FlattenedProp propInfo, string value, int offset, int bitLength) : base(propInfo, offset, bitLength) {
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new StringEntProp(PropInfo, Value, Offset, BitLength);
		}

		public override void CopyPropertyTo(EntityProperty other) {
			var casted = (StringEntProp)other;
			casted.Value = Value;
			casted.Offset = Offset;
			casted.BitLength = BitLength;
		}
		
		private protected override DisplayType DetermineDisplayType() {
			return EntPropToStringHelper.IdentifyTypeForString(PropInfo.Name);
		}


		protected override string PropToString() {
			return EntPropToStringHelper.CreateStrPropStr(Value, ThisDisplayType);
		}
	}
	
	/******************************************************************************/
	
	public class IntArrEntProp : EntityProperty {

		public List<int> Value;
		
		public IntArrEntProp(FlattenedProp propInfo, List<int> value, int offset, int bitLength) : base(propInfo, offset, bitLength) {
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new IntArrEntProp(PropInfo, new List<int>(Value), Offset, BitLength);
		}
		
		public override void CopyPropertyTo(EntityProperty other) {
			IntArrEntProp casted = (IntArrEntProp)other;
			casted.Offset = Offset;
			casted.BitLength = BitLength;
			for (int i = 0; i < Value.Count; i++) {
				if (i >= casted.Value.Count)
					casted.Value.Add(Value[i]);
				else
					casted.Value[i] = Value[i];
			}
		}

		private protected override DisplayType DetermineDisplayType() {
			return EntPropToStringHelper.IdentifyTypeForInt(PropInfo.Name, PropInfo.ArrayElementProp);
		}


		protected override string PropToString() {
			return Value.Select(i =>
				EntPropToStringHelper.CreateIntPropStr(i, ThisDisplayType, DemoSettings)).SequenceToString();
		}
	}

	/******************************************************************************/
	
	public class FloatArrEntProp : EntityProperty {

		public List<float> Value;
		
		public FloatArrEntProp(FlattenedProp propInfo, List<float> value, int offset, int bitLength) : base(propInfo, offset, bitLength) {
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new FloatArrEntProp(PropInfo, new List<float>(Value), Offset, BitLength);
		}

		public override void CopyPropertyTo(EntityProperty other) {
			FloatArrEntProp casted = (FloatArrEntProp)other;
			casted.Offset = Offset;
			casted.BitLength = BitLength;
			for (int i = 0; i < Value.Count; i++) {
				if (i >= casted.Value.Count)
					casted.Value.Add(Value[i]);
				else
					casted.Value[i] = Value[i];
			}
		}

		private protected override DisplayType DetermineDisplayType() {
			return EntPropToStringHelper.IdentifyTypeForFloat(PropInfo.Name);
		}


		protected override string PropToString() {
			return Value.Select(i =>
				EntPropToStringHelper.CreateFloatPropStr(i, ThisDisplayType)).SequenceToString();
		}
	}
	
	/******************************************************************************/
	
	public class Vec3ArrEntProp : EntityProperty {

		public List<Vector3> Value;
		
		public Vec3ArrEntProp(FlattenedProp propInfo, List<Vector3> value, int offset, int bitLength) : base(propInfo, offset, bitLength) {
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new Vec3ArrEntProp(PropInfo, new List<Vector3>(Value), Offset, BitLength);
		}

		public override void CopyPropertyTo(EntityProperty other) {
			Vec3ArrEntProp casted = (Vec3ArrEntProp)other;
			casted.Offset = Offset;
			casted.BitLength = BitLength;
			for (int i = 0; i < Value.Count; i++) {
				if (i >= casted.Value.Count)
					casted.Value.Add(Value[i]);
				else
					casted.Value[i] = Value[i];
			}
		}
		
		private protected override DisplayType DetermineDisplayType() {
			return EntPropToStringHelper.IdentifyTypeForVec3(PropInfo.Name);
		}


		protected override string PropToString() {
			return Value.Select(i =>
				EntPropToStringHelper.CreateVec3PropStr(i, ThisDisplayType)).SequenceToString();
		}
	}
	
	/******************************************************************************/
	
	public class Vec2ArrEntProp : EntityProperty {

		public List<Vector2> Value;
		
		public Vec2ArrEntProp(FlattenedProp propInfo, List<Vector2> value, int offset, int bitLength) : base(propInfo, offset, bitLength) {
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new Vec2ArrEntProp(PropInfo, new List<Vector2>(Value), Offset, BitLength);
		}

		public override void CopyPropertyTo(EntityProperty other) {
			Vec2ArrEntProp casted = (Vec2ArrEntProp)other;
			casted.Offset = Offset;
			casted.BitLength = BitLength;
			for (int i = 0; i < Value.Count; i++) {
				if (i >= casted.Value.Count)
					casted.Value.Add(Value[i]);
				else
					casted.Value[i] = Value[i];
			}
		}

		private protected override DisplayType DetermineDisplayType() {
			return EntPropToStringHelper.IdentifyTypeForVec2(PropInfo.Name);
		}


		protected override string PropToString() {
			return Value.Select(i =>
				EntPropToStringHelper.CreateVec2PropStr(i, ThisDisplayType)).SequenceToString();
		}
	}
	
	/******************************************************************************/
	
	public class StringArrEntProp : EntityProperty {

		public List<string> Value;
		
		public StringArrEntProp(FlattenedProp propInfo, List<string> value, int offset, int bitLength) : base(propInfo, offset, bitLength) {
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new StringArrEntProp(PropInfo, new List<string>(Value), Offset, BitLength);
		}
		
		public override void CopyPropertyTo(EntityProperty other) {
			StringArrEntProp casted = (StringArrEntProp)other;
			casted.Offset = Offset;
			casted.BitLength = BitLength;
			for (int i = 0; i < Value.Count; i++) {
				if (i >= casted.Value.Count)
					casted.Value.Add(Value[i]);
				else
					casted.Value[i] = Value[i];
			}
		}
		
		private protected override DisplayType DetermineDisplayType() {
			return EntPropToStringHelper.IdentifyTypeForString(PropInfo.Name);
		}


		protected override string PropToString() {
			return Value.Select(i =>
				EntPropToStringHelper.CreateStrPropStr(i, ThisDisplayType)).SequenceToString();
		}
	}
	
	/******************************************************************************/

	// a 'failed to parse' type of thing that doesn't have a value
	// used to suppress some exceptions that might happen during prop parsing
	public class UnparsedProperty : EntityProperty { 
		
		internal UnparsedProperty(FlattenedProp propInfo) : base(propInfo, -1, -1) {}
		
		public override EntityProperty CopyProperty() {
			return new UnparsedProperty(PropInfo);
		}
		
		public override void CopyPropertyTo(EntityProperty other) {
			throw new InvalidOperationException("unparsed property attempted to be copied to another property");
		}

		private protected override DisplayType DetermineDisplayType() {
			return DisplayType.UNPARSED;
		}


		protected override string PropToString() {
			return "UNPARSED_DATA";
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
			var props = new List<(int propIndex, EntityProperty prop)>(fProps.Count);
			
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
		private static EntityProperty CreateAndReadProp(FlattenedProp prop, BitStreamReader bsr) {
			
			const string exceptionMsg = "an impossible entity type has appeared while creating/reading props ";
			try {
				int offset = bsr.AbsoluteBitIndex;
				switch (prop.Prop.SendPropType) {
					case SendPropType.Int:
						int i = bsr.DecodeInt(prop.Prop);
						return new IntEntProp(prop, i, offset, bsr.AbsoluteBitIndex - offset);
					case SendPropType.Float:
						float f = bsr.DecodeFloat(prop.Prop);
						return new FloatEntProp(prop, f, offset, bsr.AbsoluteBitIndex - offset);
					case SendPropType.Vector3:
						bsr.DecodeVector3(prop.Prop, out Vector3 v3);
						return new Vec3EntProp(prop, ref v3, offset, bsr.AbsoluteBitIndex - offset);
					case SendPropType.Vector2:
						bsr.DecodeVector2(prop.Prop, out Vector2 v2);
						return new Vec2EntProp(prop, ref v2, offset, bsr.AbsoluteBitIndex - offset);
					case SendPropType.String:
						string s = bsr.DecodeString();
						return new StringEntProp(prop, s, offset, bsr.AbsoluteBitIndex - offset);
					case SendPropType.Array:
						return prop.ArrayElementProp!.SendPropType switch {
							SendPropType.Int     => new IntArrEntProp(prop, bsr.DecodeIntArr(prop), offset, bsr.AbsoluteBitIndex - offset),
							SendPropType.Float   => new FloatArrEntProp(prop, bsr.DecodeFloatArr(prop), offset, bsr.AbsoluteBitIndex - offset),
							SendPropType.Vector3 => new Vec3ArrEntProp(prop, bsr.DecodeVector3Arr(prop), offset, bsr.AbsoluteBitIndex - offset),
							SendPropType.Vector2 => new Vec2ArrEntProp(prop, bsr.DecodeVector2Arr(prop), offset, bsr.AbsoluteBitIndex - offset),
							SendPropType.String  => new StringArrEntProp(prop, bsr.DecodeStringArr(prop), offset, bsr.AbsoluteBitIndex - offset),
							_ => throw new ArgumentException(exceptionMsg, nameof(prop.Prop.SendPropType))
						};
				}
				throw new ArgumentException(exceptionMsg, nameof(prop.Prop.SendPropType));
			} catch (ArgumentOutOfRangeException) { // catch errors during parsing
				return new UnparsedProperty(prop);
			}
		}
	}
}
