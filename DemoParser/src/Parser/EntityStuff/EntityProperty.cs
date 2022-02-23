using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DemoParser.Parser.GameState;
using DemoParser.Utils;
using static DemoParser.Parser.EntityStuff.EntPropToStringHelper;

namespace DemoParser.Parser.EntityStuff {

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


		public override void PrettyWrite(IPrettyWriter pw) {
			int tmp = pw.LastLineLength;
			pw.Append(FProp.TypeString());
			pw.PadLastLine(tmp + 12, ' ');
			pw.Append($"{FProp.Name}: ");
			pw.Append(PropToString());
		}

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


		protected override string PropToString() {
			return this switch {
				SingleEntProp<int>     ip  => CreateIntPropStr(ip.Value, FProp.DisplayType, FProp.DemoInfo),
				SingleEntProp<float>   fp  => CreateFloatPropStr(fp.Value, FProp.DisplayType),
				SingleEntProp<Vector2> v2P => CreateVec2PropStr(v2P.Value, FProp.DisplayType),
				SingleEntProp<Vector3> v3P => CreateVec3PropStr(v3P.Value, FProp.DisplayType),
				SingleEntProp<string>  sp  => CreateStrPropStr(sp.Value, FProp.DisplayType),
				_ => throw new Exception($"bad property type: {GetType()}")
			};
		}
	}


	// when an array property is updated, it doesn't just replace the old array, it's sort of a delta
	public abstract class ArrEntProp : EntityProperty {
		protected ArrEntProp(FlattenedProp fProp, int offset, int bitLength) : base(fProp, offset, bitLength) {}
		public abstract ArrEntProp CopyArrayProp();
		public abstract void UpdateArrayProp(ArrEntProp other);
	}



	public class ArrEntProp<T> : ArrEntProp, IReadOnlyList<T> {

		public List<T> Values;


		public ArrEntProp(FlattenedProp fProp, List<T> values, int offset, int bitLength) : base(fProp, offset, bitLength) {
			Values = values;
		}


		public override ArrEntProp CopyArrayProp() {
			return new ArrEntProp<T>(FProp, Values.ToList(), Offset, BitLength);
		}


		public override void UpdateArrayProp(ArrEntProp other) {
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
				ArrEntProp<int> aip => aip.Values.Select(i => CreateIntPropStr(i, FProp.DisplayType, FProp.DemoInfo)).SequenceToString(),
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


		protected override string PropToString() {
			return "UNPARSED_DATA";
		}
	}
}
