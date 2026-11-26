// Licensed under the MIT License
// https://github.com/sator-imaging/DotnetTool-StaticImport

using System;

namespace SatorImaging.DotnetTool.StaticImport.Core
{
    public class GitHubUrlBuilderTests
    {
        [Test]
        public void GitHubUrlBuilderTest()
        {
            foreach (var url in new string[]
            {
                "git",
                "github",
                "github:",
                "github:/",
                "github:@",
                "github:@/",
                "github:u@",
                "github:u@/",
                "github:@r",
                "github:@r/",
                "github:u@r",
                "github:u@r/",
                "github:u@r/b",
                "github:u@r/b/",
            })
            {
                try
                {
                    _ = GitHubFileProvider.ParseUrl(url);
                }
                catch (Exception error)
                {
                    Console.WriteLine($"Expected exception: {error.Message}");
                    continue;
                }

                throw new Exception($"Expected exception has not thrown: {url}");
            }

            var correctGitHubUrl = "github:u@r/b/f";
            _ = GitHubFileProvider.ParseUrl(correctGitHubUrl);

            Console.WriteLine($"Successfully parsed: {correctGitHubUrl}");
        }
    }
}
