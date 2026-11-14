// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.AI.Observables;

internal interface IObservableToolResultOutputs : INotifyPropertyChanged
{
    string CallId { get; }

    ObservableAIContentCollection? Outputs { get; }
}
