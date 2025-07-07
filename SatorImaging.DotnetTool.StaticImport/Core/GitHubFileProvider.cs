// Licensed under the MIT License
// https://github.com/sator-imaging/DotnetTool-StaticImport

using System;
using System.Collections.Immutable;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SatorImaging.DotnetTool.StaticImport.Core;

internal class GitHubFileProvider  // TODO : IFileProvider
{
    internal static (string? userName, string? repoName, string? REF, string? filePath) ParseUrl(string input)
    {
        int pos_pathStart = input.IndexOf('/');
        if (pos_pathStart < 0 || !input.StartsWith(SR.GitHubSchemeFull, StringComparison.Ordinal))
        {
            throw new ArgumentException("invalid git url: " + input);
        }

        input = input[SR.GitHubSchemeFull.Length..];
        pos_pathStart -= SR.GitHubSchemeFull.Length;

        var userAndRepo = input[..pos_pathStart];

        int pos_atMark = userAndRepo.IndexOf('@');
        if (pos_atMark < 0)
        {
            throw new ArgumentException("user or repo name is invalid: " + userAndRepo);
        }

        var userName = userAndRepo[..pos_atMark];
        var repoName = userAndRepo[(pos_atMark + 1)..pos_pathStart];

        if (string.IsNullOrWhiteSpace(userName) ||
            string.IsNullOrWhiteSpace(repoName))
        {
            throw new ArgumentException("user or repo name is empty: " + userAndRepo);
        }

        var filePath = input[(pos_pathStart + 1)..];
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("file path is invalid: " + filePath);
        }

        int pos_REF = filePath.IndexOf('/');
        if (pos_REF < 0)
        {
            throw new ArgumentException("no branch, tag or commit hash: " + filePath);
        }

        var REF = filePath[..pos_REF];
        filePath = filePath[(pos_REF + 1)..];
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("file path is invalid: " + filePath);
        }

        Console.WriteDebugOnlyLine($"GitHub User: {userName}");
        Console.WriteDebugOnlyLine($"GitHub Repo: {repoName}");
        Console.WriteDebugOnlyLine($"Github Refs: {REF}");
        Console.WriteDebugOnlyLine($"Github Path: {filePath}");

        return (userName, repoName, REF, filePath);
    }


    static Uri BuildContentUrl(string userName, string repoName, string REF, string filePath)
    {
        var url = new Uri($"{SR.GitHubHostName}/{userName}/{repoName}/raw/{REF}/{filePath}");

        Console.WriteDebugOnlyLine($"GitHub Content URL: {url}");
        return url;
    }

    static Uri BuildApiUrl(string userName, string repoName, string REF, string filePath)
    {
        var url = new Uri($"{SR.GitHubApiHostName}/repos/{userName}/{repoName}/commits?sha={REF}&path=/{filePath}");

        Console.WriteDebugOnlyLine($"GitHub Commits API: {url}");
        return url;
    }


#pragma warning disable IDE0079
#pragma warning disable CA1822   // Instance/Default/Shared members should not be made static
#pragma warning restore IDE0079

    public static GitHubFileProvider Instance { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; set; } = new();

    protected GitHubFileProvider() { }


    public AuthenticationHeaderValue? GitHubToken { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; }

    public void Initialize()
    {
        foreach (var varName in ImmutableArray.Create(SR.GitHubTokenVarName1st, SR.GitHubTokenVarName2nd))
        {
            var token = Environment.GetEnvironmentVariable(varName, EnvironmentVariableTarget.Process)
                     ?? Environment.GetEnvironmentVariable(varName, EnvironmentVariableTarget.User)
                     ?? Environment.GetEnvironmentVariable(varName, EnvironmentVariableTarget.Machine)
                     ;

            if (!string.IsNullOrWhiteSpace(token))
            {
                GitHubToken = new("Bearer", token);

                Console.WriteImportantLine($"'{varName}' was loaded");
                break;
            }
        }
    }


    public async ValueTask<(byte[]? content, DateTimeOffset? lastModified)> TryGetAsync(string url, CancellationToken ct = default)
    {
        var (userName, repoName, REF, filePath) = ParseUrl(url);
        if (userName == null || repoName == null || REF == null || filePath == null)
        {
            return (null, null);
        }

        var modDateTask = DownloadAsync(HttpMethod.Head, BuildApiUrl(userName, repoName, REF, filePath).ToString(), ct);
        var contentTask = DownloadAsync(HttpMethod.Get, BuildContentUrl(userName, repoName, REF, filePath).ToString(), ct);

        await Task.WhenAll(modDateTask, contentTask);

        var (_, lastModified) = modDateTask.Result;
        var (content, _) = contentTask.Result;

        return (content, lastModified);
    }


    async Task<(byte[]? content, DateTimeOffset? lastModified)> DownloadAsync(
        HttpMethod method,
        string url,
        CancellationToken ct = default
        )
    {
        var client = HttpClient.Shared;

        using var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = GitHubToken;

        var GET = await client.SendAsync(req, ct);
        if (!GET.IsSuccessStatusCode)
        {
            return (null, null);
        }

        var lastModified = GET.Content.Headers.LastModified;

        var content = await GET.Content.ReadAsByteArrayAsync(ct);
        return (content, lastModified);
    }
}
