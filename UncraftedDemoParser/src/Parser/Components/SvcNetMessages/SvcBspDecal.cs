using System.Numerics;
using System.Text;
using UncraftedDemoParser.Parser.Components.Abstract;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.Parser.Components.SvcNetMessages {
	
	public class SvcBspDecal : SvcNetMessage {

		public Vector3 Position;
		public int DecalTextureIndex;
		public int? EntityIndex;
		public int? ModelIndex;
		public bool LowPriority;
		
		
		public SvcBspDecal(byte[] data, SourceDemo demoRef, int tick) : base(data, demoRef, tick) {}
		
		
		protected override void ParseBytes(BitFieldReader bfr) {
			Position = bfr.ReadVector3();
			DecalTextureIndex = bfr.ReadBitsAsInt(9);
			if (bfr.ReadBool()) {
				EntityIndex = bfr.ReadBitsAsInt(11);
				ModelIndex = bfr.ReadBitsAsInt(11);
			}
			LowPriority = bfr.ReadBool();
		}


		protected override void PopulatedBuilder(StringBuilder builder) {
			builder.AppendLine($"\t\tposition: {Position.ToString("G5")}");
			builder.AppendLine($"\t\tdecal texture index: {DecalTextureIndex}");
			if (EntityIndex.HasValue) {
				builder.AppendLine($"\t\tentity index: {EntityIndex}");
				builder.AppendLine($"\t\tmodel index: {ModelIndex}");
			} else {
				builder.AppendLine("\t\tno entity or model index");
			}
			builder.Append($"\t\tlow priority: {LowPriority}");
		}
	}
}