using System;
using System.Numerics;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.HelperClasses;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	public class SvcBspDecal : DemoMessage {
	
		public Vector3 Pos;
		public int DecalTextureIndex;
		public uint? EntityIndex;
		public uint? ModelIndex;
		public bool LowPriority;
		

		public SvcBspDecal(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			bsr.ReadVectorCoord(out Pos);
			DecalTextureIndex = (int)bsr.ReadBitsAsUInt(9);
			if (bsr.ReadBool()) {
				EntityIndex = bsr.ReadBitsAsUInt(DemoInfo.MaxEdictBits);
				ModelIndex = bsr.ReadBitsAsUInt(DemoInfo.MaxEdictBits);
			}
			LowPriority = bsr.ReadBool();
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"position: {Pos:F4}");

			var mgr = DemoRef.CurStringTablesManager;
			pw.Append(mgr.TableReadable.GetValueOrDefault(TableNames.DecalPreCache)
				? $"decal texture: {mgr.Tables[TableNames.DecalPreCache].Entries[DecalTextureIndex]}"
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