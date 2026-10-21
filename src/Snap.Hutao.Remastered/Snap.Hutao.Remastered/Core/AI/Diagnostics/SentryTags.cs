// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

namespace Snap.Hutao.Remastered.Core.AI.Diagnostics;

internal static class SentryTags
{
    public const string AIModel = "ai.model";
    public const string AIProvider = "ai.provider";
    public const string DiagnosticsOperation = "diagnostics.operation";
    public const string ExceptionIsTerminating = "exception.is_terminating";
    public const string SettingsFileExists = "settings.file.exists";
    public const string SettingsGitAvailable = "settings.git.available";
    public const string SettingsGitCommand = "settings.git.command";
    public const string SettingsGitCommandSucceeded = "settings.git.command_succeeded";
    public const string SettingsGitFailureKind = "settings.git.failure_kind";
    public const string SettingsGitHasRemote = "settings.git.has_remote";
    public const string SettingsGitRepository = "settings.git.repository";
    public const string SettingsGitSucceeded = "settings.git.succeeded";
    public const string SettingsOptions = "settings.options";
    public const string UINavigationSucceeded = "ui.navigation.succeeded";
    public const string UIPage = "ui.page";
    public const string UIWindow = "ui.window";
    public const string UIWindowCloseCancelled = "ui.window.close_cancelled";
    public const string UrlScheme = "url.scheme";
}
