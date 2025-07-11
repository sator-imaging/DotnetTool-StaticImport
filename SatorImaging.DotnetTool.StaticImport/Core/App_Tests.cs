// Licensed under the MIT License
// https://github.com/sator-imaging/DotnetTool-StaticImport

#if DEBUG

using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SatorImaging.DotnetTool.StaticImport.Core
{
    // hehe
    internal static class App_Tests
    {
        public static void RunAllTests()
        {
            SymbolCombinationTest();
            ParseComplexDirectiveTreeTest();
            TypeMigratorTest();
            RewriterTest();

            GitHubUrlBuilderTest();
            ReadKeyTest();

            Console.WriteImportantLine('\n' + @"  \\\ All tests was done ///" + '\n');
        }


        static void SymbolCombinationTest()
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

        static void ParseComplexDirectiveTreeTest()
        {
            impl();

            static void impl([CallerFilePath] string? thisScriptFilePath = null)
            {
                if (string.IsNullOrWhiteSpace(thisScriptFilePath))
                    return;

                var sourceCode = File.ReadAllText(thisScriptFilePath);

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


        static void TypeMigratorTest()
        {
            var sourceCode = """
                namespace Foo.Bar.Baz
                {
                    namespace Untouched
                    {
                        public class MyClass
                        {
                            public enum UntouchedEnum { }
                            public struct UntouchedStruct { }
                            public record UntouchedRecord { }
                            public interface IUntouched { }
                        }
                    }
                }
                """;

            _ = new TypeMigrator().Migrate(sourceCode, newNamespace: null, makeTypeInternal: false);
            _ = new TypeMigrator().Migrate(sourceCode, newNamespace: null, makeTypeInternal: true);
            _ = new TypeMigrator().Migrate(sourceCode, "ReplacedNamespace", false);
            _ = new TypeMigrator().Migrate(sourceCode, "PrefixMode.", true);
        }

        static void RewriterTest()
        {
            var nestedNamespaces = """
                namespace Root.Name.Space
                {
                    namespace NestedNS
                    {
                        namespace DeepNestNS { }
                    }
                }
                """;

            var root = CSharpSyntaxTree.ParseText(nestedNamespaces).GetRoot();

            var ns = new TypeMigrator.NamespaceRewriter("REP");
            ns.Visit(root);
            Debug.Assert(ns.ChangeLog.Count == 1);
            Debug.Assert(ns.ChangeLog[0].from == "Root.Name.Space");
            Debug.Assert(ns.ChangeLog[0].to == "REP");

            ns = new TypeMigrator.NamespaceRewriter("PREFIX.");
            ns.Visit(root);
            Debug.Assert(ns.ChangeLog.Count == 1);
            Debug.Assert(ns.ChangeLog[0].from == "Root.Name.Space");
            Debug.Assert(ns.ChangeLog[0].to == "PREFIX.Root.Name.Space");

            var fileScopedNsWithTypes = """
                namespace File.Scoped;

                public class Root
                {
                    public class Nested
                    {
                        public class DeepNest { }
                    }
                }
                public record RootRecord { }
                public struct RootStruct { }
                public interface IRoot { }
                public enum ERoot { }
                """;

            root = CSharpSyntaxTree.ParseText(fileScopedNsWithTypes).GetRoot();

            ns = new TypeMigrator.NamespaceRewriter("REP");
            ns.Visit(root);
            Debug.Assert(ns.ChangeLog.Count == 1);
            Debug.Assert(ns.ChangeLog[0].from == "File.Scoped");
            Debug.Assert(ns.ChangeLog[0].to == "REP");

            ns = new TypeMigrator.NamespaceRewriter("PREFIX.");
            ns.Visit(root);
            Debug.Assert(ns.ChangeLog.Count == 1);
            Debug.Assert(ns.ChangeLog[0].from == "File.Scoped");
            Debug.Assert(ns.ChangeLog[0].to == "PREFIX.File.Scoped");

            var modifier = new TypeMigrator.TypeModifierRewriter(SyntaxKind.PublicKeyword, SyntaxKind.InternalKeyword);
            modifier.Visit(root);
            Debug.Assert(modifier.ChangeLog.Count == 5);
            Debug.Assert(modifier.ChangeLog[0] == "Root");
            Debug.Assert(modifier.ChangeLog[1] == "RootRecord");
            Debug.Assert(modifier.ChangeLog[2] == "RootStruct");
            Debug.Assert(modifier.ChangeLog[3] == "IRoot");
            Debug.Assert(modifier.ChangeLog[4] == "ERoot");

            Console.WriteImportantLine("\ntype rewriter unit tests were done\n");
        }


        static void GitHubUrlBuilderTest()
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

        static void ReadKeyTest()
        {
            if (!Console.CanReadKey)
            {
                Console.WriteWarning("cannot read key. skipped.");
                return;
            }

            var key = Console.ReadKey("ReadKey timeout test (1,000ms): ", 1000);
            Console.WriteLine($"Modifiers: '{key.Modifiers}'  Key: '{key.Key}'  KeyChar: '{key.KeyChar}'");
        }
    }
}


#region ////////  Test Type Declarations  ////////
#if true && !false

//// NOTE: expected exception is thrown if directive is found that is always false.
//#if false == true
//#elif true == false
//#elif false
//#endif

#endif

// same symbol must be removed from combinations.
#if Z_MULTI_USE
#endif

#if Z_MULTI_USE
#elif Z_MULTI_USE
#if Z_MULTI_USE
#endif
#elif Z_MULTI_USE
#endif

#if DEBUG == true && NET == true

namespace SatorImaging.DotnetTool.StaticImport.Core
{
    namespace NestedNamespaceMustBeUntouched
    {
        public class TestFile
        {
            public static void TestMethod() => _ = ConsoleKey.Enter;
        }

        public abstract partial class AbstractPartialClass { }
        public abstract class AbstractClass { }
        public partial class PartialClass { }
        public static class StaticClass { }
        public sealed class SealedClass { }
        public sealed partial class SealedPartialClass { }

        public struct Struct { }
        public partial struct PartialStruct { }
        public readonly struct ReadOnlyStruct { }
        public readonly partial struct ReadOnlyPartialStruct { }
    }
}

#if true == NESTED_DIRECTIVE_IS_NOT_SUPPORTED

namespace HELLO
{
    public class WORLD
    {
        public class MakeMeInternal { }
    }
}

#elif FOO == BAR || BAZ == QUX
#if DEEP_NEST == !true
#if DEEP_NEST_MORE == !true
#if !(A != B && C != D && E)

namespace DeepNestNamespace
{
    namespace DoNotTouchMe
    {
        public class ThisClassMustBeInternalIfRequested
        {
            public sealed class NestedClass
            {
                public static partial class DeepNest { }
            }
        }
    }
}

#endif
#endif
#endif
#endif

#endif

#if !NET == false

namespace SatorImaging.DotnetTool.StaticImport.Core
{
    namespace OtherNestedNamespace
    {
        public interface ISomething { }

#pragma warning disable IDE0079
#pragma warning disable SMA0026 // Enum Obfuscation
        public enum TheEnum
        {
            Default,
            Value,
        }
#pragma warning restore SMA0026 // Enum Obfuscation
#pragma warning restore IDE0079

        public partial record PartialRecord { }
        public sealed record SealedRecord { }
        public record Record { }
        public record struct RecordStruct { }
        public partial record struct PartialRecordStruct { }
        public readonly record struct ReadOnlyRecordStruct { }
        public readonly partial record struct ReadOnlyPartialRecordStruct { }
    }
}

#endif
#endregion

#endif
