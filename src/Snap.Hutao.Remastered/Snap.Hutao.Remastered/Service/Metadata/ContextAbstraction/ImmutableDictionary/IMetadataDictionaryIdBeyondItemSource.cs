using Snap.Hutao.Remastered.Model.Metadata.Item;
using Snap.Hutao.Remastered.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction.ImmutableDictionary;

public interface IMetadataDictionaryIdBeyondItemSource
{
    ImmutableDictionary<BeyondItemId, BeyondItem> IdBeyondItemMap { get; set; }
}
