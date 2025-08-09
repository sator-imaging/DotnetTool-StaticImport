// Licensed under the MIT License
// https://github.com/sator-imaging/DotnetTool-StaticImport

namespace SatorImaging.DotnetTool.StaticImport.Core
{
    /// <summary>
    /// Provides a mechanism to transform content.
    /// </summary>
    internal interface IContentTransformer
    {
        /// <summary>
        /// Transforms the given content.
        /// </summary>
        /// <param name="content">The content to transform.</param>
        /// <param name="newNamespace">The new namespace to apply.</param>
        /// <param name="makeTypeInternal">Whether to make types internal.</param>
        /// <returns>The transformed content.</returns>
        string Transform(string content, string? newNamespace, bool makeTypeInternal);
    }
}
