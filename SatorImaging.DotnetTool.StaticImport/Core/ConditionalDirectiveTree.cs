// Licensed under the MIT License
// https://github.com/sator-imaging/DotnetTool-StaticImport

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;

namespace SatorImaging.DotnetTool.StaticImport.Core;

internal class ConditionalDirectiveTree
{
    public sealed class Node
    {
        public Node? Parent;
        public List<Node> Children = new(SR.DefaultListCapacity);

        public List<string> Symbols = new(SR.DefaultListCapacity);

        public override string ToString()
        {
            if (Parent == null)
                return "[ROOT]";

            if (Symbols.Count == 0)
                return "[EMPTY]";

            return $"{string.Join(",", Symbols)} --> {Parent}";
        }

        public List<List<string>> ToSymbolCombinations()
        {
            if (Parent != null)
                throw new InvalidOperationException("node is not root: " + this);

            return BuildSymbolCombinations(this);
        }
    }


    public static Node Parse(string sourceCode)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = tree.GetRoot();

        var directiveSyntaxes =
            root.DescendantTrivia()
                .Select(x => x.GetStructure() as DirectiveTriviaSyntax)
                .Where(x => x != null)
                .Cast<DirectiveTriviaSyntax>()
                ;

        var rootNode = new Node();
        var parentStack = new Stack<Node>();

        foreach (var directiveStx in directiveSyntaxes)
        {
            if (directiveStx.Kind() is SyntaxKind.IfDirectiveTrivia or SyntaxKind.ElifDirectiveTrivia)
            {
                if (directiveStx is ConditionalDirectiveTriviaSyntax conditionalStx &&
                    !conditionalStx.ConditionValue)
                {
                    if (!directiveStx.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any())
                    {
                        throw new Exception("There is a preprocessor directive which will have never been met: " + directiveStx);
                    }
                }

                if (directiveStx.IsKind(SyntaxKind.ElifDirectiveTrivia))
                {
                    exitCurrentParent(parentStack);
                }

                var parent = (parentStack.Count > 0) ? parentStack.Peek() : rootNode;

                Console.WriteDebugOnlyLine($"{(directiveStx.IsKind(SyntaxKind.IfDirectiveTrivia) ? "IF:" : "ELIF:"),-7}{directiveStx} (parent: {parent})");

                var node = new Node()
                {
                    Parent = parent,
                    Symbols = directiveStx.DescendantNodesAndSelf()
                                          .OfType<IdentifierNameSyntax>()
                                          .Select(x => x.Identifier.ValueText)
                                          .Where(x => !string.IsNullOrWhiteSpace(x))
                                          .OrderBy(x => x)
                                          .ToList(),
                };

                if (node.Symbols.Count == 0)
                {
                    Console.WriteWarning($"symbol not found: " + directiveStx);
                }

                parent.Children.Add(node);

                parentStack.Push(node);
            }
            else if (directiveStx.IsKind(SyntaxKind.EndIfDirectiveTrivia))
            {
                Console.WriteDebugOnlyLine("ENDIF: " + directiveStx.ToString());

                exitCurrentParent(parentStack);
            }

            //nest
            static void exitCurrentParent(Stack<Node> stack)
            {
                if (!stack.TryPop(out _))
                    throw new FormatException("#if/#elif and #endif pair is not correct");
            }
        }

        return rootNode;
    }


    static List<List<string>> BuildSymbolCombinations(Node rootNode)
    {
        var allNodes = new List<Node>(SR.DefaultListCapacity * 2);
        {
            getNodes(rootNode, allNodes);

            static void getNodes(Node node, List<Node> symbols)
            {
                symbols.Add(node);

                foreach (var child in node.Children)
                {
                    getNodes(child, symbols);
                }
            }
        }

        var result = new List<List<string>>(collection: [[]]);  // always insert empty!

        // all possible symbol combinations can be built from tip node.
        foreach (var tip in allNodes.Where(x => x.Children.Count == 0))
        {
            impl(tip, result);
        }

        Debug.Assert(result.First().Count == 0);
        Debug.Assert(result.Skip(1).All(x => x.Count != 0));
        Debug.Assert(result.Count == result.Distinct(new UnorderedListStringComparer()).Count());

        return result;


        /* =====  impl  ===== */

        // recall, max possible combinations of 8 elements is 256.
        // that is the same as combinations of bit flags, 0b_0000_0000 to 0b_1111_1111.
        // --> treat element index as bit position to build all possible combinations.
        static void impl(Node tipNode, List<List<string>> result)
        {
            var symbolList = new List<string>(SR.DefaultListCapacity * 2);
            {
                var node = tipNode;
                do
                {
                    symbolList.AddRange(node.Symbols.Except(symbolList));
                    node = node.Parent;
                }
                while (node != null);
            }

            if (symbolList.Count == 0)
            {
                return;
            }

            // cannot use sizeof(long) due to array size limit.
            const int MAX_ITEMS = (sizeof(int) * 8) - 1;  // cannot use most significant bit!!

            if (symbolList.Count > MAX_ITEMS)
            {
                throw new IndexOutOfRangeException($"max preprocessor symbol count is '{MAX_ITEMS}'");
            }

            var symbolsSpan = CollectionsMarshal.AsSpan(symbolList);
            var unorderedComparer = new UnorderedListStringComparer();

            int comboCount = (int)Math.Pow(2, symbolsSpan.Length);
            result.Capacity += comboCount;

            for (int i = 1; i < comboCount; i++)  // exclude 0
            {
                var list = new List<string>(symbolsSpan.Length);

                for (int bitPosition = 0; bitPosition < symbolsSpan.Length; bitPosition++)
                {
                    if (((i >> bitPosition) & 1) == 1)
                    {
                        list.Add(symbolsSpan[bitPosition]);
                    }
                }

                if (!result.Contains(list, unorderedComparer))
                {
                    result.Add(list);
                }
            }
        }
    }


    readonly struct UnorderedListStringComparer : IEqualityComparer<List<string>>
    {
        public int GetHashCode([DisallowNull] List<string> obj) => obj.Sum(x => x.Length);

        public bool Equals(List<string>? left, List<string>? right)
        {
            if (left == null || right == null)
            {
                return (left == right);
            }

            if (left.Count != right.Count)
            {
                return false;
            }

            for (int i = 0, count = left.Count; i < count; i++) //perf
            {
                var item = left[i];

                if (!right.Contains(item))
                {
                    return false;
                }
            }

            return true;
        }
    }


    // this method doesn't build directive hierarchy. just for reference.
    internal static IEnumerable<string> GetReferencedPreprocessorSymbols(SyntaxTree syntaxTree)
    {
        // https://gist.github.com/terrajobst/5d7d48da5af0a4be891d381fd7b2e5ed
        return syntaxTree.GetRoot()
                         .DescendantTrivia()
                         .Where(t => t.IsKind(SyntaxKind.IfDirectiveTrivia) ||
                                     t.IsKind(SyntaxKind.ElifDirectiveTrivia))
                         .Select(t => t.GetStructure())
                         .Cast<ConditionalDirectiveTriviaSyntax>()
                         .SelectMany(c => c.Condition.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
                         .Select(i => i.Identifier.ValueText)
                         .Distinct()
                         .OrderBy(i => i);
    }
}
