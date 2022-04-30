using System.Numerics;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.GameState;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {

	public class SvcBspDecal : DemoMessage {

		public Vector3 Pos;
		public int DecalTextureIndex;
		public uint? EntityIndex;
		public uint? ModelIndex;
		public bool LowPriority;


		public SvcBspDecal(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {
			bsr.ReadVectorCoord(out Pos);
			DecalTextureIndex = (int)bsr.ReadUInt(9);
			if (bsr.ReadBool()) {
				EntityIndex = bsr.ReadUInt(DemoInfo.MaxEdictBits);
				ModelIndex = bsr.ReadUInt(DemoInfo.MaxEdictBits);
			}
			LowPriority = bsr.ReadBool();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"position: {Pos:F4}");

			pw.Append(GameState.StringTablesManager.IsTableStateValid(TableNames.DecalPreCache)
				? $"decal texture: {GameState.StringTablesManager.Tables[TableNames.DecalPreCache].Entries[DecalTextureIndex].Name}"
				: "decal texture index:");
			pw.AppendLine($" [{DecalTextureIndex}]");
			if (EntityIndex.HasValue) {
				pw.AppendLine($"entity index: {EntityIndex}");
				pw.AppendLine($"model index: {ModelIndex}");
			}
			pw.Append($"low priority: {LowPriority}");
		}
	}
}
