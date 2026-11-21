using Snap.Hutao.Remastered.Model.Metadata.Item;
using Snap.Hutao.Remastered.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction.ImmutableDictionary;

public interface IMetadataDictionaryIdBydMaterialSource
{
    ImmutableDictionary<BydMaterialId, BydMaterial> IdBydMaterialMap { get; set; }
}
