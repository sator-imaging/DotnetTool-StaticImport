// Licensed under the MIT License
// https://github.com/sator-imaging/DotnetTool-StaticImport

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SatorImaging.DotnetTool.StaticImport.Core
{
    internal class LocalFileProvider : IFileProvider
    {
        public static LocalFileProvider Instance { get; } = new();

        private LocalFileProvider() { }

        public ValueTask<DateTimeOffset?> TryGetLastModifiedDateAsync(string uri, CancellationToken ct = default)
        {
            if (File.Exists(uri))
            {
                return new ValueTask<DateTimeOffset?>(new DateTimeOffset(File.GetLastWriteTimeUtc(uri)));
            }
            return new ValueTask<DateTimeOffset?>(result: null);
        }

        public async ValueTask<byte[]?> TryGetContentAsync(string uri, CancellationToken ct = default)
        {
            if (File.Exists(uri))
            {
                return await File.ReadAllBytesAsync(uri, ct);
            }
            return null;
        }
    }
}
