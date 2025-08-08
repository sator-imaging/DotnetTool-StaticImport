// Licensed under the MIT License
// https://github.com/sator-imaging/DotnetTool-StaticImport

using System;
using System.Collections.Immutable;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
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
        string repoName = userAndRepo[(pos_atMark + 1)..].ToString();

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
        var url = new Uri($"{SR.GitHubApiHostName}/repos/{userName}/{repoName}/commits?sha={REF}&path={filePath}&per_page=1");
        Console.WriteDebugOnlyLine($"GitHub Commits API: {url}");
        return url;
    }

    public static GitHubFileProvider Instance { get; } = new();

    private GitHubFileProvider() { }

    public AuthenticationHeaderValue? GitHubToken { get; private set; }

    public void Initialize()
    {
        foreach (var varName in ImmutableArray.Create(SR.GitHubTokenVarName1st, SR.GitHubTokenVarName2nd))
        {
            var token = Environment.GetEnvironmentVariable(varName, EnvironmentVariableTarget.Process)
                     ?? Environment.GetEnvironmentVariable(varName, EnvironmentVariableTarget.User)
                     ?? Environment.GetEnvironmentVariable(varName, EnvironmentVariableTarget.Machine);

            if (!string.IsNullOrWhiteSpace(token))
            {
                GitHubToken = new("Bearer", token);
                Console.WriteImportantLine($"'{varName}' was loaded");
                break;
            }
        }
    }

    public async ValueTask<DateTimeOffset?> TryGetLastModifiedDateAsync(Uri uri, CancellationToken ct = default)
    {
        if (uri.Scheme != SR.GitHubScheme)
        {
            return null;
        }

        var (userName, repoName, REF, filePath) = ParseUrl(uri.ToString());
        var apiUrl = BuildApiUrl(userName, repoName, REF, filePath);

        using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
        if (GitHubToken != null)
        {
            request.Headers.Authorization = GitHubToken;
        }

        try
        {
            using var response = await HttpClient.Shared.SendAsync(request, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteWarning($"File not found on GitHub: {uri}");
                return null;
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var commits = doc.RootElement;

            if (commits.GetArrayLength() > 0)
            {
                var lastCommit = commits[0];
                if (lastCommit.TryGetProperty("commit", out var commit) &&
                    commit.TryGetProperty("committer", out var committer) &&
                    committer.TryGetProperty("date", out var dateElement) &&
                    dateElement.TryGetDateTimeOffset(out var date))
                {
                    return date;
                }
            }
            // if no commit history, fallback to Now.
            return DateTimeOffset.Now;
        }
        catch (Exception ex)
        {
            Console.WriteError($"Error getting last modified date from GitHub for {uri}: {ex.Message}");
            return null;
        }
    }

    public async ValueTask<byte[]?> TryGetContentAsync(Uri uri, CancellationToken ct = default)
    {
        if (uri.Scheme != SR.GitHubScheme)
        {
            return null;
        }

        var (userName, repoName, REF, filePath) = ParseUrl(uri.ToString());
        var contentUrl = BuildContentUrl(userName, repoName, REF, filePath);

        using var request = new HttpRequestMessage(HttpMethod.Get, contentUrl);
        if (GitHubToken != null)
        {
            request.Headers.Authorization = GitHubToken;
        }

        try
        {
            using var response = await HttpClient.Shared.SendAsync(request, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteWarning($"File not found on GitHub: {uri}");
                return null;
            }

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync(ct);
        }
        catch (Exception ex)
        {
            Console.WriteError($"Error getting content from GitHub for {uri}: {ex.Message}");
            return null;
        }
    }
}
