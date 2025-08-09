// Licensed under the MIT License
// https://github.com/sator-imaging/DotnetTool-StaticImport

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SatorImaging.DotnetTool.StaticImport.Core;

#pragma warning disable IDE0290  // Use primary constructor

internal class TypeMigrator : IContentTransformer
{
    public string Transform(string content, string? newNamespace, bool makeTypeInternal)
    {
        NamespaceRewriter? namespaceRewriter = null;
        if (!string.IsNullOrWhiteSpace(newNamespace))
        {
            namespaceRewriter = new NamespaceRewriter(newNamespace);
        }

        TypeModifierRewriter? visibilityRewriter = null;
        if (makeTypeInternal)
        {
            visibilityRewriter = new TypeModifierRewriter(SyntaxKind.PublicKeyword, SyntaxKind.InternalKeyword);
        }

        // when preprocessor symbol is used in C# source code, all combinations must be enabled to process file correctly.
        // --> C# parser will skip creating syntax tree enclosed by directive that condition is not met.
        foreach (var symbols in ConditionalDirectiveTree.Parse(content).ToSymbolCombinations())
        {
            var options = symbols.Count == 0
                ? CSharpParseOptions.Default
                : CSharpParseOptions.Default.WithPreprocessorSymbols(symbols)
                ;

            var tree = CSharpSyntaxTree.ParseText(content, options);
            var root = tree.GetCompilationUnitRoot();

            Console.WriteLine($"# {(symbols.Count == 0 ? "No Symbol" : "Symbols: " + string.Join(", ", symbols))}");
            content = rewrite(root, namespaceRewriter, visibilityRewriter);
        }

        return content;

        //nest
        static string rewrite(SyntaxNode root, NamespaceRewriter? namespaceRewriter, TypeModifierRewriter? visibilityRewriter)
        {
            const string PREFIX = "\n- ";

            if (namespaceRewriter != null)
            {
                root = namespaceRewriter.Visit(root);

                var changes = namespaceRewriter.ChangeLog;
                if (changes.Count > 0)
                {
                    Console.WriteLine($"## {nameof(NamespaceRewriter)}: {changes.Count} change(s){PREFIX}{string.Join(PREFIX, changes)}");
                    changes.Clear();
                }
            }

            if (visibilityRewriter != null)
            {
                root = visibilityRewriter.Visit(root);

                var changes = visibilityRewriter.ChangeLog;
                if (changes.Count > 0)
                {
                    Console.WriteLine($"## {nameof(TypeModifierRewriter)}: {changes.Count} change(s){PREFIX}{string.Join(PREFIX, changes)}");
                    changes.Clear();
                }
            }

            return root.ToFullString();
        }
    }


    internal class TypeModifierRewriter : CSharpSyntaxRewriter
    {
        public readonly List<string> ChangeLog = new(SR.DefaultListCapacity);

        readonly SyntaxKind targetVisibility;
        readonly SyntaxKind newVisibility;

        public TypeModifierRewriter(SyntaxKind targetVisibility, SyntaxKind newVisibility)
        {
            this.targetVisibility = targetVisibility;
            this.newVisibility = newVisibility;
        }

        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax n) => Visit(n) ?? base.VisitClassDeclaration(n);
        public override SyntaxNode? VisitEnumDeclaration(EnumDeclarationSyntax n) => Visit(n) ?? base.VisitEnumDeclaration(n);
        public override SyntaxNode? VisitInterfaceDeclaration(InterfaceDeclarationSyntax n) => Visit(n) ?? base.VisitInterfaceDeclaration(n);
        public override SyntaxNode? VisitRecordDeclaration(RecordDeclarationSyntax n) => Visit(n) ?? base.VisitRecordDeclaration(n);
        public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax n) => Visit(n) ?? base.VisitStructDeclaration(n);

        BaseTypeDeclarationSyntax? Visit(BaseTypeDeclarationSyntax node)
        {
            // don't change nested type visibility!!
            if (node.Ancestors().OfType<BaseTypeDeclarationSyntax>().Any())
            {
                return null;
            }

            var targetModifier = node.Modifiers.FirstOrDefault(m => m.IsKind(targetVisibility));
            if (targetModifier == default)
            {
                return null;
            }

            ChangeLog.Add(node.Identifier.ToString());

            var internalModifier = SyntaxFactory.Token(newVisibility)
                .WithLeadingTrivia(targetModifier.LeadingTrivia)
                .WithTrailingTrivia(targetModifier.TrailingTrivia);

            var newModifiers = node.Modifiers.Replace(targetModifier, internalModifier);

            return node.WithModifiers(newModifiers);
        }
    }


    internal class NamespaceRewriter : CSharpSyntaxRewriter
    {
        public readonly List<(string from, string to)> ChangeLog = new(SR.DefaultListCapacity);

        readonly string newNamespace;
        readonly bool isPrependMode;

        public NamespaceRewriter(string newNamespace)
        {
            this.newNamespace = newNamespace;
            this.isPrependMode = newNamespace.EndsWith('.');
        }

        public override SyntaxNode? VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            // ignore nested declaration
            if (node.Ancestors().OfType<NamespaceDeclarationSyntax>().Any())
            {
                return base.VisitNamespaceDeclaration(node);
            }

            var oldNamespace = node.Name.ToString();
            var newNamespace = this.newNamespace;

            if (isPrependMode)
            {
                if (oldNamespace.StartsWith(newNamespace, StringComparison.Ordinal))
                {
                    return base.VisitNamespaceDeclaration(node);
                }

                newNamespace += oldNamespace;
            }
            else
            {
                if (oldNamespace == newNamespace)
                {
                    return base.VisitNamespaceDeclaration(node);
                }
            }

            ChangeLog.Add((oldNamespace, newNamespace));

            var nameStx = SyntaxFactory.IdentifierName(newNamespace)
                .WithLeadingTrivia(node.Name.GetLeadingTrivia())
                .WithTrailingTrivia(node.Name.GetTrailingTrivia())
                ;

            return node.WithName(nameStx);
        }

        public override SyntaxNode? VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
        {
            var oldNamespace = node.Name.ToString();
            var newNamespace = this.newNamespace;

            if (isPrependMode)
            {
                if (oldNamespace.StartsWith(newNamespace, StringComparison.Ordinal))
                {
                    return base.VisitFileScopedNamespaceDeclaration(node);
                }

                newNamespace += oldNamespace;
            }
            else
            {
                if (oldNamespace == newNamespace)
                {
                    return base.VisitFileScopedNamespaceDeclaration(node);
                }
            }

            ChangeLog.Add((oldNamespace, newNamespace));

            var nameStx = SyntaxFactory.IdentifierName(newNamespace)
                .WithLeadingTrivia(node.Name.GetLeadingTrivia())
                .WithTrailingTrivia(node.Name.GetTrailingTrivia())
                ;

            return node.WithName(nameStx);
        }
    }
}
