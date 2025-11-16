// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.IO.Hashing;
using Snap.Hutao.Web.Hutao;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Windows.Storage;

namespace Snap.Hutao.Service.Git;

internal static class RepositoryAffinity
{
    private static readonly ApplicationDataContainer RepositoryContainer = ApplicationData.Current.LocalSettings.CreateContainer("RepositoryAffinity", ApplicationDataCreateDisposition.Always);
    private static readonly Lock SyncRoot = new();

    public static ImmutableArray<GitRepository> Sort(ImmutableArray<GitRepository> repositories)
    {
        lock (SyncRoot)
        {
            int[] counts = new int[repositories.Length];
            for (int i = 0; i < repositories.Length; i++)
            {
                GitRepository repository = repositories[i];
                ApplicationDataContainer container = RepositoryContainer.CreateContainer(repository.Name, ApplicationDataCreateDisposition.Always);
                string key = Hash.ToHexString(HashAlgorithmName.SHA256, repository.HttpsUrl.OriginalString.ToUpperInvariant());
                counts[i] = container.Values[key] is int c ? c : 0;
            }

            Array.Sort(counts, ImmutableCollectionsMarshal.AsArray(repositories));
            return repositories;
        }
    }

    public static void IncreaseFailure(GitRepository repository)
    {
        IncreaseFailure(repository.Name, repository.HttpsUrl.OriginalString);
    }

    public static void IncreaseFailure(string name, string url)
    {
        lock (SyncRoot)
        {
            ApplicationDataContainer container = RepositoryContainer.CreateContainer(name, ApplicationDataCreateDisposition.Always);
            string key = Hash.ToHexString(HashAlgorithmName.SHA256, url.ToUpperInvariant());
            object box = container.Values[key];
            container.Values[key] = box is int count ? unchecked(count + 1) : 1;
        }
    }

    public static void DecreaseFailure(GitRepository repository)
    {
        DecreaseFailure(repository.Name, repository.HttpsUrl.OriginalString);
    }

    public static void DecreaseFailure(string name, string url)
    {
        lock (SyncRoot)
        {
            ApplicationDataContainer container = RepositoryContainer.CreateContainer(name, ApplicationDataCreateDisposition.Always);
            string key = Hash.ToHexString(HashAlgorithmName.SHA256, url.ToUpperInvariant());
            object box = container.Values[key];
            container.Values[key] = box is int count ? unchecked(count - 1) : 0;
        }
    }
}