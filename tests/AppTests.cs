// Licensed under the MIT License
// https://github.com/sator-imaging/DotnetTool-StaticImport

using System;

namespace SatorImaging.DotnetTool.StaticImport.Core
{
    // hehe
    internal partial class AppTests
    {
        [Test]
        public void RunAllTests()
        {
            FileProviderRegistrationTest();
            FileProviderTest();
            SymbolCombinationTest();
            ParseComplexDirectiveTreeTest();
            TypeMigratorTest();
            RewriterTest();

            GitHubUrlBuilderTest();
            ReadKeyTest();

            Console.WriteImportantLine('\n' + @"  \\\ All tests were done ///" + '\n');
        }
    }
}
