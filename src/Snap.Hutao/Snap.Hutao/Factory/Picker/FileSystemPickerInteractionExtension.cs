// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.IO;

namespace Snap.Hutao.Factory.Picker;

internal static class FileSystemPickerInteractionExtension
{
    extension(IFileSystemPickerInteraction interaction)
    {
        public ValueResult<bool, ValueFile> PickFile(FileSystemPickerOptions options)
        {
            return interaction.PickFile(options.Title, options.DefaultFileName, options.FilterName, options.FilterType);
        }

        public ValueResult<bool, ValueFile> SaveFile(FileSystemPickerOptions options)
        {
            return interaction.SaveFile(options.Title, options.DefaultFileName, options.FilterName, options.FilterType);
        }

        public ValueResult<bool, ValueFile> PickFile(string? title, string? filterName, string? filterType)
        {
            return interaction.PickFile(title, null, filterName, filterType);
        }

        public ValueResult<bool, string?> PickFolder()
        {
            return interaction.PickFolder(null);
        }
    }
}