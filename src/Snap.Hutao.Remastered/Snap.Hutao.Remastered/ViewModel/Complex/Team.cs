// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.ObjectModel;
using System.ComponentModel;
using Snap.Hutao.Remastered.Model.Metadata.Avatar;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.Web.Hutao.SpiralAbyss;
using System.Collections.Immutable;
using System.Globalization;

namespace Snap.Hutao.Remastered.ViewModel.Complex;

public sealed partial class Team : ObservableCollection<AvatarView>
{
    public Team(ItemRate<string, int> team, ImmutableDictionary<AvatarId, Avatar> idAvatarMap, int rank)
        : base()
    {
        ReadOnlySpan<char> itemSpan = team.Item.AsSpan();
        foreach (Range range in itemSpan.Split(','))
        {
            uint id = uint.Parse(itemSpan[range], CultureInfo.InvariantCulture);
            Add(new(idAvatarMap[id], 0));
        }

        foreach (AvatarView avatar in new AvatarView[4 - Count]) Add(avatar);

        UpCount = SH.FormatModelBindingHutaoTeamUpCount(team.Rate);
        Rank = rank;
    }

    public string UpCount { get; }

    public int Rank
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Rank)));
        }
    }
}
