// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Google.Protobuf;
using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Metadata;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto;

/// <summary>
/// Maps one proto category into the application model. A category fails as a whole when an entry
/// throws; a value the model cannot represent (an overflowing id, a bad required URI) is skipped as a
/// single entry and reported once per field.
/// </summary>
internal abstract class ProtoCategoryAdapter<TProto, TModel> : IProtoCategoryAdapter
    where TProto : class, IMessage
{
    protected ProtoCategoryAdapter(MetadataCategory category)
    {
        Category = category;
    }

    public MetadataCategory Category { get; }

    public Type ElementType
    {
        get => typeof(TModel);
    }

    public abstract IEnumerable<TProto> Enumerate(LanguageMetadata language);

    public abstract TModel Map(TProto message, MetadataConvert convert);

    object IProtoCategoryAdapter.Read(LanguageMetadata language, MetadataConvert convert)
    {
        ImmutableArray<TModel>.Builder builder = ImmutableArray.CreateBuilder<TModel>();
        int index = 0;

        foreach (TProto message in Enumerate(language))
        {
            try
            {
                builder.Add(Map(message, convert));
            }
            catch (MetadataEntrySkippedException exception)
            {
                convert.WarnOnce(
                    $"skip.{Category.ProtoName}.{exception.Field}",
                    $"Skipped {Category.ProtoName} entry #{index}: {exception.Message}");
            }
            catch (Exception exception)
            {
                throw HutaoException.InvalidOperation($"Metadata category '{Category.ProtoName}' failed at entry #{index}", exception);
            }

            index++;
        }

        if (builder.Count is 0)
        {
            convert.WarnOnce($"empty.{Category.ProtoName}", $"Metadata category '{Category.ProtoName}' contains no entries");
        }

        return builder.ToImmutable();
    }
}
