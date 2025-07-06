// Licensed under the MIT License
// https://github.com/sator-imaging/DotnetTool-StaticImport

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SatorImaging.DotnetTool.StaticImport.Core;

internal class HttpFileProvider  // TODO : IFileProvider
{
#pragma warning disable IDE0079
#pragma warning disable CA1822   // Instance/Default/Shared members should not be made static
#pragma warning restore IDE0079

    public static HttpFileProvider Instance { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; set; } = new();

    protected HttpFileProvider() { }


    public async ValueTask<(byte[]? content, DateTimeOffset? lastModified)> TryGetAsync(string url, CancellationToken ct = default)
    {
        if (!url.StartsWith(SR.HttpsSchemeFull, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"invalid url scheme: {url}");
        }

        var client = HttpClient.Shared;

        var GET = await client.GetAsync(url, ct);
        if (!GET.IsSuccessStatusCode)
        {
            return (null, null);
        }

        var lastModified = GET.Content.Headers.LastModified;

        var content = await GET.Content.ReadAsByteArrayAsync(ct);
        return (content, lastModified);
    }
}
