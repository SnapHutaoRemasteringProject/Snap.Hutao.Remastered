// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.EntityFrameworkCore;
using Snap.Hutao.Model.Entity.Database;

namespace Snap.Hutao.Service.Abstraction;

internal static class ServiceScopeExtension
{
    extension(IServiceScope scope)
    {
        public TService GetRequiredService<TService>()
            where TService : class
        {
            return scope.ServiceProvider.GetRequiredService<TService>();
        }

        public TDbContext GetDbContext<TDbContext>()
            where TDbContext : DbContext
        {
            return scope.GetRequiredService<TDbContext>();
        }

        public AppDbContext GetAppDbContext()
        {
            return scope.GetDbContext<AppDbContext>();
        }
    }
}