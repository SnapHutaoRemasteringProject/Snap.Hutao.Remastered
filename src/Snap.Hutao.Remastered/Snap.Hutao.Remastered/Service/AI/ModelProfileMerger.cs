using System.Collections.Generic;
using System.Linq;
using Snap.Hutao.Remastered.Core.AI;
using Snap.Hutao.Remastered.Service.AI.Settings;
using Snap.Hutao.Remastered.Service.AI.Models;

namespace Snap.Hutao.Remastered.Service.AI;

internal static class ModelProfileMerger
{
    private const string RemovedSuffix = " [Removed]";

    public static void Merge(ObservableSettingsCollection<ModelProfile, Guid> destination, IReadOnlyList<ModelProfile> source)
    {
        Dictionary<string, ModelProfile> existing = new(StringComparer.Ordinal);
        foreach (ModelProfile model in destination)
        {
            if (!string.IsNullOrWhiteSpace(model.ModelId)) { existing.TryAdd(model.ModelId.Trim(), model); }
        }
        bool selectFirst = destination.CurrentItem is null || string.IsNullOrWhiteSpace(destination.CurrentItem.ModelId);
        HashSet<string> seen = new(StringComparer.Ordinal);
        ModelProfile? first = null;
        int index = 0;
        foreach (ModelProfile fetched in source)
        {
            string id = fetched.ModelId?.Trim() ?? string.Empty;
            if (id.Length == 0 || !seen.Add(id)) { continue; }
            string name = string.IsNullOrWhiteSpace(fetched.Name) ? id : fetched.Name;
            if (!existing.TryGetValue(id, out ModelProfile? model))
            {
                // Populate before collection observers or XAML see the new item.
                model = new() { ModelId = id, Name = name };
                destination.Insert(index, model);
            }
            else
            {
                model.ModelId = id;
                if (string.IsNullOrWhiteSpace(model.Name) || model.Name.EndsWith(RemovedSuffix, StringComparison.Ordinal)) { model.Name = name; }
                int currentIndex = destination.IndexOf(model);
                if (currentIndex != index) { destination.Move(currentIndex, index); }
            }
            first ??= model;
            index++;
        }
        foreach (ModelProfile model in destination.Skip(index))
        {
            if (string.IsNullOrWhiteSpace(model.ModelId) || seen.Contains(model.ModelId.Trim())) { continue; }
            string name = model.DisplayName;
            if (!name.EndsWith(RemovedSuffix, StringComparison.Ordinal)) { model.Name = name + RemovedSuffix; }
        }
        if (selectFirst && first is not null) { destination.CurrentItem = first; }
    }
}
