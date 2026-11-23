// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization.Metadata;

namespace Snap.Hutao.Remastered.Service.Update;

[Service(ServiceLifetime.Singleton)]
internal sealed class AlphaUpdateHistoryClient(IHttpClientFactory clients)
{
    public const string HttpClientName = nameof(AlphaUpdateHistoryClient);
    public const string RepositoryPath = "repos/SnapHutaoRemasteringProject/Snap.Hutao.Remastered";
    private const string RepositoryPage = "https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered";
    private const int MaxDiffBytes = 256 * 1024;

    public static string? GetBuildCommit()
    {
        // The .NET SDK appends SourceRevisionId to InformationalVersion, including for AOT.
        string? version = typeof(AlphaUpdateHistoryClient).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (version is null)
        {
            return null;
        }

        int separator = version.IndexOf('+');
        if (separator < 0)
        {
            return null;
        }

        string commit = version[(Math.Max(separator, version.LastIndexOf('.')) + 1)..];
        return IsCommitSha(commit) ? commit : null;
    }

    public static bool IsCommitSha([NotNullWhen(true)] string? sha) => sha is { Length: 40 } && sha.All(Uri.IsHexDigit);

    public static Uri ComparePage(string baseline, string current) => new($"{RepositoryPage}/compare/{baseline}...{current}");

    public static Uri CommitPage(string sha) => new($"{RepositoryPage}/commit/{sha}");

    public async Task<(string Sha, string Label)> GetBaselineAsync(string? previousCommit, CancellationToken token)
    {
        if (IsCommitSha(previousCommit))
        {
            return (previousCommit, previousCommit[..8]);
        }

        (AlphaGitHubRelease release, _) = await GetAsync("releases/latest", AlphaUpdateJsonContext.Default.AlphaGitHubRelease, token).ConfigureAwait(false);
        if (string.IsNullOrEmpty(release.TagName))
        {
            throw new JsonException("The latest GitHub release has no tag.");
        }

        (AlphaGitHubCommit commit, _) = await GetAsync($"commits/{Uri.EscapeDataString(release.TagName)}?per_page=1", AlphaUpdateJsonContext.Default.AlphaGitHubCommit, token).ConfigureAwait(false);
        return IsCommitSha(commit.Sha) ? (commit.Sha, release.TagName) : throw new JsonException("Invalid release commit SHA.");
    }

    public async Task<List<AlphaGitHubCommit>> GetUpdateCommitsAsync(string baseline, string current, CancellationToken token)
    {
        List<AlphaGitHubCommit> commits = [];
        if (string.Equals(baseline, current, StringComparison.OrdinalIgnoreCase))
        {
            return commits;
        }

        for (int page = 1; ; page++)
        {
            (AlphaGitHubComparison comparison, bool hasNext) = await GetAsync(
                $"compare/{baseline}...{current}?per_page=100&page={page}", AlphaUpdateJsonContext.Default.AlphaGitHubComparison, token).ConfigureAwait(false);
            commits.AddRange(comparison.Commits);
            if (!hasNext)
            {
                // GitHub Compare returns oldest first, even when paginated.
                commits.Reverse();
                return commits;
            }
        }
    }

    public Task<(AlphaGitHubCommit[] Value, bool HasNext)> GetOlderCommitsAsync(string baseline, int page, CancellationToken token)
    {
        return GetAsync($"commits?sha={baseline}&per_page=30&page={page}", AlphaUpdateJsonContext.Default.AlphaGitHubCommitArray, token);
    }

    public async Task<(string? Parent, List<AlphaGitHubFile> Files)> GetChangedFilesAsync(string commit, CancellationToken token)
    {
        List<AlphaGitHubFile> files = [];
        string? parent = null;
        for (int page = 1; ; page++)
        {
            (AlphaGitHubCommit result, bool hasNext) = await GetAsync(
                $"commits/{commit}?per_page=100&page={page}", AlphaUpdateJsonContext.Default.AlphaGitHubCommit, token).ConfigureAwait(false);
            parent ??= result.Parents.FirstOrDefault()?.Sha;
            files.AddRange(result.Files);
            if (!hasNext)
            {
                return (parent, files);
            }
        }
    }

    public async Task<(string OldText, string NewText)?> GetDiffAsync(string commit, string? parent, AlphaGitHubFile file, CancellationToken token)
    {
        if (file.Patch is null)
        {
            return null;
        }

        // Fetch the contents at the exact SHAs. A GitHub patch omits unchanged lines.
        Task<string?> oldFile = parent is null || file.Status == "added"
            ? Task.FromResult<string?>(string.Empty)
            : GetTextFileAsync(parent, file.PreviousFilename ?? file.Filename, token);
        Task<string?> newFile = file.Status == "removed"
            ? Task.FromResult<string?>(string.Empty)
            : GetTextFileAsync(commit, file.Filename, token);
        string?[] texts = await Task.WhenAll(oldFile, newFile).ConfigureAwait(false);
        return texts is [string oldText, string newText] ? (oldText, newText) : null;
    }

    private async Task<(T Value, bool HasNext)> GetAsync<T>(string path, JsonTypeInfo<T> typeInfo, CancellationToken token)
        where T : class
    {
        using HttpClient client = clients.CreateClient(HttpClientName);
        using HttpResponseMessage response = await client.GetAsync(path, token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        T value = await response.Content.ReadFromJsonAsync(typeInfo, token).ConfigureAwait(false) ?? throw new JsonException("Empty GitHub API response.");
        bool hasNext = response.Headers.TryGetValues("Link", out IEnumerable<string>? links)
            && links.Any(static link => link.Contains("rel=\"next\"", StringComparison.Ordinal));
        return (value, hasNext);
    }

    private async Task<string?> GetTextFileAsync(string sha, string filename, CancellationToken token)
    {
        string path = string.Join('/', filename.Split('/').Select(Uri.EscapeDataString));
        using HttpClient client = clients.CreateClient(HttpClientName);
        using HttpRequestMessage request = new(HttpMethod.Get, $"contents/{path}?ref={sha}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.raw+json"));
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(client.Timeout);
        using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > MaxDiffBytes)
        {
            return null;
        }

        using Stream stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
        byte[] buffer = new byte[MaxDiffBytes + 1];
        int length = await stream.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false, cancellationToken: timeout.Token).ConfigureAwait(false);
        if (length > MaxDiffBytes)
        {
            return null;
        }

        int offset = buffer.AsSpan(0, length).StartsWith(Encoding.UTF8.Preamble) ? Encoding.UTF8.Preamble.Length : 0;
        return Encoding.UTF8.GetString(buffer, offset, length - offset);
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, GenerationMode = JsonSourceGenerationMode.Metadata, RespectNullableAnnotations = true)]
[JsonSerializable(typeof(AlphaGitHubRelease))]
[JsonSerializable(typeof(AlphaGitHubComparison))]
[JsonSerializable(typeof(AlphaGitHubCommit))]
[JsonSerializable(typeof(AlphaGitHubCommit[]))]
internal sealed partial class AlphaUpdateJsonContext : JsonSerializerContext;

internal sealed class AlphaGitHubRelease
{
    public required string TagName { get; init; }
}

internal sealed class AlphaGitHubComparison
{
    public required AlphaGitHubCommit[] Commits { get; init; }
}

internal sealed class AlphaGitHubCommit
{
    public required string Sha { get; init; }

    public required AlphaGitHubCommitInfo Commit { get; init; }

    public AlphaGitHubCommitReference[] Parents { get; init; } = [];

    public AlphaGitHubFile[] Files { get; init; } = [];

    [JsonIgnore]
    public string Summary => Commit.Message.Split('\n', 2)[0].TrimEnd();

    [JsonIgnore]
    public string Byline => $"{Sha[..8]}  ·  {Commit.Author.Name}  ·  {Commit.Author.Date.ToLocalTime():g}";
}

internal sealed class AlphaGitHubCommitInfo
{
    public required string Message { get; init; }

    public required AlphaGitHubAuthor Author { get; init; }
}

internal sealed class AlphaGitHubAuthor
{
    public required string Name { get; init; }

    public DateTimeOffset Date { get; init; }
}

internal sealed class AlphaGitHubCommitReference
{
    public required string Sha { get; init; }
}

internal sealed class AlphaGitHubFile
{
    public required string Filename { get; init; }

    public required string Status { get; init; }

    public string? PreviousFilename { get; init; }

    public string? Patch { get; init; }
}
