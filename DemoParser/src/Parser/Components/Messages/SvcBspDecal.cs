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
				EntityIndex = bsr.ReadBitsAsUInt(DemoSettings.MaxEdictBits);
				ModelIndex = bsr.ReadBitsAsUInt(DemoSettings.MaxEdictBits);
			}
			LowPriority = bsr.ReadBool();
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"position: {Pos:F4}");

			var mgr = DemoRef.CurStringTablesManager;
			iw.Append(mgr.TableReadable.GetValueOrDefault(TableNames.DecalPreCache)
				? $"decal texture: {mgr.Tables[TableNames.DecalPreCache].Entries[DecalTextureIndex]}"
				: "decal texture index:");
			iw.AppendLine($" [{DecalTextureIndex}]");
			if (EntityIndex.HasValue) {
				iw.AppendLine($"entity index: {EntityIndex}");
				iw.AppendLine($"model index: {ModelIndex}");
			}
			iw.Append($"low priority: {LowPriority}");
		}
	}
}