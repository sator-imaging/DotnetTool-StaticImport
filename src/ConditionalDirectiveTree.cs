// Licensed under the MIT License
// https://github.com/sator-imaging/DotnetTool-StaticImport

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace SatorImaging.DotnetTool.StaticImport;

/// <summary>
/// Parses conditional compilation directives and builds their symbol combinations.
/// </summary>
public static class ConditionalDirectiveTree
{
    /// <summary>
    /// A node in a parsed conditional directive tree.
    /// </summary>
    public sealed class Node
    {
        /// <summary>
        /// The parent node, or <see langword="null"/> for the root.
        /// </summary>
        public Node? Parent;

        /// <summary>
        /// The child nodes in directive order.
        /// </summary>
        public readonly List<Node> Children = new(capacity: 0); // 0 (cached) -> 4 -> 8

        /// <summary>
        /// The preprocessor symbols associated with this node.
        /// </summary>
        public readonly List<string> Symbols = new(capacity: 0); // 0 (cached) -> 4 -> 8

        /// <summary>
        /// Returns a string representation of this node and its ancestors.
        /// </summary>
        public override string ToString()
        {
            if (Parent == null)
                return "[ROOT]";

            if (Symbols.Count == 0)
                return "[EMPTY]";

            return $"{string.Join(",", Symbols)} --> {Parent}";
        }

        /// <summary>
        /// Builds all non-empty symbol combinations represented by this root node.
        /// </summary>
        /// <exception cref="InvalidOperationException">This node is not the root.</exception>
        public List<List<string>> ToSymbolCombinations()
        {
            if (Parent != null)
                throw new InvalidOperationException("node is not root: " + this);

            return BuildSymbolCombinations(this);
        }
    }

    /// <summary>
    /// Parses conditional compilation directives from C# source code.
    /// </summary>
    /// <param name="sourceCode">The source code to parse.</param>
    /// <returns>Root node of parsed conditional directive tree.</returns>
    public static Node Parse(string sourceCode)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = tree.GetRoot();

        var rootNode = new Node();
        var parentStack = new Stack<Node>();

        foreach (var trivia in root.DescendantTrivia())
        {
            if (trivia.GetStructure() is not DirectiveTriviaSyntax directiveStx)
                continue;

            if (directiveStx.Kind() is SyntaxKind.IfDirectiveTrivia or SyntaxKind.ElifDirectiveTrivia)
            {
                if (directiveStx is ConditionalDirectiveTriviaSyntax conditionalStx &&
                    !conditionalStx.ConditionValue)
                {
                    var hasIdentifier = false;
                    foreach (var syntax in directiveStx.DescendantNodesAndSelf())
                    {
                        if (syntax is IdentifierNameSyntax)
                        {
                            hasIdentifier = true;
                            break;
                        }
                    }

                    if (!hasIdentifier)
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
                };

                foreach (var syntax in directiveStx.DescendantNodesAndSelf())
                {
                    if (syntax is IdentifierNameSyntax identifier)
                    {
                        node.Symbols.Add(identifier.Identifier.ValueText);
                    }
                }

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
        // NOTE: always insert an empty as the first item!!
        var result = new List<List<string>>(collection: [[]]);
        var combinations = new HashSet<List<string>>(UnorderedListStringComparer.Default)
        {
            result[0],
        };

        visit(rootNode, result, combinations);

        Debug.Assert(result[0].Count == 0);
        Debug.Assert(System.Linq.Enumerable.All(System.Linq.Enumerable.Skip(result, 1), x => x.Count != 0));
        Debug.Assert(result.Count == System.Linq.Enumerable.Count(System.Linq.Enumerable.Distinct(result, UnorderedListStringComparer.Default)));

        return result;


        /* =====  impl  ===== */

        static void visit(Node node, List<List<string>> result, HashSet<List<string>> combinations)
        {
            // all possible symbol combinations can be built from tip node.
            if (node.Children.Count == 0)
            {
                impl(node, result, combinations);
                return;
            }
            foreach (var child in node.Children)
            {
                visit(child, result, combinations);
            }
        }

        // recall, max possible combinations of 8 elements is 256.
        // that is the same as combinations of bit flags, 0b_0000_0000 to 0b_1111_1111.
        // --> treat element index as bit position to build all possible combinations.
        static void impl(Node tipNode, List<List<string>> result, HashSet<List<string>> combinations)
        {
            const int BitMask = 1;
            var symbolList = new List<string>(SR.DefaultListCapacity);
            {
                var node = tipNode;
                do
                {
                    foreach (var symbol in node.Symbols)
                    {
                        if (!symbolList.Contains(symbol))
                        {
                            symbolList.Add(symbol);
                        }
                    }
                    node = node.Parent;
                }
                while (node != null);
            }

            if (symbolList.Count == 0)
            {
                return;
            }

            // cannot use sizeof(long) due to array size limit.
            const int MAX_ITEMS = (sizeof(int) * 8) - 1;  // don't use sign bit. max array size is about int.MaxValue.

            if (symbolList.Count > MAX_ITEMS)
            {
                throw new IndexOutOfRangeException($"max preprocessor symbol count is '{MAX_ITEMS}'");
            }

            var symbolsSpan = CollectionsMarshal.AsSpan(symbolList);

            int comboCount = (int)Math.Pow(2, symbolsSpan.Length);
            result.Capacity = result.Count + (comboCount - 1);

            for (int i = 1; i < comboCount; i++)  // exclude 0
            {
                var list = new List<string>(symbolsSpan.Length);

                for (int bitPosition = 0; bitPosition < symbolsSpan.Length; bitPosition++)
                {
                    if (((i >> bitPosition) & BitMask) != 0)
                    {
                        list.Add(symbolsSpan[bitPosition]);
                    }
                }

                if (combinations.Add(list))
                {
                    result.Add(list);
                }
            }
        }
    }

    sealed class UnorderedListStringComparer : IEqualityComparer<List<string>>
    {
        public static readonly UnorderedListStringComparer Default = new();

        private UnorderedListStringComparer() { }

        public int GetHashCode([DisallowNull] List<string> obj)
        {
            int hashCode = 0;
            foreach (var item in obj)
            {
                hashCode += item.Length;
            }
            return hashCode;
        }

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
}
