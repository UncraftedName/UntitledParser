using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DemoParser.Parser.Components.Packets;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.HelperClasses.EntityStuff {
	
    public abstract class EntityProperty : Appendable {
		
        public readonly FlattenedProp PropInfo;
		public string Name => PropInfo.Name;
		// To be able to find the props later, technically might not
		// work with arrays but I probably won't worry about that for now.
		public int Offset;
		public int BitLength;


		protected EntityProperty(FlattenedProp propInfo, int offset, int bitLength) {
			PropInfo = propInfo;
			Offset = offset;
			BitLength = bitLength;
		}


		public override void AppendToWriter(IndentedWriter iw) {
			int tmp = iw.LastLineLength;
			iw.Append(PropInfo.TypeString());
			iw.PadLastLine(tmp + 12, ' ');
			iw.Append(PropInfo.Name + ": ");
			iw.Append(PropToString());
		}
		
		public abstract EntityProperty CopyProperty();

		public abstract void CopyPropertyTo(EntityProperty other);

		/* The structure for all of these is that first the "type" is identified based on the name
		 * (this is pretty much a guess, but in theory most props should be named correctly by volvo).
		 * Then we call a helper to string function based on that type which will create a special string for that
		 * specific prop. Those two steps are done separately for the case of arrays so that the type doesn't have to
		 * be determined several times. For cases like floats and strings, those helpers just return the default
		 * ToString() calls (but the methods are still there in case I want to change them in the future).
		 * For arrays, I pass the element props to the helper after identifying the type.
		 */
		public abstract string PropToString();
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
		
		public override string PropToString() {
			DisplayType tmp = EntPropToStringHelper.IdentifyTypeForInt(PropInfo.Name, PropInfo.Prop);
			return EntPropToStringHelper.CreateIntPropStr(Value, tmp);
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

		public override string PropToString() {
			DisplayType tmp = EntPropToStringHelper.IdentifyTypeForFloat(PropInfo.Name);
			return EntPropToStringHelper.CreateFloatPropStr(Value, tmp);
		}
	}
	
	/******************************************************************************/

	public class Vec3EntProp : EntityProperty {

		public Vector3 Value;
		
		public Vec3EntProp(FlattenedProp propInfo, Vector3 value, int offset, int bitLength) : base(propInfo, offset, bitLength) {
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new Vec3EntProp(PropInfo, Value, Offset, BitLength);
		}

		public override void CopyPropertyTo(EntityProperty other) {
			var casted = (Vec3EntProp)other;
			casted.Value = Value;
			casted.Offset = Offset;
			casted.BitLength = BitLength;
		}
		
		public override string PropToString() {
			DisplayType tmp = EntPropToStringHelper.IdentifyTypeForVec3(PropInfo.Name);
			return EntPropToStringHelper.CreateVec3PropStr(in Value, tmp);
		}
	}
	
	/******************************************************************************/
	
	public class Vec2EntProp : EntityProperty {

		public Vector2 Value;
		
		public Vec2EntProp(FlattenedProp propInfo, Vector2 value, int offset, int bitLength) : base(propInfo, offset, bitLength) {
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new Vec2EntProp(PropInfo, Value, Offset, BitLength);
		}

		public override void CopyPropertyTo(EntityProperty other) {
			var casted = (Vec2EntProp)other;
			casted.Value = Value;
			casted.Offset = Offset;
			casted.BitLength = BitLength;
		}

		public override string PropToString() {
			DisplayType tmp = EntPropToStringHelper.IdentifyTypeForVec2(PropInfo.Name);
			return EntPropToStringHelper.CreateVec2PropStr(in Value, tmp);
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

		public override string PropToString() {
			DisplayType tmp = EntPropToStringHelper.IdentifyTypeForString(PropInfo.Name);
			return EntPropToStringHelper.CreateStrPropStr(Value, tmp);
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

		public override string PropToString() {
			DisplayType tmp = EntPropToStringHelper.IdentifyTypeForInt(PropInfo.Name, PropInfo.ArrayElementProp);
			return Value.Select(i => EntPropToStringHelper.CreateIntPropStr(i, tmp)).SequenceToString();
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
		
		public override string PropToString() {
			DisplayType tmp = EntPropToStringHelper.IdentifyTypeForFloat(PropInfo.Name);
			return Value.Select(i => EntPropToStringHelper.CreateFloatPropStr(i, tmp)).SequenceToString();
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

		public override string PropToString() {
			DisplayType tmp = EntPropToStringHelper.IdentifyTypeForVec3(PropInfo.Name);
			return Value.Select(i => EntPropToStringHelper.CreateVec3PropStr(i, tmp)).SequenceToString();
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

		public override string PropToString() {
			DisplayType tmp = EntPropToStringHelper.IdentifyTypeForVec2(PropInfo.Name);
			return Value.Select(i => EntPropToStringHelper.CreateVec2PropStr(i, tmp)).SequenceToString();
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

		public override string PropToString() {
			DisplayType tmp = EntPropToStringHelper.IdentifyTypeForString(PropInfo.Name);
			return Value.Select(i => EntPropToStringHelper.CreateStrPropStr(i, tmp)).SequenceToString();
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

		public override string PropToString() {
			return "INVALID_DATA";
		}
	}
	
	/******************************************************************************/

	public static class EntPropFactory {
		
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
						Vector3 v3 = default;
						bsr.DecodeVector3(prop.Prop, ref v3);
						return new Vec3EntProp(prop, v3, offset, bsr.AbsoluteBitIndex - offset);
					case SendPropType.Vector2:
						Vector2 v2 = default;
						bsr.DecodeVector2(prop.Prop, ref v2);
						return new Vec2EntProp(prop, v2, offset, bsr.AbsoluteBitIndex - offset);
					case SendPropType.String:
						string s = bsr.DecodeString();
						return new StringEntProp(prop, s, offset, bsr.AbsoluteBitIndex - offset);
					case SendPropType.Array:
						return prop.ArrayElementProp.SendPropType switch {
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


		// this right here is the real juice, it's how prop info is decoded
		public static List<(int propIndex, EntityProperty prop)> ReadEntProps(
			this BitStreamReader bsr,
			IReadOnlyList<FlattenedProp> fProps, 
			SourceDemo demoRef) 
		{
			var props = new List<(int propIndex, EntityProperty prop)>();
			
			int i = -1;
			if (demoRef.DemoSettings.NewEngine) {
				bool newWay = bsr.ReadBool();
				while ((i = bsr.ReadFieldIndex(i, newWay)) != -1) {
					props.Add((i, CreateAndReadProp(fProps[i], bsr)));
					// temporary fix
					if ((fProps[i].Prop.Flags & SendPropFlags.CoordMpIntegral) != 0)
						bsr.SkipBits(24);
				}
			} else {
				while (bsr.ReadBool()) {
					i += (int)bsr.ReadUBitVar() + 1;
					props.Add((i, CreateAndReadProp(fProps[i], bsr)));
				}
			}
			return props;
		} 
	}
}