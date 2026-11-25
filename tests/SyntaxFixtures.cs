// Licensed under the MIT License
// https://github.com/sator-imaging/DotnetTool-StaticImport

#if DEBUG || !DEBUG || true // DO NOT remove! this directive is a part of the test!!

#region ////////  Test Type Declarations  ////////
#if true && !false
#pragma warning disable CS1591 // Public declarations are syntax fixtures for visibility rewriting.

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

#pragma warning restore CS1591
#endif
#endregion

#endif
