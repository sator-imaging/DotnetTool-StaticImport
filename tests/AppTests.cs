// Licensed under the MIT License
// https://github.com/sator-imaging/DotnetTool-StaticImport

using System;

namespace SatorImaging.DotnetTool.StaticImport.Core
{
    // hehe
    internal class AppTests
    {
        [Test]
        public void RunAllTests()
        {
            FileProviderTests.FileProviderRegistrationTest();
            FileProviderTests.FileProviderTest();
            ConditionalDirectiveTreeTests.SymbolCombinationTest();
            ConditionalDirectiveTreeTests.ParseComplexDirectiveTreeTest();
            TypeMigratorTests.TypeMigratorTest();
            TypeMigratorTests.RewriterTest();

            GitHubUrlBuilderTests.GitHubUrlBuilderTest();
            ConsoleTests.ReadKeyTest();

            Console.WriteImportantLine('\n' + @"  \\\ All tests were done ///" + '\n');
        }
    }
}
