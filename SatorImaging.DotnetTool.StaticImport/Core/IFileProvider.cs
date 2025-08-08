// Licensed under the MIT License
// https://github.com/sator-imaging/DotnetTool-StaticImport

using System;
using System.Threading;
using System.Threading.Tasks;

namespace SatorImaging.DotnetTool.StaticImport.Core;

/// <summary>
/// Provides a unified interface for accessing file contents from various sources.
/// </summary>
internal interface IFileProvider
{
    /// <summary>
    /// Try to get the last modified date of the specified resource.
    /// </summary>
    /// <param name="uri">The URI of the resource.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The last modified date of the resource.
    /// Returns null if the resource is not found.
    /// Returns <see cref="DateTimeOffset.Now"/> if the last modified date is not available.
    /// </returns>
    ValueTask<DateTimeOffset?> TryGetLastModifiedDateAsync(Uri uri, CancellationToken ct = default);

    /// <summary>
    /// Try to get the content of the specified resource.
    /// </summary>
    /// <param name="uri">The URI of the resource.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The content of the resource as a byte array.</returns>
    ValueTask<byte[]?> TryGetContentAsync(Uri uri, CancellationToken ct = default);
}
