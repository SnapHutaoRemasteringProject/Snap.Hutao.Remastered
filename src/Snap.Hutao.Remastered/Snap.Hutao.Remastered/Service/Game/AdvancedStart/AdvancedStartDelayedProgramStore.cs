// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.

using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using Snap.Hutao.Remastered.Core.Database;
using Snap.Hutao.Remastered.Model.Entity.Database;
using Snap.Hutao.Remastered.Service.Game.AdvancedStart.Model;
using System.Collections.ObjectModel;

namespace Snap.Hutao.Remastered.Service.Game.AdvancedStart;

[Service(ServiceLifetime.Singleton)]
internal sealed class AdvancedStartDelayedProgramStore
{
    private readonly IServiceProvider serviceProvider;

    public AdvancedStartDelayedProgramStore(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public ObservableCollection<AdvancedStartDelayedProgramEntry> Load()
    {
        AdvancedStartDelayedProgramsConfiguration config = GetConfiguration();

        List<AdvancedStartDelayedProgramEntry> list = config.Entries
            .Select(static e => new AdvancedStartDelayedProgramEntry(e.Name, e.Path, unchecked((int)e.DelaySeconds)))
            .ToList();

        return new(list);
    }

    public void Save(ObservableCollection<AdvancedStartDelayedProgramEntry> entries)
    {
        AdvancedStartDelayedProgramsConfiguration config = new();
        config.Entries.AddRange(entries.Select(static e => new AdvancedStartDelayedProgramEntryDto
        {
            Name = e.Name ?? string.Empty,
            Path = e.Path ?? string.Empty,
            DelaySeconds = unchecked((uint)Math.Max(0, e.DelaySeconds)),
        }));

        SetConfiguration(config);
    }

    private AdvancedStartDelayedProgramsConfiguration GetConfiguration()
    {
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            string? base64 = db.Settings.SingleOrDefault(e => e.Key == Core.Setting.SettingKeys.LaunchAdvancedStartDelayedPrograms)?.Value;
            if (string.IsNullOrEmpty(base64))
            {
                return new();
            }

            try
            {
                byte[] bytes = Convert.FromBase64String(base64);
                AdvancedStartDelayedProgramsConfiguration config = new();
                config.MergeFrom(bytes);
                return config;
            }
            catch
            {
                return new();
            }
        }
    }

    private void SetConfiguration(AdvancedStartDelayedProgramsConfiguration config)
    {
        string base64 = Convert.ToBase64String(config.ToByteArray());

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Settings.Where(e => e.Key == Core.Setting.SettingKeys.LaunchAdvancedStartDelayedPrograms).ExecuteDelete();
            db.Settings.AddAndSave(new(Core.Setting.SettingKeys.LaunchAdvancedStartDelayedPrograms, base64));
        }
    }
}
