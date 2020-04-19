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


		protected EntityProperty(FlattenedProp propInfo) {
            PropInfo = propInfo;
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
		
		public IntEntProp(FlattenedProp propInfo, int value) : base(propInfo) {
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new IntEntProp(PropInfo, Value);
		}

		public override void CopyPropertyTo(EntityProperty other) {
			((IntEntProp)other).Value = Value;
		}
		
		public override string PropToString() {
			DisplayType tmp = EntPropToStringHelper.IdentifyTypeForInt(PropInfo.Name, PropInfo.Prop);
			return EntPropToStringHelper.CreateIntPropStr(Value, tmp);
		}
	}
	
	/******************************************************************************/
	
	public class FloatEntProp : EntityProperty {

		public float Value;
		
		public FloatEntProp(FlattenedProp propInfo, float value) : base(propInfo) {
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new FloatEntProp(PropInfo, Value);
		}

		public override void CopyPropertyTo(EntityProperty other) {
			((FloatEntProp)other).Value = Value;
		}

		public override string PropToString() {
			DisplayType tmp = EntPropToStringHelper.IdentifyTypeForFloat(PropInfo.Name);
			return EntPropToStringHelper.CreateFloatPropStr(Value, tmp);
		}
	}
	
	/******************************************************************************/

	public class Vec3EntProp : EntityProperty {

		public Vector3 Value;
		
		public Vec3EntProp(FlattenedProp propInfo, Vector3 value) : base(propInfo) {
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new Vec3EntProp(PropInfo, Value);
		}

		public override void CopyPropertyTo(EntityProperty other) {
			((Vec3EntProp)other).Value = Value;
		}
		
		public override string PropToString() {
			DisplayType tmp = EntPropToStringHelper.IdentifyTypeForVec3(PropInfo.Name);
			return EntPropToStringHelper.CreateVec3PropStr(in Value, tmp);
		}
	}
	
	/******************************************************************************/
	
	public class Vec2EntProp : EntityProperty {

		public Vector2 Value;
		
		public Vec2EntProp(FlattenedProp propInfo, Vector2 value) : base(propInfo) {
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new Vec2EntProp(PropInfo, Value);
		}

		public override void CopyPropertyTo(EntityProperty other) {
			((Vec2EntProp)other).Value = Value;
		}

		public override string PropToString() {
			DisplayType tmp = EntPropToStringHelper.IdentifyTypeForVec2(PropInfo.Name);
			return EntPropToStringHelper.CreateVec2PropStr(in Value, tmp);
		}
	}
	
	/******************************************************************************/
	
	public class StringEntProp : EntityProperty {

		public string Value;
		
		public StringEntProp(FlattenedProp propInfo, string value) : base(propInfo) {
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new StringEntProp(PropInfo, Value);
		}

		public override void CopyPropertyTo(EntityProperty other) {
			((StringEntProp)other).Value = Value;
		}

		public override string PropToString() {
			DisplayType tmp = EntPropToStringHelper.IdentifyTypeForString(PropInfo.Name);
			return EntPropToStringHelper.CreateStrPropStr(Value, tmp);
		}
	}
	
	/******************************************************************************/
	
	public class IntArrEntProp : EntityProperty {

		public List<int> Value;
		
		public IntArrEntProp(FlattenedProp propInfo, List<int> value) : base(propInfo) {
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new IntArrEntProp(PropInfo, new List<int>(Value));
		}
		
		public override void CopyPropertyTo(EntityProperty other) {
			IntArrEntProp casted = (IntArrEntProp)other;
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
		
		public FloatArrEntProp(FlattenedProp propInfo, List<float> value) : base(propInfo) {
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new FloatArrEntProp(PropInfo, new List<float>(Value));
		}

		public override void CopyPropertyTo(EntityProperty other) {
			FloatArrEntProp casted = (FloatArrEntProp)other;
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
		
		public Vec3ArrEntProp(FlattenedProp propInfo, List<Vector3> value) : base(propInfo) {
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new Vec3ArrEntProp(PropInfo, new List<Vector3>(Value));
		}

		public override void CopyPropertyTo(EntityProperty other) {
			Vec3ArrEntProp casted = (Vec3ArrEntProp)other;
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
		
		public Vec2ArrEntProp(FlattenedProp propInfo, List<Vector2> value) : base(propInfo) {
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new Vec2ArrEntProp(PropInfo, new List<Vector2>(Value));
		}

		public override void CopyPropertyTo(EntityProperty other) {
			Vec2ArrEntProp casted = (Vec2ArrEntProp)other;
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
		
		public StringArrEntProp(FlattenedProp propInfo, List<string> value) : base(propInfo) {
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new StringArrEntProp(PropInfo, new List<string>(Value));
		}
		
		public override void CopyPropertyTo(EntityProperty other) {
			StringArrEntProp casted = (StringArrEntProp)other;
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
		
		internal UnparsedProperty(FlattenedProp propInfo) : base(propInfo) {}
		
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
				switch (prop.Prop.SendPropType) {
					case SendPropType.Int:
						return new IntEntProp(prop, bsr.DecodeInt(prop.Prop));
					case SendPropType.Float:
						return new FloatEntProp(prop, bsr.DecodeFloat(prop.Prop));
					case SendPropType.Vector3:
						Vector3 v3 = default;
						bsr.DecodeVector3(prop.Prop, ref v3);
						return new Vec3EntProp(prop, v3);
					case SendPropType.Vector2:
						Vector2 v2 = default;
						bsr.DecodeVector2(prop.Prop, ref v2);
						return new Vec2EntProp(prop, v2);
					case SendPropType.String:
						return new StringEntProp(prop, bsr.DecodeString());
					case SendPropType.Array:
						return prop.ArrayElementProp.SendPropType switch {
							SendPropType.Int 		=> new IntArrEntProp(prop, bsr.DecodeIntArr(prop)),
							SendPropType.Float 		=> new FloatArrEntProp(prop, bsr.DecodeFloatArr(prop)),
							SendPropType.Vector3 	=> new Vec3ArrEntProp(prop, bsr.DecodeVector3Arr(prop)),
							SendPropType.Vector2 	=> new Vec2ArrEntProp(prop, bsr.DecodeVector2Arr(prop)),
							SendPropType.String 	=> new StringArrEntProp(prop, bsr.DecodeStringArr(prop)),
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