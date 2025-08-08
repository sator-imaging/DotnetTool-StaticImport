// Licensed under the MIT License
// https://github.com/sator-imaging/DotnetTool-StaticImport

namespace SatorImaging.DotnetTool.StaticImport.Core;

/// <summary>
/// Provides a unified interface for transforming content.
/// </summary>
internal interface IContentTransformer
{
    /// <summary>
    /// Transforms the given content.
    /// </summary>
    /// <param name="content">The content to transform.</param>
    /// <returns>The transformed content.</returns>
    string Transform(string content);
}
