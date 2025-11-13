// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.ExceptionService;
using Snap.Hutao.Win32;
using System.IO;

namespace Snap.Hutao.Core.IO;

internal static class ValueFileExtension
{
    extension(ValueFile file)
    {
        public async ValueTask<ValueResult<bool, T?>> DeserializeFromJsonNoThrowAsync<T>(JsonSerializerOptions options)
            where T : class
        {
            try
            {
                using (FileStream stream = File.OpenRead(file))
                {
                    T? t = await JsonSerializer.DeserializeAsync<T>(stream, options).ConfigureAwait(false);
                    return new(true, t);
                }
            }
            catch (Exception ex)
            {
                HutaoNative.Instance.ShowErrorMessage(ex.Message, ExceptionFormat.Format(ex));
                return new(false, null);
            }
        }

        public async ValueTask<bool> SerializeToJsonNoThrowAsync<T>(T obj, JsonSerializerOptions options)
        {
            try
            {
                using (FileStream stream = File.Create(file))
                {
                    await JsonSerializer.SerializeAsync(stream, obj, options).ConfigureAwait(false);
                }

                return true;
            }
            catch (Exception ex)
            {
                HutaoNative.Instance.ShowErrorMessage(ex.Message, ExceptionFormat.Format(ex));
                return false;
            }
        }
    }
}