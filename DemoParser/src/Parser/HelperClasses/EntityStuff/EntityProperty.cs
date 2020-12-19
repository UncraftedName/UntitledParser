using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;
using static DemoParser.Parser.HelperClasses.EntityStuff.SendPropEnums;

namespace DemoParser.Parser.HelperClasses.EntityStuff {
	
	public abstract class EntityProperty : AppendableClass {

		public readonly FlattenedProp PropInfo;
		public DemoSettings DemoSettings => PropInfo.DemoSettings;

		public string Name => PropInfo.Name;
		public int Offset;
		public int BitLength;


		protected EntityProperty(FlattenedProp propInfo, int offset, int bitLength) {
			PropInfo = propInfo;
			Offset = offset;
			BitLength = bitLength;
		}


		public override void AppendToWriter(IIndentedWriter iw) {
			int tmp = iw.LastLineLength;
			iw.Append(PropInfo.TypeString());
			iw.PadLastLine(tmp + 12, ' ');
			iw.Append($"{PropInfo.Name}: ");
			iw.Append(PropToString());
		}


		public abstract EntityProperty CopyProperty();

		public abstract void CopyPropertyTo(EntityProperty other);

		protected abstract string PropToString();
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


		protected override string PropToString() {
			return EntPropToStringHelper.CreateIntPropStr(Value, PropInfo.DisplayType, DemoSettings);
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


		protected override string PropToString() {
			return EntPropToStringHelper.CreateFloatPropStr(Value, PropInfo.DisplayType);
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


		protected override string PropToString() {
			return EntPropToStringHelper.CreateVec3PropStr(in Value, PropInfo.DisplayType);
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


		protected override string PropToString() {
			return EntPropToStringHelper.CreateVec2PropStr(in Value, PropInfo.DisplayType);
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


		protected override string PropToString() {
			return EntPropToStringHelper.CreateStrPropStr(Value, PropInfo.DisplayType);
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


		protected override string PropToString() {
			return Value.Select(i =>
				EntPropToStringHelper.CreateIntPropStr(i, PropInfo.DisplayType, DemoSettings)).SequenceToString();
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


		protected override string PropToString() {
			return Value.Select(i =>
				EntPropToStringHelper.CreateFloatPropStr(i, PropInfo.DisplayType)).SequenceToString();
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


		protected override string PropToString() {
			return Value.Select(i =>
				EntPropToStringHelper.CreateVec3PropStr(i, PropInfo.DisplayType)).SequenceToString();
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


		protected override string PropToString() {
			return Value.Select(i =>
				EntPropToStringHelper.CreateVec2PropStr(i, PropInfo.DisplayType)).SequenceToString();
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


		protected override string PropToString() {
			return Value.Select(i =>
				EntPropToStringHelper.CreateStrPropStr(i, PropInfo.DisplayType)).SequenceToString();
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
