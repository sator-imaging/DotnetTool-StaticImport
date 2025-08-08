// Licensed under the MIT License
// https://github.com/sator-imaging/DotnetTool-StaticImport

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SatorImaging.DotnetTool.StaticImport.Core;

internal class HttpFileProvider : IFileProvider
{
    public static HttpFileProvider Instance { get; } = new();

    private HttpFileProvider() { }

    public async ValueTask<DateTimeOffset?> TryGetLastModifiedDateAsync(Uri uri, CancellationToken ct = default)
    {
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, uri);
            using var response = await HttpClient.Shared.SendAsync(request, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteWarning($"File not found: {uri}");
                return null;
            }

            // some servers doesn't support HEAD request.
            if (response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
            {
                Console.WriteWarning($"HEAD method not allowed for: {uri}");
                // fallback to GET request.
                return await TryGetLastModifiedDateByGetRequestAsync(uri, ct);
            }

            response.EnsureSuccessStatusCode();

            return response.Content.Headers.LastModified ?? DateTimeOffset.Now;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteError($"Error getting last modified date for {uri}: {ex.Message}");
            return null;
        }
    }

    private async ValueTask<DateTimeOffset?> TryGetLastModifiedDateByGetRequestAsync(Uri uri, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using var response = await HttpClient.Shared.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteWarning($"File not found: {uri}");
                return null;
            }

            response.EnsureSuccessStatusCode();

            return response.Content.Headers.LastModified ?? DateTimeOffset.Now;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteError($"Error getting last modified date for {uri}: {ex.Message}");
            return null;
        }
    }

    public async ValueTask<byte[]?> TryGetContentAsync(Uri uri, CancellationToken ct = default)
    {
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return null;
        }

        try
        {
            return await HttpClient.Shared.GetByteArrayAsync(uri, ct);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteError($"Error getting content for {uri}: {ex.Message}");
            return null;
        }
    }
}
