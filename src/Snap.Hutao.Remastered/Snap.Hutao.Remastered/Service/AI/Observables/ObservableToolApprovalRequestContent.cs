// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Snap.Hutao.Remastered.Service.AI.Observables;

internal sealed partial class ObservableToolApprovalRequestContent : ObservableInputRequestContent
{
    public ToolApprovalRequestContent? RawRepresentation { get; set; }

    public Guid TargetMessageId { get; set; }

    [ObservableProperty]
    public partial ObservableToolCallContent? ToolCall { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRespond))]
    public partial bool IsHandled { get; set; }

    [ObservableProperty]
    public partial bool? Approved { get; set; }

    [ObservableProperty]
    public partial string? Reason { get; set; }

    [JsonIgnore]
    public bool CanRespond { get => !IsHandled && RawRepresentation is not null; }

    public static ObservableToolApprovalRequestContent Create(ToolApprovalRequestContent toolApprovalRequestContent, JsonSerializerOptions jsonOptions)
    {
        return new()
        {
            RawRepresentation = toolApprovalRequestContent,
            RequestId = toolApprovalRequestContent.RequestId,
            ToolCall = ObservableToolCallContent.Create(toolApprovalRequestContent.ToolCall, jsonOptions),
        };
    }

    public void Update(ObservableToolApprovalRequestContent toolApprovalRequestContent)
    {
        ToolCall = toolApprovalRequestContent.ToolCall;
        TargetMessageId = toolApprovalRequestContent.TargetMessageId;
        RawRepresentation = toolApprovalRequestContent.RawRepresentation;
    }

    public void Update(ObservableToolApprovalResponseContent toolApprovalResponseContent)
    {
        Approved = toolApprovalResponseContent.Approved;
        Reason = toolApprovalResponseContent.Reason;
        IsHandled = true;
    }
}
