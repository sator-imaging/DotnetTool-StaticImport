// Licensed under the MIT License
// https://github.com/sator-imaging/DotnetTool-StaticImport

using System;
using System.Collections.Immutable;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SatorImaging.DotnetTool.StaticImport.Core;

internal class GitHubFileProvider : IFileProvider
{
    internal static (string userName, string repoName, string REF, string filePath) ParseUrl(ReadOnlySpan<char> input)
    {
        static ArgumentException error(string message, ReadOnlySpan<char> input) => new(message + input.ToString());

        if (!input.StartsWith(SR.GitHubSchemeFull, StringComparison.Ordinal))
        {
            throw error("invalid git url: ", input);
        }

        input = input[SR.GitHubSchemeFull.Length..];

        int pos_pathStart = input.IndexOf('/');
        if (pos_pathStart < 0)
        {
            throw error("file path is not found: ", input);
        }

        var userAndRepo = input[..pos_pathStart];

        int pos_atMark = userAndRepo.IndexOf('@');
        if (pos_atMark < 0)
        {
            throw error("user and repo name is not found: ", input);
        }

        string userName = userAndRepo[..pos_atMark].ToString();
        string repoName = userAndRepo[(pos_atMark + 1)..pos_pathStart].ToString();

        if (string.IsNullOrWhiteSpace(userName) ||
            string.IsNullOrWhiteSpace(repoName))
        {
            throw error("user or repo name is empty: ", input);
        }

        string filePath = input[(pos_pathStart + 1)..].ToString();

        int pos_REF = filePath.IndexOf('/');
        if (pos_REF < 0)
        {
            throw error("no branch, tag or commit hash: ", input);
        }

        string REF = filePath[..pos_REF];
        if (string.IsNullOrWhiteSpace(REF))
        {
            throw error("branch, tag or commit hash is empty: ", input);
        }

        filePath = filePath[(pos_REF + 1)..];
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw error("file path is empty: ", input);
        }

        Console.WriteDebugOnlyLine($"GitHub User: {userName}");
        Console.WriteDebugOnlyLine($"GitHub Repo: {repoName}");
        Console.WriteDebugOnlyLine($"Github Refs: {REF}");
        Console.WriteDebugOnlyLine($"Github Path: {filePath}");

        return (userName, repoName, REF, filePath);
    }


    static Uri BuildContentUrl(string userName, string repoName, string REF, string filePath)
    {
        var url = new Uri($"{SR.GitHubRawContentHostName}/{userName}/{repoName}/{REF}/{filePath}");

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

    private async Task<HttpResponseMessage?> SendRequestAsync(HttpMethod method, string url, CancellationToken ct)
    {
        try
        {
            var client = HttpClient.Shared;
            using var req = new HttpRequestMessage(method, url);
            req.Headers.Authorization = GitHubToken;
            return await client.SendAsync(req, ct);
        }
        catch (HttpRequestException e)
        {
            Console.WriteWarning(e.ToString());
            return null;
        }
    }

    public async ValueTask<DateTimeOffset?> TryGetLastModifiedDateAsync(string uri, CancellationToken ct = default)
    {
        var (userName, repoName, REF, filePath) = ParseUrl(uri);
        var apiUrl = BuildApiUrl(userName, repoName, REF, filePath).ToString();

        var res = await SendRequestAsync(HttpMethod.Head, apiUrl, ct);

        if (res == null || !res.IsSuccessStatusCode)
        {
            if (res != null) Console.WriteWarning($"{res}");
            return null;
        }

        return res.Content.Headers.LastModified ?? DateTimeOffset.Now;
    }

    public async ValueTask<byte[]?> TryGetContentAsync(string uri, CancellationToken ct = default)
    {
        var (userName, repoName, REF, filePath) = ParseUrl(uri);
        var contentUrl = BuildContentUrl(userName, repoName, REF, filePath).ToString();

        var res = await SendRequestAsync(HttpMethod.Get, contentUrl, ct);

        if (res == null || !res.IsSuccessStatusCode)
        {
            if (res != null) Console.WriteWarning($"{res}");
            return null;
        }

        return await res.Content.ReadAsByteArrayAsync(ct);
    }

        public string GetOutputFilePath(Uri uri, string outputDirOrFilePath, string? outputFilePrefix)
    {
            if (!Directory.Exists(outputDirOrFilePath))
        {
            return outputDirOrFilePath;
        }
        string fileName = Path.GetFileName(uri.AbsolutePath);
        return Path.Combine(outputDirOrFilePath, (outputFilePrefix + fileName));
    }
}
