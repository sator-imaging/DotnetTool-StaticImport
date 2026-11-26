// Licensed under the MIT License
// https://github.com/sator-imaging/DotnetTool-StaticImport

using Microsoft.CodeAnalysis.CSharp;
using System.Diagnostics;
using System.Text;

namespace SatorImaging.DotnetTool.StaticImport.Core
{
    public class TypeMigratorTests
    {
        [Test]
        public void TypeMigratorTest()
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
            var sourceCodeBytes = Encoding.UTF8.GetBytes(sourceCode);

            _ = new TypeMigrator(null, false).Transform(sourceCodeBytes);
            _ = new TypeMigrator(null, true).Transform(sourceCodeBytes);
            _ = new TypeMigrator("ReplacedNamespace", false).Transform(sourceCodeBytes);
            _ = new TypeMigrator("PrefixMode.", true).Transform(sourceCodeBytes);
        }

        [Test]
        public void RewriterTest()
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
    }
}
