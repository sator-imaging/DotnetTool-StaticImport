// Licensed under the MIT License
// https://github.com/sator-imaging/DotnetTool-StaticImport

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SatorImaging.DotnetTool.StaticImport.Core
{
    internal static class AppCore
    {
        static readonly UTF8Encoding Encoder = new(encoderShouldEmitUTF8Identifier: false);

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
                IFileProvider fileProvider;
                string outputPath = outputDirOrFilePath;

                if (Uri.TryCreate(inputUrlOrPath, UriKind.Absolute, out var inputUri))
                {
                    switch (inputUri.Scheme)
                    {
                        case SR.HttpsScheme:
                            fileProvider = HttpFileProvider.Instance;
                            if (isOutputDirectory)
                            {
                                string fileName = Path.GetFileName(inputUri.AbsolutePath);
                                outputPath = Path.Combine(outputDirOrFilePath, (outputFilePrefix + fileName));
                            }
                            break;

                        case SR.GitHubScheme:
                            fileProvider = GitHubFileProvider.Instance;
                            if (isOutputDirectory)
                            {
                                string fileName = Path.GetFileName(inputUri.AbsolutePath);
                                outputPath = Path.Combine(outputDirOrFilePath, (outputFilePrefix + fileName));
                            }
                            break;

                        case SR.FileScheme:
                            fileProvider = LocalFileProvider.Instance;
                            if (isOutputDirectory)
                            {
                                string fileName = Path.GetFileName(inputUri.LocalPath);
                                outputPath = Path.Combine(outputDirOrFilePath, (outputFilePrefix + fileName));
                            }
                            break;

                        default:
                            Console.WriteError($"Unsupported URI scheme: {inputUri.Scheme}");
                            return SR.Result.ErrorUncategorized;
                    }
                }
                else
                {
                    // Not an absolute URI, assume it's a local file path.
                    fileProvider = LocalFileProvider.Instance;
                    if (isOutputDirectory)
                    {
                        string fileName = Path.GetFileName(inputUrlOrPath);
                        outputPath = Path.Combine(outputDirOrFilePath, (outputFilePrefix + fileName));
                    }
                }

                var sourceLastModified = await fileProvider.TryGetLastModifiedDateAsync(inputUrlOrPath, ct);
                if (sourceLastModified == null)
                {
                    Console.WriteError($"Input not found or could not be accessed: {inputUrlOrPath}");
                    return SR.Result.ErrorUncategorized;
                }

                string resultMessage = string.Empty;
                var outputInfo = new FileInfo(outputPath);
                if (outputInfo.Exists)
                {
                    if (!forceOverwrite)
                    {
                        if (sourceLastModified.Value.ToUniversalTime() <= outputInfo.LastWriteTimeUtc)
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
                    resultMessage = "[overwritten] ";
                }


                var contentBytes = await fileProvider.TryGetContentAsync(inputUrlOrPath, ct);
                if (contentBytes == null)
                {
                    Console.WriteError($"Failed to get content for: {inputUrlOrPath}");
                    return SR.Result.ErrorUncategorized;
                }

                bool applyCSharpScriptFilter = isCSharpScriptMode && IsCSharpScriptFile(outputPath);
                if (applyCSharpScriptFilter)
                {
                    await Task.Run(async () =>
                    {
                        Console.WriteLine(); // spacer
                        var transformedBytes = typeMigrator.Transform(contentBytes);
                        await File.WriteAllBytesAsync(outputPath, transformedBytes, ct);
                    }, ct);

                    resultMessage += $"File written: {outputPath}";
                }
                else
                {
                    await File.WriteAllBytesAsync(outputPath, contentBytes, ct);
                    resultMessage += $"File copied: {outputPath}";
                }

                File.SetLastWriteTimeUtc(outputPath, sourceLastModified.Value.UtcDateTime);

                Console.WriteImportantLine(resultMessage);
                Console.WriteLine();  // spacer
            }

            return SR.Result.Succeeded;
        }


        static bool IsCSharpScriptFile(string filePath)
        {
            return filePath.EndsWith(SR.EXT_CS, StringComparison.OrdinalIgnoreCase);
        }

    }
}
