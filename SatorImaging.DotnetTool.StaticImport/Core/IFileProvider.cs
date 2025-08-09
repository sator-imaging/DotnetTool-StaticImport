// Licensed under the MIT License
// https://github.com/sator-imaging/DotnetTool-StaticImport

using System;
using System.Threading;
using System.Threading.Tasks;

namespace SatorImaging.DotnetTool.StaticImport.Core
{
    /// <summary>
    /// Provides a mechanism to access file content and metadata.
    /// </summary>
    internal interface IFileProvider
    {
        /// <summary>
        /// Tries to get the last modified date of a file.
        /// </summary>
        /// <param name="uri">The URI of the file.</param>
        /// <param name="ct">A cancellation token.</param>
        /// <returns>
        /// A <see cref="DateTimeOffset"/> if the file exists and the last modified date is available;
        /// otherwise, <c>null</c>. For remote resources, if the Last-Modified header is not found,
        /// it returns <see cref="DateTimeOffset.Now"/>.
        /// </returns>
        ValueTask<DateTimeOffset?> TryGetLastModifiedDateAsync(string uri, CancellationToken ct = default);

        /// <summary>
        /// Tries to get the content of a file.
        /// </summary>
        /// <param name="uri">The URI of the file.</param>
        /// <param name="ct">A cancellation token.</param>
        /// <returns>A byte array of the file content, or <c>null</c> if the file is not found.</returns>
        ValueTask<byte[]?> TryGetContentAsync(string uri, CancellationToken ct = default);
    }
}
