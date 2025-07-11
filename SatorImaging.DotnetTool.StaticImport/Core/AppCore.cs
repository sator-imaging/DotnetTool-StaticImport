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

        // TODO: new logic to reduce network traffic and provide IFileProvider/IContentTransformer extension points.
        //       - create IFileProvider (local/http/github)
        //       - fileProvider.TryGetLastModifiedDate (head request)
        //         - null if remote file not found
        //         - DateTimeOffset.Now if no Last-Modified header found
        //       - compare lastModified and overwrite if requested
        //       - fileProvider.TryGetContent
        //       - apply IContentTransformer (TypeMigrator, etc.)
        //       - write file.
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

                string resultMessage = string.Empty;

                var outputInfo = new FileInfo(outputPath);
                if (outputInfo.Exists)
                {
                    if (!forceOverwrite)
                    {
                        if (inputInfo.LastWriteTimeUtc <= outputInfo.LastWriteTimeUtc)
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

                // apply only when input file is .cs file.
                if (applyCSharpScriptFilter)
                {
                    await Task.Run(async () =>
                    {
                        Console.WriteLine();  // spacer for non-silent mode

                        var sourceCode = await File.ReadAllTextAsync(inputPath, ct);
                        var outputFileContent = typeMigrator.Migrate(sourceCode, newNamespace, makeTypeInternal);

                        await File.WriteAllTextAsync(outputPath, outputFileContent, Encoder, ct);
                    },
                    ct);

                    resultMessage = $"{resultMessage}File written: {outputPath}";
                }
                else
                {
                    File.Copy(inputPath, outputPath, overwrite: true);

                    resultMessage = $"{resultMessage}File copied: {outputPath}";
                }

                Console.WriteImportantLine(resultMessage);
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
