// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Snap.Hutao.Remastered.Service.AI.Observables;

internal sealed class ObservableChatMessageCollection : ObservableCollection<ObservableChatMessage>
{
    public ObservableChatMessageCollection()
        : base()
    {
    }

    public ObservableChatMessageCollection(IEnumerable<ObservableChatMessage> collection)
        : base(collection)
    {
    }

    public ObservableChatMessageCollection(List<ObservableChatMessage> list)
        : base(list)
    {
    }

    public static void Add(ObservableChatMessageCollection collection, ObservableChatMessage message)
    {
        collection.Add(message);
    }
}
