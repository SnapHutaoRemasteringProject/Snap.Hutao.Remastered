// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;

namespace Snap.Hutao.Remastered.Model.Entity.Database;

/// <summary>
/// 此类只用于在生成迁移时提供数据库上下文
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <inheritdoc/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public AppDbContext CreateDbContext(string[] args)
    {
        // Design-time model generation must not inspect a developer's live database.
        return new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);
    }
}
