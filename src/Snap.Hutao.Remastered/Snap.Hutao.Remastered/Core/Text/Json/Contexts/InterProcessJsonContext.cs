// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.LifeCycle;
using Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.BetterGenshinImpact;
using Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.BetterGenshinImpact.Cultivation;
using Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.BetterGenshinImpact.Task;
using Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.Model;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Core.Text.Json;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(HutaoActivationArguments))]
[JsonSerializable(typeof(ToastNotificationRequest))]
[JsonSerializable(typeof(ElevationStatusResponse))]
[JsonSerializable(typeof(PipeRequest<Void>))]
[JsonSerializable(typeof(PipeRequest<long>))]
[JsonSerializable(typeof(PipeRequest<string>))]
[JsonSerializable(typeof(PipeRequest<JsonElement>))]
[JsonSerializable(typeof(PipeRequest<AutomationTaskDefinition>))]
[JsonSerializable(typeof(PipeRequest<SteppedAutomationTaskDefinition>))]
[JsonSerializable(typeof(PipeRequest<AddAutomationTaskStepDefinition>))]
[JsonSerializable(typeof(PipeRequest<UpdateAutomationTaskStepDefinition>))]
[JsonSerializable(typeof(PipeRequest<UpdateAutomationTaskStepIndex>))]
[JsonSerializable(typeof(PipeResponse<Void>))]
[JsonSerializable(typeof(PipeResponse<JsonElement>))]
[JsonSerializable(typeof(PipeResponse<AutomationCultivationProject>))]
[JsonSerializable(typeof(AutomationCultivationProject))]
[JsonSerializable(typeof(AutomationTaskDefinition))]
[JsonSerializable(typeof(SteppedAutomationTaskDefinition))]
[JsonSerializable(typeof(AddAutomationTaskStepDefinition))]
[JsonSerializable(typeof(UpdateAutomationTaskStepDefinition))]
[JsonSerializable(typeof(UpdateAutomationTaskStepIndex))]
[JsonSerializable(typeof(ImmutableArray<AutomationTaskDefinition>))]
[JsonSerializable(typeof(Void))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(JsonElement))]
internal sealed partial class InterProcessJsonContext : JsonSerializerContext;
