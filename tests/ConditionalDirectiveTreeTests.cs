// Licensed under the MIT License
// https://github.com/sator-imaging/DotnetTool-StaticImport

using System;
using System.IO;
using System.Linq;

namespace SatorImaging.DotnetTool.StaticImport.Core
{
    public class ConditionalDirectiveTreeTests
    {
        [Test]
        public void SymbolCombinationTest()
        {
            static void expectError(string code)
            {
                try
                {
                    _ = ConditionalDirectiveTree.Parse(code);
                }
                catch (Exception error)
                {
                    Console.WriteLine($"Expected exception: {error.Message}");
                    return;
                }

                throw new Exception($"Expected exception has not thrown: {code}");
            }

            expectError("#if false\n#endif");  // this script file is loaded by other test case. don't use raw string literal here!

            //// seems that roslyn parser won't parse unnecessary directives
            //expectError("""
            //    #endif
            //    #if true
            //    #endif
            //    #endif
            //    """);

            var rootNode = ConditionalDirectiveTree.Parse("""
                #if ONE && TWO
                    #if THREE && FOUR
                      #if FIVE
                      #elif SIX
                      #endif
                    #endif
                #elif SEVEN && EIGHT
                    #if NINE
                    #endif
                #endif

                #if TEN
                #endif

                // duplicates must be ignored
                #if TEN
                #if TEN == TEN
                #elif TEN == TEN
                #elif TEN == TEN
                #endif
                #elif TEN
                #endif

                // must be parsed as empty node
                #if true
                #endif
                """);

            if (rootNode.Children.Last().Symbols.Count != 0)
            {
                throw new Exception($"[FAILED] {nameof(SymbolCombinationTest)}: last node Symbols must be empty");
            }

            var expected =
                // no symbol
                1
                // #if tree
                + (Math.Pow(2, 5) - 1)
                + (Math.Pow(2, 5) - 1)
                    // remove same combinations (#if ONE && TWO + #if THREE && FOUR)
                    - (Math.Pow(2, 4) - 1)
                // #elif tree
                + (Math.Pow(2, 3) - 1)
                // #if TEN and duplicates
                + 1
                // #if true
                + 0
                ;

            DumpDirectiveTreeInfo(rootNode);

            var combo = rootNode.ToSymbolCombinations();
            if (combo.Count != expected)
            {
                throw new Exception($"[FAILED] {nameof(SymbolCombinationTest)}: expected count is {expected} but was {combo.Count}");
            }
        }

        [Test]
        public void ParseComplexDirectiveTreeTest()
        {
            impl();

            static void impl()
            {
                // Resolve the test source relative to the test assembly's output directory.
                var sourcePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../SyntaxFixtures.cs"));
                var sourceCode = File.ReadAllText(sourcePath);

                var rootNode = ConditionalDirectiveTree.Parse(sourceCode);

                DumpDirectiveTreeInfo(rootNode);
            }
        }

        static void DumpDirectiveTreeInfo(ConditionalDirectiveTree.Node rootNode)
        {
            write(rootNode, 0);

            static void write(ConditionalDirectiveTree.Node node, int level)
            {
                Console.WriteDebugOnlyLine($"{level}: {new string(' ', level * 2)}{node}");

                foreach (var child in node.Children)
                {
                    write(child, level + 1);
                }
            }

            var combinations = rootNode.ToSymbolCombinations();

            foreach (var combo in combinations.Select(x => x.Order().ThenByDescending(y => y.Length)).OrderBy(x => x.FirstOrDefault()))
            {
                Console.WriteDebugOnlyLine(string.Join(", ", combo));
            }

            Console.WriteDebugOnlyLine($"Total combination count: {combinations.Count:#,0}");
        }
    }
}
