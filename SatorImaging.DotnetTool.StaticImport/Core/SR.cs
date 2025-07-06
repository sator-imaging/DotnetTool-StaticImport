// Licensed under the MIT License
// https://github.com/sator-imaging/DotnetTool-StaticImport

namespace SatorImaging.DotnetTool.StaticImport.Core;

/// <summary>
/// Stands for static resources.
/// </summary>
internal static class SR
{
    public static class Result
    {
        public const int Succeeded = 0;
        public const int ErrorUncategorized = 1;
    }

    public const int DefaultListCapacity = 8;

    public const string UserAgentName = $"{nameof(SatorImaging)}.{nameof(DotnetTool)}.{nameof(StaticImport)}";
    public const string UserAgentVersion = "1.0.0";

    public const string GitHubTokenVariableName = "GITHUB_TOKEN";
    public const string EXT_CS = ".cs";

    public const string HttpsScheme = "https";
    public const string HttpsSchemeFull = HttpsScheme + "://";

    public const string GitHubScheme = "github";
    public const string GitHubSchemeFull = GitHubScheme + ":";
    public const string GitHubHostName = "https://github.com";
    public const string GitHubApiHostName = "https://api.github.com";
}
