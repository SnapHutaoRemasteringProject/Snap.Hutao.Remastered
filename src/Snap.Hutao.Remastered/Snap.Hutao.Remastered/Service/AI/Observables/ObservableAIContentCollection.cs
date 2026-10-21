// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace Snap.Hutao.Remastered.Service.AI.Observables;

internal sealed class ObservableAIContentCollection : ObservableCollection<ObservableAIContent>
{
    public ObservableAIContentCollection()
        : base()
    {
    }

    public ObservableAIContentCollection(IEnumerable<ObservableAIContent> collection)
        : base(collection)
    {
    }

    public ObservableAIContentCollection(List<ObservableAIContent> list)
        : base(list)
    {
    }

    public static ObservableAIContentCollection? Create(IEnumerable<AIContent>? contents, JsonSerializerOptions jsonOptions)
    {
        if (contents is null)
        {
            return null;
        }

        ObservableAIContentCollection collection = [];
        foreach (AIContent content in contents)
        {
            // TODO: Should contiguous text/reasoning content be merged into one item?
            // Or is it even valid to have multiple contiguous text/reasoning content items?
            collection.AddOrUpdate(ObservableAIContent.Create(content, jsonOptions));
        }

        return collection.Count is 0 ? null : collection;
    }

    public static void AddOrUpdate(ObservableAIContentCollection collection, ObservableAIContent? newContent)
    {
        collection.AddOrUpdate(newContent);
    }

    public void AddOrUpdate(ObservableAIContent? newContent)
    {
        if (newContent is null)
        {
            return;
        }

        if (Count is 0)
        {
            Add(newContent);
            return;
        }

        ObservableAIContent lastContent = Items[^1];
        if (lastContent is ObservableTextReasoningContent lastReasoningContent && newContent is not ObservableTextReasoningContent)
        {
            lastReasoningContent.IsCompleted = true;
        }

        switch (lastContent)
        {
            case ObservableTextContent lastText when newContent is ObservableTextContent newText:
                lastText.Update(newText);
                return;
            case ObservableTextReasoningContent lastReasoning when newContent is ObservableTextReasoningContent newReasoning:
                lastReasoning.Update(newReasoning);
                return;
            case ObservableFunctionCallContent lastFunctionCall when newContent is ObservableFunctionCallContent newFunctionCall && lastFunctionCall.CallId == newFunctionCall.CallId:
                lastFunctionCall.Update(newFunctionCall);
                return;
            case ObservableFunctionResultContent lastFunctionResult when newContent is ObservableFunctionResultContent newFunctionResult && lastFunctionResult.CallId == newFunctionResult.CallId:
                lastFunctionResult.Update(newFunctionResult);
                return;
            case ObservableToolApprovalRequestContent lastApprovalRequest when newContent is ObservableToolApprovalRequestContent newApprovalRequest && lastApprovalRequest.RequestId == newApprovalRequest.RequestId:
                lastApprovalRequest.Update(newApprovalRequest);
                return;
            case ObservableToolApprovalRequestContent lastApprovalRequest when newContent is ObservableToolApprovalResponseContent newApprovalResponse && lastApprovalRequest.RequestId == newApprovalResponse.RequestId:
                lastApprovalRequest.Update(newApprovalResponse);
                return;
            case ObservableToolApprovalResponseContent lastApprovalResponse when newContent is ObservableToolApprovalResponseContent newApprovalResponse && lastApprovalResponse.RequestId == newApprovalResponse.RequestId:
                lastApprovalResponse.Update(newApprovalResponse);
                return;
            case ObservableUsageContent lastUsage when newContent is ObservableUsageContent newUsage:
                lastUsage.Update(newUsage);
                return;
            default:
                Add(newContent);
                return;
        }
    }
}
