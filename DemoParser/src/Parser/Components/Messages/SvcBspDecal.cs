using System.Numerics;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	public class SvcBspDecal : DemoMessage {
	
		public Vector3 Pos;
		public int DecalTextureIndex;
		public uint? EntityIndex;
		public uint? ModelIndex;
		public bool LowPriority;
		

		public SvcBspDecal(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			Pos = bsr.ReadVectorCoord();
			DecalTextureIndex = (int)bsr.ReadBitsAsUInt(9);
			if (bsr.ReadBool()) {
				EntityIndex = bsr.ReadBitsAsUInt(11);
				ModelIndex = bsr.ReadBitsAsUInt(11);
			}
			LowPriority = bsr.ReadBool();
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"position: {Pos:F4}");
			iw.Append(DemoRef.CStringTablesManager.Readable
				? $"decal texture: {DemoRef.CStringTablesManager.DecalTable.Entries[DecalTextureIndex]}"
				: "decal texture index:");
			iw.AppendLine($" ({DecalTextureIndex})");
			if (EntityIndex.HasValue) {
				iw.AppendLine($"entity index: {EntityIndex}");
				iw.AppendLine($"model index: {ModelIndex}");
			}
			iw.Append($"low priority: {LowPriority}");
		}
	}
}