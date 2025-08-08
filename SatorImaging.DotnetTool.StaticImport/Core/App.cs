// Licensed under the MIT License
// https://github.com/sator-imaging/DotnetTool-StaticImport

using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace SatorImaging.DotnetTool.StaticImport.Core;

public class App
{
    static async Task<int> Main(string[] args)
    {
        int result = SR.Result.ErrorUncategorized;

        try
        {
            var cmd = GetRootCommand();
            var options = cmd.Parse(args, new CommandLineConfiguration(cmd)
            {
                EnablePosixBundling = true,
            });

            // hehe: force run tests!
#if DEBUG
            if (options.GetValue(opt_TEST))
            {
                App_Tests.RunAllTests();
                return SR.Result.Succeeded;
            }
#endif

            return await options.InvokeAsync();
        }
        catch (Exception error)
        {
            Console.WriteError($"An error occurred: {error.Message}");
        }

        return result;
    }


    /*  command line args  ================================================================ */

    static readonly Option<string[]> opt_inputFilePaths = new("-i", "--input-files")
    {
        Description = $"""
            Input file paths or urls ({SR.HttpsSchemeFull}... or {SR.GitHubSchemeFull}user@repo/<REF>/path/to/file.ext)
            If environment variable `{SR.GitHubTokenVarName1st}` or `{SR.GitHubTokenVarName2nd}` is defined, it is used to access to GitHub
            """,
        Required = true,

        AllowMultipleArgumentsPerToken = true,
    };

    static readonly Option<string> opt_outputPath = new("-o", "--output-path")
    {
        Description = """
            Output folder or file path
            Current folder is used if omitted
            """,
    };

    static readonly Option<string> opt_outputFilePrefix = new("-op", "--output-prefix")
    {
        Description = "Output file prefix used when output path is set to folder",
    };

    static readonly Option<bool> opt_forceOverwrite = new("-f", "--force-overwrite")
    {
        Description = """
            Overwrite file without confirmation
            Note that output file will be overwritten even if it is newer than input file
            """,
    };

    static readonly Option<string> opt_namespace = new("--namespace")
    {
        Description = """
            [C#] Change namespace
                 If name is ending with '.', it will be prepended
            """,
    };

    static readonly Option<bool> opt_internal = new("--internal")
    {
        Description = "[C#] Change type visibility to `internal`",
    };

    static readonly Option<int> opt_timeout = new("--timeout")
    {
        Description = "Network timeout (milliseconds)",
    };

    static readonly Option<bool> opt_silent = new("--silent")
    {
        Description = "Suppress verbose logging",
    };

#if DEBUG
    static readonly Option<bool> opt_TEST = new("--TEST")
    {
        Description = "Run tests! (available only if DEBUG symbol is defined)",
    };
#endif


    /* =====  don't forget to update!  ===== */

    static RootCommand GetRootCommand()
    {
        var cmd = new RootCommand("""
            Migrate file(s) from another project, github or public website
            """);

        int i = 0;

        // need to insert to reorder...!!
#if DEBUG
        cmd.Options.Insert(i++, opt_TEST);
#endif
        cmd.Options.Insert(i++, opt_inputFilePaths);
        cmd.Options.Insert(i++, opt_outputPath);
        cmd.Options.Insert(i++, opt_outputFilePrefix);
        cmd.Options.Insert(i++, opt_forceOverwrite);
        cmd.Options.Insert(i++, opt_namespace);
        cmd.Options.Insert(i++, opt_internal);
        cmd.Options.Insert(i++, opt_timeout);
        cmd.Options.Insert(i++, opt_silent);

        cmd.SetAction((options, ct) => RunAsync(options, ct).AsTask());

        return cmd;
    }


    /* =====  impl  ===== */

    static ValueTask<int> RunAsync(ParseResult options, CancellationToken ct = default)
    {
        GitHubFileProvider.Instance.Initialize();

        var inputFilePaths = options.GetRequiredValue(opt_inputFilePaths);
        if (inputFilePaths.Length == 0)
        {
            Console.WriteError("no input file path or url");
            return new(SR.Result.ErrorUncategorized);
        }
        foreach (var input in inputFilePaths)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteError($"Empty input file path or url is found");
                return new(SR.Result.ErrorUncategorized);
            }
        }

        Console.IsSilentMode = options.GetValue(opt_silent);

        var timeout = options.GetValue(opt_timeout);
        if (timeout <= 0)
        {
            // value is 0 if flag is omitted.
            if (timeout != 0)
            {
                Console.WriteWarning($"invalid timeout is ignored: {timeout}");
            }
        }
        else
        {
            HttpClient.Timeout = TimeSpan.FromMilliseconds(timeout);
        }

        var outputPath = options.GetValue(opt_outputPath);
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = ".";
        }

        var newNamespace = options.GetValue(opt_namespace);
        if (string.IsNullOrWhiteSpace(newNamespace))
        {
            newNamespace = null;
        }

        var outputFilePrefix = options.GetValue(opt_outputFilePrefix);
        if (string.IsNullOrWhiteSpace(outputFilePrefix))
        {
            outputFilePrefix = null;
        }

        AppCore.RegisterFileProvider(Uri.UriSchemeFile, LocalFileProvider.Instance);
        AppCore.RegisterFileProvider(Uri.UriSchemeHttp, HttpFileProvider.Instance);
        AppCore.RegisterFileProvider(Uri.UriSchemeHttps, HttpFileProvider.Instance);
        AppCore.RegisterFileProvider(SR.GitHubScheme, GitHubFileProvider.Instance);

        return AppCore.ProcessAsync(
            inputUrlOrFilePaths: inputFilePaths,
            outputDirOrFilePath: outputPath,
            outputFilePrefix: outputFilePrefix,
            newNamespace: newNamespace,
            forceOverwrite: options.GetValue(opt_forceOverwrite),
            makeTypeInternal: options.GetValue(opt_internal),
            ct
            );
    }
}
