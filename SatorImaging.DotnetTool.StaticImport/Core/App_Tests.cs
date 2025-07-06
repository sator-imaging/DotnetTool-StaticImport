// Licensed under the MIT License
// https://github.com/sator-imaging/DotnetTool-StaticImport

#if DEBUG

using System;
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
            ParseDirectiveTree();
            GitHubUrlBuilder();

            var key = Console.ReadKey("ReadKey timeout test (1,000ms): ", 1000);
            Console.WriteLine($"Key: '{key.Key}'  KeyChar: '{key.KeyChar}'  Modifiers: '{key.Modifiers}'");
        }


        public static void ParseDirectiveTree()
        {
            impl();

            static void impl([CallerFilePath] string? thisScriptFilePath = null)
            {
                if (string.IsNullOrWhiteSpace(thisScriptFilePath))
                    return;

                var sourceCode = File.ReadAllText(thisScriptFilePath);

                var rootNode = ConditionalDirectiveTree.Parse(sourceCode);
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
                }

                var combinations = rootNode.ToSymbolCombinations();

                foreach (var combo in combinations.Select(x => x.OrderByDescending(y => y.Length)).OrderBy(x => x.FirstOrDefault()))
                {
                    Console.WriteDebugOnlyLine(string.Join(", ", combo));
                }

                Console.WriteDebugOnlyLine($"Total combination count: {combinations.Count}");
            }
        }

        public static void GitHubUrlBuilder()
        {
            foreach (var url in new string[]{
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
                "github:u@r/",
                "github:u@r/d",
                "github:u@r/d/",
                //"github:u@r/d/file.ext",
            })
            {
                try
                {
                    _ = Core.GitHubFileProvider.ParseUrl(url);
                }
                catch (Exception error)
                {
                    Console.WriteLine($"Expected exception: {error.Message}");
                    continue;
                }

                throw new Exception($"Expected exception has not thrown: {url}");
            }
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
#elif Z_MULTI_USE
#endif
#if Z_MULTI_USE
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
