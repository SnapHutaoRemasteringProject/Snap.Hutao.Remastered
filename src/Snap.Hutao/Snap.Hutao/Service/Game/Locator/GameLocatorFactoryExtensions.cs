// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Immutable;

namespace Snap.Hutao.Service.Game.Locator;

internal static class GameLocatorFactoryExtensions
{
    extension(IGameLocatorFactory factory)
    {
        public ValueTask<ValueResult<bool, string>> LocateSingleAsync(GameLocationSourceKind source)
        {
            return factory.Create(source).LocateSingleGamePathAsync();
        }

        public ValueTask<ImmutableArray<string>> LocateMultipleAsync(GameLocationSourceKind source)
        {
            IGameLocator locator = factory.Create(source);
            if (locator is IGameLocator2 locator2)
            {
                return locator2.LocateMultipleGamePathAsync();
            }

            return ValueTask.FromResult(ImmutableArray<string>.Empty);
        }
    }
}