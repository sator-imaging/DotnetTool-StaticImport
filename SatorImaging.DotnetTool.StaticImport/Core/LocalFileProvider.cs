// Licensed under the MIT License
// https://github.com/sator-imaging/DotnetTool-StaticImport

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SatorImaging.DotnetTool.StaticImport.Core;

internal class LocalFileProvider : IFileProvider
{
    public static LocalFileProvider Instance { get; } = new();

    private LocalFileProvider() { }

    public ValueTask<byte[]?> TryGetContentAsync(Uri uri, CancellationToken ct = default)
    {
        if (!uri.IsFile)
        {
            return ValueTask.FromResult<byte[]?>(null);
        }

        try
        {
            return new ValueTask<byte[]?>(File.ReadAllBytes(uri.LocalPath));
        }
        catch (Exception ex)
        {
            Console.WriteError(ex.Message);
            return ValueTask.FromResult<byte[]?>(null);
        }
    }

    public ValueTask<DateTimeOffset?> TryGetLastModifiedDateAsync(Uri uri, CancellationToken ct = default)
    {
        if (!uri.IsFile)
        {
            return ValueTask.FromResult<DateTimeOffset?>(null);
        }

        try
        {
            return ValueTask.FromResult<DateTimeOffset?>(new FileInfo(uri.LocalPath).LastWriteTimeUtc);
        }
        catch (Exception ex)
        {
            Console.WriteError(ex.Message);
            return ValueTask.FromResult<DateTimeOffset?>(null);
        }
    }
}
