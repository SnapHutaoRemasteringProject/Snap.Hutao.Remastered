// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.EntityFrameworkCore;
using Snap.Hutao.Remastered.Core.Database;
using Snap.Hutao.Remastered.Core.Diagnostics;
using Snap.Hutao.Remastered.Core.Text.Json;
using Snap.Hutao.Remastered.Core.Database;
using Snap.Hutao.Remastered.Factory.Process;
using Snap.Hutao.Remastered.Model.Entity.Database;
using Snap.Hutao.Remastered.Win32;
using System.Data.Common;
using System.IO;

namespace Snap.Hutao.Remastered.Core.DependencyInjection;

public static partial class ServiceCollectionExtension
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static partial IServiceCollection AddServices(this IServiceCollection services);

    extension(IServiceCollection services)
    {
        public IServiceCollection AddJsonOptions()
        {
            return services.AddSingleton(JsonOptions.Default);
        }

        public IServiceCollection AddDatabase()
        {
            return services.AddDbContextPool<AppDbContext>(AddDbContext);

            static void AddDbContext(IServiceProvider serviceProvider, DbContextOptionsBuilder builder)
            {
                string dbFile = HutaoRuntime.GetDataDirectoryFile("Userdata.db");
                string sqlConnectionString = $"Data Source={dbFile}";

                try
                {
                    int applied = DatabaseSchema.Upgrade(sqlConnectionString);
                    if (applied > 0)
                    {
                        List<string> pendingMigrations = [.. context.Database.GetPendingMigrations()];
                        if (pendingMigrations.Count > 0)
                        {
                            serviceProvider.GetRequiredService<ILogger<AppDbContext>>().LogInformation("[Database] Applied {Count} schema migrations", applied);

                            using (SentrySpanScope migration = SentrySpanScope.StartTransaction("db.migration", "db.migration"))
                            {
                                migration
                                    .SetData("db.pending_migrations", pendingMigrations.Count)
                                    .SetData("db.file", Path.GetFileName(dbFile));

                                context.Database.Migrate();
                            }
                        }
                    }
                }
                catch (Exception ex) when (ex is DbException or InvalidDataException)
                {
                    string message = $"""
                        Snap Hutao 在执行数据库迁移时发生错误。
                        Snap Hutao encountered an error while performing database migration.

                        Database at '{dbFile}'

                        {ex.Message}
                        """;
                    HutaoNative.Instance.ShowErrorMessage("Warning | 警告", message);
                    ProcessFactory.KillCurrent();
                    return;
                }

                builder
#if DEBUG
                    .EnableSensitiveDataLogging()
#endif
                    .AddInterceptors(SentryDbCommandInterceptor.Instance)
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                    .UseLoggerFactory(serviceProvider.GetRequiredService<ILoggerFactory>())
                    .UseSqlite(sqlConnectionString);
            }
        }
    }
}
