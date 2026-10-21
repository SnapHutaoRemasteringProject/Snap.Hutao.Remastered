// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;

namespace Snap.Hutao.Remastered.Service.AI.Observables;

internal sealed partial class ObservableToolApprovalResponseContent : ObservableInputResponseContent
{
    [ObservableProperty]
    public partial bool Approved { get; set; }

    [ObservableProperty]
    public partial string? Reason { get; set; }

    public static ObservableToolApprovalResponseContent Create(ToolApprovalResponseContent toolApprovalResponseContent)
    {
        return new()
        {
            RequestId = toolApprovalResponseContent.RequestId,
            Approved = toolApprovalResponseContent.Approved,
            Reason = toolApprovalResponseContent.Reason,
        };
    }

    public void Update(ObservableToolApprovalResponseContent toolApprovalResponseContent)
    {
        Approved = toolApprovalResponseContent.Approved;
        Reason = toolApprovalResponseContent.Reason;
    }
}
