using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;
using static DemoParser.Parser.HelperClasses.EntityStuff.EntPropToStringHelper;
using static DemoParser.Parser.HelperClasses.EntityStuff.SendPropEnums;

namespace DemoParser.Parser.HelperClasses.EntityStuff {
	
	public abstract class EntityProperty : PrettyClass {
		
		public readonly FlattenedProp FProp;
		public string Name => FProp.Name;
		public int Offset;
		public int BitLength;
		
		
		protected EntityProperty(FlattenedProp fProp, int offset, int bitLength) {
			FProp = fProp;
			Offset = offset;
			BitLength = bitLength;
		}
		
		
		public override void PrettyWrite(IPrettyWriter iw) {
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
	


	public class SingleEntProp<T> : EntityProperty {
		
		public T Value;

		public static implicit operator T(SingleEntProp<T> prop) => prop.Value;
		
		
		public SingleEntProp(FlattenedProp fProp, in T value, int offset, int bitLength)
			: base(fProp, offset, bitLength)
		{
			Value = value;
		}
		
		
		public override EntityProperty CopyProperty() {
			return new SingleEntProp<T>(FProp, Value, Offset, BitLength);
		}
		
		
		public override void CopyPropertyTo(EntityProperty other) {
			var casted = (SingleEntProp<T>)other;
			casted.Value = Value;
			casted.Offset = Offset;
			casted.BitLength = BitLength;
		}
		
		
		protected override string PropToString() {
			return this switch {
				SingleEntProp<int>     ip  => CreateIntPropStr(ip.Value, FProp.DisplayType, FProp.DemoSettings),
				SingleEntProp<float>   fp  => CreateFloatPropStr(fp.Value, FProp.DisplayType),
				SingleEntProp<Vector2> v2P => CreateVec2PropStr(v2P.Value, FProp.DisplayType),
				SingleEntProp<Vector3> v3P => CreateVec3PropStr(v3P.Value, FProp.DisplayType),
				SingleEntProp<string>  sp  => CreateStrPropStr(sp.Value, FProp.DisplayType),
				_ => throw new Exception($"bad property type: {GetType()}")
			};
		}
	}
	


	public class ArrEntProp<T> : EntityProperty, IReadOnlyList<T> {
		
		public List<T> Values;
		
		
		public ArrEntProp(FlattenedProp fProp, List<T> values, int offset, int bitLength) : base(fProp, offset, bitLength) {
			Values = values;
		}
		
		
		public override EntityProperty CopyProperty() {
			return new ArrEntProp<T>(FProp, Values, Offset, BitLength);
		}
		
		
		public override void CopyPropertyTo(EntityProperty other) {
			ArrEntProp<T> casted = (ArrEntProp<T>)other;
			casted.Offset = Offset;
			casted.BitLength = BitLength;
			for (int i = 0; i < Values.Count; i++) {
				if (i >= casted.Values.Count)
					casted.Values.Add(Values[i]);
				else
					casted.Values[i] = Values[i];
			}
		}
		
		
		protected override string PropToString() {
			return this switch {
				ArrEntProp<int> aip => aip.Values.Select(i => CreateIntPropStr(i, FProp.DisplayType, FProp.DemoSettings)).SequenceToString(),
				ArrEntProp<float> afp => afp.Values.Select(f => CreateFloatPropStr(f, FProp.DisplayType)).SequenceToString(),
				ArrEntProp<Vector2> av2P => av2P.Values.Select(v => CreateVec2PropStr(v, FProp.DisplayType)).SequenceToString(),
				ArrEntProp<Vector3> av3P => av3P.Values.Select(v => CreateVec3PropStr(v, FProp.DisplayType)).SequenceToString(),
				ArrEntProp<string> asp => asp.Values.Select(s => CreateStrPropStr(s, FProp.DisplayType)).SequenceToString(),
				_ => throw new Exception($"bad property type: {GetType()}")
			};
		}
		
		
		public IEnumerator<T> GetEnumerator() {
			return Values.GetEnumerator();
		}
		
		
		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
		
		
		public int Count => Values.Count;
		
		
		public T this[int index] => Values[index];
	}
	
	

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
						return new SingleEntProp<int>(fProp, i, offset, bsr.AbsoluteBitIndex - offset);
					case SendPropType.Float:
						float f = bsr.DecodeFloat(fProp.PropInfo);
						return new SingleEntProp<float>(fProp, f, offset, bsr.AbsoluteBitIndex - offset);
					case SendPropType.Vector3:
						bsr.DecodeVector3(fProp.PropInfo, out Vector3 v3);
						return new SingleEntProp<Vector3>(fProp, v3, offset, bsr.AbsoluteBitIndex - offset);
					case SendPropType.Vector2:
						bsr.DecodeVector2(fProp.PropInfo, out Vector2 v2);
						return new SingleEntProp<Vector2>(fProp, v2, offset, bsr.AbsoluteBitIndex - offset);
					case SendPropType.String:
						string s = bsr.DecodeString();
						return new SingleEntProp<string>(fProp, s, offset, bsr.AbsoluteBitIndex - offset);
					case SendPropType.Array:
						return fProp.ArrayElementPropInfo!.SendPropType switch {
							SendPropType.Int => new ArrEntProp<int>(fProp, bsr.DecodeIntArr(fProp), offset, bsr.AbsoluteBitIndex - offset),
							SendPropType.Float => new ArrEntProp<float>(fProp, bsr.DecodeFloatArr(fProp), offset, bsr.AbsoluteBitIndex - offset),
							SendPropType.Vector3 => new ArrEntProp<Vector3>(fProp, bsr.DecodeVector3Arr(fProp), offset, bsr.AbsoluteBitIndex - offset),
							SendPropType.Vector2 => new ArrEntProp<Vector2>(fProp, bsr.DecodeVector2Arr(fProp), offset, bsr.AbsoluteBitIndex - offset),
							SendPropType.String => new ArrEntProp<string>(fProp, bsr.DecodeStringArr(fProp), offset, bsr.AbsoluteBitIndex - offset),
							_ => throw new ArgumentException(exceptionMsg, nameof(fProp.PropInfo.SendPropType))
						};
				} throw new ArgumentException(exceptionMsg, nameof(fProp.PropInfo.SendPropType));
			} catch (ArgumentOutOfRangeException) { // catch errors during parsing
				return new UnparsedProperty(fProp);
			}
		}
	}
}
