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

            var uriCreationOptions = new UriCreationOptions();
            var typeMigrator = new TypeMigrator();

            foreach (var inputUrlOrPath in inputUrlOrFilePaths)
            {
                _ = Uri.TryCreate(inputUrlOrPath, uriCreationOptions, out Uri? inputUrl);

                string outputPath = outputDirOrFilePath;

                if (isOutputDirectory)
                {
                    string fileName = (inputUrl == null)
                        ? Path.GetFileName(inputUrlOrPath)
                        : Path.GetFileName(inputUrl.AbsolutePath)
                        ;

                    outputPath = Path.Combine(outputDirOrFilePath, (outputFilePrefix + fileName));
                }

                string? inputPath = inputUrlOrPath;

                // need to check before downloading file.
                bool applyCSharpScriptFilter = isCSharpScriptMode && IsCSharpScriptFile(inputPath);

                if (inputUrl != null)
                {
                    // full path of local file may be parsed as url, ignore it.
                    if (inputUrl.Scheme != SR.FileScheme)
                    {
                        inputPath = await TryDownloadFileAsync(inputUrl, ct);

                        if (inputPath == null)
                        {
                            return SR.Result.ErrorUncategorized;
                        }
                    }
                }

                var inputInfo = new FileInfo(inputPath);
                if (!inputInfo.Exists)
                {
                    Console.WriteError($"Input file is not found: {inputPath}");
                    return SR.Result.ErrorUncategorized;
                }

                var outputInfo = new FileInfo(outputPath);
                if (outputInfo.Exists)
                {
                    if (inputInfo.Exists)
                    {
                        if (inputInfo.LastWriteTimeUtc <= outputInfo.LastWriteTimeUtc)
                        {
                            Console.WriteImportantLine($"No change: {outputPath}");
                            continue;
                        }
                    }

                    if (!forceOverwrite)
                    {
                        if (!Console.CanReadKey)
                        {
                            Console.WriteWarning($"cannot read user key input. set force overwrite option to copy file: {outputPath}");
                            continue;
                        }

                        var choice = Console.ReadKey($"File exists ({outputPath})  overwrite? [y/N]: ");
                        if (choice.Key != ConsoleKey.Y)
                        {
                            Console.WriteImportantLine($"Skipped: {outputPath}");
                            continue;
                        }
                    }
                }

                // apply only when input file is .cs file.
                if (applyCSharpScriptFilter)
                {
                    await Task.Run(async () =>
                    {
                        Console.WriteLine();  // spacer for non-silent mode

                        var sourceCode = await File.ReadAllTextAsync(inputPath, ct);
                        var outputFileContent = typeMigrator.Migrate(sourceCode, newNamespace, makeTypeInternal);

                        await File.WriteAllTextAsync(outputPath, outputFileContent, Encoding.UTF8, ct);

                        Console.WriteImportantLine($"File written: {outputPath}");
                    },
                    ct);
                }
                else
                {
                    File.Copy(inputPath, outputPath, overwrite: true);

                    Console.WriteImportantLine($"File copied: {outputPath}");
                }

                Console.WriteLine();  // spacer for non-silent mode
            }

            return SR.Result.Succeeded;
        }


        static bool IsCSharpScriptFile(string filePath)
        {
            return filePath.EndsWith(SR.EXT_CS, StringComparison.OrdinalIgnoreCase);
        }


        static async Task<string?> TryDownloadFileAsync(Uri url, CancellationToken ct = default)
        {
            byte[]? downloadedBytes = null;
            DateTimeOffset? lastModified = null;

            if (url.Scheme == SR.GitHubScheme)
            {
                (downloadedBytes, lastModified) = await GitHubFileProvider.Instance.TryGetAsync(url.ToString(), ct);

                if (downloadedBytes == null)
                {
                    Console.WriteError($"Failed to download from GitHub: {url}");
                    goto ERROR;
                }
            }
            else if (url.Scheme == SR.HttpsScheme)
            {
                (downloadedBytes, lastModified) = await HttpFileProvider.Instance.TryGetAsync(url.ToString(), ct);

                if (downloadedBytes == null)
                {
                    Console.WriteError($"Failed to download from url: {url}");
                    goto ERROR;
                }
            }

            if (downloadedBytes == null)
            {
                Console.WriteError($"Unknown url scheme: {url}");
                goto ERROR;
            }

            //main
            var tempFilePath = Path.GetTempFileName();

            await File.WriteAllBytesAsync(tempFilePath, downloadedBytes, ct);

            if (lastModified.HasValue)
            {
                File.SetLastWriteTimeUtc(tempFilePath, lastModified.Value.UtcDateTime);
            }
            else
            {
                Console.WriteWarning($"Cannot retrieve last modified date: {url}");
            }


            return tempFilePath;

        ERROR:
            return null;
        }

    }
}
