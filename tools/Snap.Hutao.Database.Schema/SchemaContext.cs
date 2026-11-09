using Microsoft.EntityFrameworkCore;

namespace Snap.Hutao.Remastered.Model.Entity.Database;

// The historical migration attributes name AppDbContext. This design-only host uses
// their provider-level snapshots, so generating SQL needs neither UI nor live entities.
internal sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options);
