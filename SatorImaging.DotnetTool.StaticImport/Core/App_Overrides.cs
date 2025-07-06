// Licensed under the MIT License
// https://github.com/sator-imaging/DotnetTool-StaticImport

using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Threading;
using CONSOLE = System.Console;
using HTTP_CLIENT = System.Net.Http.HttpClient;

// hehe: prevent direct use of system libraries

namespace SatorImaging.DotnetTool.StaticImport.Core
{
    internal static class Console
    {
        public static void WriteImportantLine(object message)
        {
            CONSOLE.WriteLine(message);
        }

        public static void WriteError(object message)
        {
            CONSOLE.ForegroundColor = ConsoleColor.Red;
            CONSOLE.Error.WriteLine(message);
            CONSOLE.ResetColor();
        }


        public static bool IsSilentMode { get; set; }

        public static void WriteLine(object? message = null)
        {
            if (IsSilentMode)
                return;

            CONSOLE.WriteLine(message);
        }

        public static void WriteWarning(object message)
        {
            if (IsSilentMode)
                return;

            CONSOLE.ForegroundColor = ConsoleColor.Yellow;
            CONSOLE.WriteLine(message);
            CONSOLE.ResetColor();
        }

        [Conditional("DEBUG")] public static void WriteDebugOnlyLine(object message) => WriteLine(message);


        // for GitHub actions.
        public static bool CanReadKey => !CONSOLE.IsInputRedirected;

        // must be finished without user interaction.
        public static ConsoleKeyInfo ReadKey(string message, int timeoutMilliseconds = 10_000, ConsoleKeyInfo timeoutKey = default)
        {
            // ensure key is not available.
            while (CONSOLE.KeyAvailable)
            {
                _ = CONSOLE.ReadKey();
            }

            CONSOLE.Write($"{message.TrimEnd()} ");

            var startAt = Stopwatch.GetTimestamp();

            while (!CONSOLE.KeyAvailable)
            {
                Thread.Sleep(250);

                var elapsed = Stopwatch.GetElapsedTime(startAt);
                if (elapsed.TotalMilliseconds > timeoutMilliseconds)
                {
                    WriteWarning("timed out");
                    return timeoutKey;
                }
            }

            var result = CONSOLE.ReadKey();
            CONSOLE.WriteLine();

            return result;
        }
    }

    internal static class HttpClient
    {
        public static TimeSpan Timeout { get; internal set; } = TimeSpan.FromSeconds(10);

        public static HTTP_CLIENT Shared
        {
            get
            {
                var client = cache_client.Value;
                return client;
            }
        }

        static readonly Lazy<HTTP_CLIENT> cache_client = new(() =>
        {
            var client = new HTTP_CLIENT();

            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(SR.UserAgentName, SR.UserAgentVersion));

            client.Timeout = Timeout;

            return client;
        },
        isThreadSafe: true);
    }
}
