// Licensed under the MIT License
// https://github.com/sator-imaging/DotnetTool-StaticImport

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SatorImaging.DotnetTool.StaticImport.Core
{
    internal static class AppCore
    {
        static readonly UTF8Encoding Encoder = new(encoderShouldEmitUTF8Identifier: false);
        private static readonly Dictionary<string, IFileProvider> _fileProviders = new(StringComparer.OrdinalIgnoreCase);

        public static void RegisterFileProvider(string scheme, IFileProvider provider)
        {
            _fileProviders[scheme] = provider;
        }

        public static async ValueTask<int> ProcessAsync(
            string[] inputUrlOrFilePaths,
            string outputDirOrFilePath,
            string? outputFilePrefix,
            string? newNamespace,
            bool forceOverwrite,
            bool makeTypeInternal,
            CancellationToken ct = default
            )
        {
            bool isCSharpScriptMode = makeTypeInternal || newNamespace != null;

            bool isOutputDirectory = Directory.Exists(outputDirOrFilePath);
            if (!isOutputDirectory)
            {
                if (inputUrlOrFilePaths.Length > 1)
                {
                    Console.WriteError($"output file is specified for multiple inputs: {outputDirOrFilePath}");
                    return SR.Result.ErrorUncategorized;
                }
            }

            var typeMigrator = new TypeMigrator(newNamespace, makeTypeInternal);

            foreach (var inputUrlOrPath in inputUrlOrFilePaths)
            {
                if (!Uri.TryCreate(inputUrlOrPath, UriKind.Absolute, out var inputUri))
                {
                    var fullPath = Path.GetFullPath(inputUrlOrPath);
                    if (!Uri.TryCreate("file://" + fullPath, UriKind.Absolute, out inputUri))
                    {
                        Console.WriteError($"Invalid input format: {inputUrlOrPath}");
                        return SR.Result.ErrorUncategorized;
                    }
                }


                if (!_fileProviders.TryGetValue(inputUri.Scheme, out var fileProvider))
                {
                    Console.WriteError($"No file provider found for scheme: {inputUri.Scheme}");
                    return SR.Result.ErrorUncategorized;
                }

                string outputPath = outputDirOrFilePath;
                if (isOutputDirectory)
                {
                    string fileName = Path.GetFileName(inputUri.IsFile ? inputUri.LocalPath : inputUri.AbsolutePath);
                    outputPath = Path.Combine(outputDirOrFilePath, (outputFilePrefix + fileName));
                }

                var outputInfo = new FileInfo(outputPath);

                if (!forceOverwrite && outputInfo.Exists)
                {
                    var sourceLastModified = await fileProvider.TryGetLastModifiedDateAsync(inputUri, ct);
                    if (sourceLastModified.HasValue && sourceLastModified.Value <= outputInfo.LastWriteTimeUtc)
                    {
                        Console.WriteImportantLine($"Up to date: {outputPath}");
                        continue;
                    }

                    if (!Console.CanReadKey)
                    {
                        Console.WriteError($"Failed to read key input. Set force overwrite option to copy file: {outputPath}");
                        return SR.Result.ErrorUncategorized;
                    }

                    var choice = Console.ReadKey($"File exists ({outputPath})  overwrite? [N/y]: ");
                    if (choice.Key != ConsoleKey.Y)
                    {
                        Console.WriteImportantLine($"Skipped: {outputPath}");
                        continue;
                    }
                }

                var contentBytes = await fileProvider.TryGetContentAsync(inputUri, ct);
                if (contentBytes == null)
                {
                    Console.WriteError($"Failed to get content from: {inputUrlOrPath}");
                    return SR.Result.ErrorUncategorized;
                }

                string resultMessage = outputInfo.Exists ? "[overwritten] " : "";

                bool applyCSharpScriptFilter = isCSharpScriptMode && IsCSharpScriptFile(outputPath);
                if (applyCSharpScriptFilter)
                {
                    var sourceCode = Encoder.GetString(contentBytes);
                    var outputFileContent = typeMigrator.Transform(sourceCode);
                    await File.WriteAllTextAsync(outputPath, outputFileContent, Encoder, ct);
                    resultMessage += $"File written: {outputPath}";
                }
                else
                {
                    await File.WriteAllBytesAsync(outputPath, contentBytes, ct);
                    resultMessage += $"File copied: {outputPath}";
                }

                Console.WriteImportantLine(resultMessage);
                Console.WriteLine();
            }

            return SR.Result.Succeeded;
        }

        static bool IsCSharpScriptFile(string filePath)
        {
            return filePath.EndsWith(SR.EXT_CS, StringComparison.OrdinalIgnoreCase);
        }
    }
}
