// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.HardChallenge;

namespace Snap.Hutao.Remastered.Model.Entity.Configuration;

internal sealed class HardChallengeEntryConfiguration : IEntityTypeConfiguration<HardChallengeEntry>
{
    public void Configure(EntityTypeBuilder<HardChallengeEntry> builder)
    {
        builder.Property(e => e.HardChallengeData)
            .HasColumnType(SqliteTypeNames.Text)
            .HasConversion<JsonTextValueConverter<HardChallengeData>>();
    }
}