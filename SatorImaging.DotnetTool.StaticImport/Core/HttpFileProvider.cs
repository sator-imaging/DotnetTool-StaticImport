// Licensed under the MIT License
// https://github.com/sator-imaging/DotnetTool-StaticImport

using System;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SatorImaging.DotnetTool.StaticImport.Core;

internal class HttpFileProvider : IFileProvider
{
#pragma warning disable IDE0079
#pragma warning disable CA1822   // Instance/Default/Shared members should not be made static
#pragma warning restore IDE0079

    public static HttpFileProvider Instance { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; set; } = new();

    protected HttpFileProvider() { }


    public async ValueTask<DateTimeOffset?> TryGetLastModifiedDateAsync(string uri, CancellationToken ct = default)
    {
        if (!uri.StartsWith(SR.HttpsSchemeFull, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"invalid url scheme: {uri}");
        }

        try
        {
            var client = HttpClient.Shared;
            using var req = new HttpRequestMessage(HttpMethod.Head, uri);
            var res = await client.SendAsync(req, ct);

            if (!res.IsSuccessStatusCode)
            {
                Console.WriteWarning($"{res}");
                return null; // File not found or other error
            }

            // File found, check for header
            return res.Content.Headers.LastModified ?? DateTimeOffset.Now;
        }
        catch (HttpRequestException e)
        {
            Console.WriteWarning(e.ToString());
            return null; // Network error
        }
    }

    public async ValueTask<byte[]?> TryGetContentAsync(string uri, CancellationToken ct = default)
    {
        if (!uri.StartsWith(SR.HttpsSchemeFull, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"invalid url scheme: {uri}");
        }

        try
        {
            var client = HttpClient.Shared;
            var GET = await client.GetAsync(uri, ct);
            if (!GET.IsSuccessStatusCode)
            {
                Console.WriteWarning($"{GET}");
                return null;
            }

            return await GET.Content.ReadAsByteArrayAsync(ct);
        }
        catch (HttpRequestException e)
        {
            Console.WriteWarning(e.ToString());
            return null;
        }
    }

        public string GetOutputFilePath(Uri uri, string outputDirOrFilePath, string? outputFilePrefix, bool isOutputDirectory)
        {
            if (!isOutputDirectory)
            {
                return outputDirOrFilePath;
            }
            string fileName = Path.GetFileName(uri.AbsolutePath);
            return Path.Combine(outputDirOrFilePath, (outputFilePrefix + fileName));
        }
}
