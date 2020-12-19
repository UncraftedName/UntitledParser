using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;
using static DemoParser.Parser.HelperClasses.EntityStuff.SendPropEnums;

namespace DemoParser.Parser.HelperClasses.EntityStuff {
	
	public abstract class EntityProperty : AppendableClass {

		public readonly FlattenedProp FProp;
		public DemoSettings DemoSettings => FProp.DemoSettings;

		public string Name => FProp.Name;
		public int Offset;
		public int BitLength;


		protected EntityProperty(FlattenedProp fProp, int offset, int bitLength) {
			FProp = fProp;
			Offset = offset;
			BitLength = bitLength;
		}


		public override void AppendToWriter(IIndentedWriter iw) {
			int tmp = iw.LastLineLength;
			iw.Append(FProp.TypeString());
			iw.PadLastLine(tmp + 12, ' ');
			iw.Append($"{FProp.Name}: ");
			iw.Append(PropToString());
		}


		public abstract EntityProperty CopyProperty();

		public abstract void CopyPropertyTo(EntityProperty other);

		protected abstract string PropToString();
	}
	
	// todo think about making these generic
	/******************************************************************************/

	public class IntEntProp : EntityProperty {

		public int Value;
		
		public static implicit operator int(IntEntProp e) => e.Value;
		
		public IntEntProp(FlattenedProp fProp, int value, int offset, int bitLength) 
			: base(fProp, offset, bitLength) 
		{
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new IntEntProp(FProp, Value, Offset, BitLength);
		}

		public override void CopyPropertyTo(EntityProperty other) {
			var casted = (IntEntProp)other;
			casted.Value = Value;
			casted.Offset = Offset;
			casted.BitLength = BitLength;
		}


		protected override string PropToString() {
			return EntPropToStringHelper.CreateIntPropStr(Value, FProp.DisplayType, DemoSettings);
		}
	}
	
	/******************************************************************************/
	
	public class FloatEntProp : EntityProperty {

		public float Value;
		
		public static implicit operator float(FloatEntProp e) => e.Value;
		
		public FloatEntProp(FlattenedProp fProp, float value, int offset, int bitLength) : base(fProp, offset, bitLength) {
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new FloatEntProp(FProp, Value, Offset, BitLength);
		}

		public override void CopyPropertyTo(EntityProperty other) {
			var casted = (FloatEntProp)other;
			casted.Value = Value;
			casted.Offset = Offset;
			casted.BitLength = BitLength;
		}


		protected override string PropToString() {
			return EntPropToStringHelper.CreateFloatPropStr(Value, FProp.DisplayType);
		}
	}
	
	/******************************************************************************/

	public class Vec3EntProp : EntityProperty {

		public Vector3 Value;
		
		public static implicit operator Vector3(Vec3EntProp e) => e.Value;
		
		public Vec3EntProp(FlattenedProp fProp, ref Vector3 value, int offset, int bitLength) : base(fProp, offset, bitLength) {
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new Vec3EntProp(FProp, ref Value, Offset, BitLength);
		}

		public override void CopyPropertyTo(EntityProperty other) {
			var casted = (Vec3EntProp)other;
			casted.Value = Value;
			casted.Offset = Offset;
			casted.BitLength = BitLength;
		}


		protected override string PropToString() {
			return EntPropToStringHelper.CreateVec3PropStr(in Value, FProp.DisplayType);
		}
	}
	
	/******************************************************************************/
	
	public class Vec2EntProp : EntityProperty {

		public Vector2 Value;
		
		public static implicit operator Vector2(Vec2EntProp e) => e.Value;
		
		public Vec2EntProp(FlattenedProp fProp, ref Vector2 value, int offset, int bitLength) : base(fProp, offset, bitLength) {
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new Vec2EntProp(FProp, ref Value, Offset, BitLength);
		}

		public override void CopyPropertyTo(EntityProperty other) {
			var casted = (Vec2EntProp)other;
			casted.Value = Value;
			casted.Offset = Offset;
			casted.BitLength = BitLength;
		}


		protected override string PropToString() {
			return EntPropToStringHelper.CreateVec2PropStr(in Value, FProp.DisplayType);
		}
	}
	
	/******************************************************************************/
	
	public class StringEntProp : EntityProperty {

		public string Value;
		
		public static implicit operator string(StringEntProp e) => e.Value;
		
		public StringEntProp(FlattenedProp fProp, string value, int offset, int bitLength) : base(fProp, offset, bitLength) {
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new StringEntProp(FProp, Value, Offset, BitLength);
		}

		public override void CopyPropertyTo(EntityProperty other) {
			var casted = (StringEntProp)other;
			casted.Value = Value;
			casted.Offset = Offset;
			casted.BitLength = BitLength;
		}


		protected override string PropToString() {
			return EntPropToStringHelper.CreateStrPropStr(Value, FProp.DisplayType);
		}
	}
	
	/******************************************************************************/
	
	public class IntArrEntProp : EntityProperty {

		public List<int> Value;
		
		public IntArrEntProp(FlattenedProp fProp, List<int> value, int offset, int bitLength) : base(fProp, offset, bitLength) {
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new IntArrEntProp(FProp, new List<int>(Value), Offset, BitLength);
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


		protected override string PropToString() {
			return Value.Select(i =>
				EntPropToStringHelper.CreateIntPropStr(i, FProp.DisplayType, DemoSettings)).SequenceToString();
		}
	}

	/******************************************************************************/
	
	public class FloatArrEntProp : EntityProperty {

		public List<float> Value;
		
		public FloatArrEntProp(FlattenedProp fProp, List<float> value, int offset, int bitLength) : base(fProp, offset, bitLength) {
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new FloatArrEntProp(FProp, new List<float>(Value), Offset, BitLength);
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


		protected override string PropToString() {
			return Value.Select(i =>
				EntPropToStringHelper.CreateFloatPropStr(i, FProp.DisplayType)).SequenceToString();
		}
	}
	
	/******************************************************************************/
	
	public class Vec3ArrEntProp : EntityProperty {

		public List<Vector3> Value;
		
		public Vec3ArrEntProp(FlattenedProp fProp, List<Vector3> value, int offset, int bitLength) : base(fProp, offset, bitLength) {
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new Vec3ArrEntProp(FProp, new List<Vector3>(Value), Offset, BitLength);
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


		protected override string PropToString() {
			return Value.Select(i =>
				EntPropToStringHelper.CreateVec3PropStr(i, FProp.DisplayType)).SequenceToString();
		}
	}
	
	/******************************************************************************/
	
	public class Vec2ArrEntProp : EntityProperty {

		public List<Vector2> Value;
		
		public Vec2ArrEntProp(FlattenedProp fProp, List<Vector2> value, int offset, int bitLength) : base(fProp, offset, bitLength) {
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new Vec2ArrEntProp(FProp, new List<Vector2>(Value), Offset, BitLength);
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


		protected override string PropToString() {
			return Value.Select(i =>
				EntPropToStringHelper.CreateVec2PropStr(i, FProp.DisplayType)).SequenceToString();
		}
	}
	
	/******************************************************************************/
	
	public class StringArrEntProp : EntityProperty {

		public List<string> Value;
		
		public StringArrEntProp(FlattenedProp fProp, List<string> value, int offset, int bitLength) : base(fProp, offset, bitLength) {
			Value = value;
		}
		
		public override EntityProperty CopyProperty() {
			return new StringArrEntProp(FProp, new List<string>(Value), Offset, BitLength);
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


		protected override string PropToString() {
			return Value.Select(i =>
				EntPropToStringHelper.CreateStrPropStr(i, FProp.DisplayType)).SequenceToString();
		}
	}
	
	/******************************************************************************/

	// a 'failed to parse' type of thing that doesn't have a value
	// used to suppress some exceptions that might happen during prop parsing
	public class UnparsedProperty : EntityProperty { 
		
		internal UnparsedProperty(FlattenedProp fProp) : base(fProp, -1, -1) {}
		
		public override EntityProperty CopyProperty() {
			return new UnparsedProperty(FProp);
		}
		
		public override void CopyPropertyTo(EntityProperty other) {
			throw new InvalidOperationException("unparsed property attempted to be copied to another property");
		}


		protected override string PropToString() {
			return "UNPARSED_DATA";
		}
	}
	
	/******************************************************************************/

	public static class EntPropFactory {
		
		// this right here is the real juice, it's how prop info is decoded
		public static List<(int propIndex, EntityProperty prop)> ReadEntProps(
			this ref BitStreamReader bsr,
			IReadOnlyList<FlattenedProp> fProps,
			SourceDemo? demoRef)
		{
			var props = new List<(int propIndex, EntityProperty prop)>();
			
			int i = -1;
			if (demoRef.DemoSettings.NewDemoProtocol) {
				bool newWay = bsr.ReadBool();
				while ((i = bsr.ReadFieldIndex(i, newWay)) != -1)
					props.Add((i, bsr.CreateAndReadProp(fProps[i])));
			} else {
				while (bsr.ReadBool()) {
					i += (int)bsr.ReadUBitVar() + 1;
					props.Add((i, bsr.CreateAndReadProp(fProps[i])));
				}
			}
			return props;
		}
		
		
		// all of this fun jazz can be found in src_main/engine/dt_encode.cpp, a summary with comments is at the very end
		private static EntityProperty CreateAndReadProp(this ref BitStreamReader bsr, FlattenedProp fProp) {
			
			const string exceptionMsg = "an impossible entity type has appeared while creating/reading props ";
			try {
				int offset = bsr.AbsoluteBitIndex;
				switch (fProp.PropInfo.SendPropType) {
					case SendPropType.Int:
						int i = bsr.DecodeInt(fProp.PropInfo);
						return new IntEntProp(fProp, i, offset, bsr.AbsoluteBitIndex - offset);
					case SendPropType.Float:
						float f = bsr.DecodeFloat(fProp.PropInfo);
						return new FloatEntProp(fProp, f, offset, bsr.AbsoluteBitIndex - offset);
					case SendPropType.Vector3:
						bsr.DecodeVector3(fProp.PropInfo, out Vector3 v3);
						return new Vec3EntProp(fProp, ref v3, offset, bsr.AbsoluteBitIndex - offset);
					case SendPropType.Vector2:
						bsr.DecodeVector2(fProp.PropInfo, out Vector2 v2);
						return new Vec2EntProp(fProp, ref v2, offset, bsr.AbsoluteBitIndex - offset);
					case SendPropType.String:
						string s = bsr.DecodeString();
						return new StringEntProp(fProp, s, offset, bsr.AbsoluteBitIndex - offset);
					case SendPropType.Array:
						return fProp.ArrayElementPropInfo!.SendPropType switch {
							SendPropType.Int     => new IntArrEntProp(fProp, bsr.DecodeIntArr(fProp), offset, bsr.AbsoluteBitIndex - offset),
							SendPropType.Float   => new FloatArrEntProp(fProp, bsr.DecodeFloatArr(fProp), offset, bsr.AbsoluteBitIndex - offset),
							SendPropType.Vector3 => new Vec3ArrEntProp(fProp, bsr.DecodeVector3Arr(fProp), offset, bsr.AbsoluteBitIndex - offset),
							SendPropType.Vector2 => new Vec2ArrEntProp(fProp, bsr.DecodeVector2Arr(fProp), offset, bsr.AbsoluteBitIndex - offset),
							SendPropType.String  => new StringArrEntProp(fProp, bsr.DecodeStringArr(fProp), offset, bsr.AbsoluteBitIndex - offset),
							_ => throw new ArgumentException(exceptionMsg, nameof(fProp.PropInfo.SendPropType))
						};
				}
				throw new ArgumentException(exceptionMsg, nameof(fProp.PropInfo.SendPropType));
			} catch (ArgumentOutOfRangeException) { // catch errors during parsing
				return new UnparsedProperty(fProp);
			}
		}
	}
}
