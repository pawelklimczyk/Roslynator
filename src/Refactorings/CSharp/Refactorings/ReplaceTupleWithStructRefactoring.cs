// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Roslynator.CSharp.CSharpFactory;
using System.Collections.Immutable;

namespace Roslynator.CSharp.Refactorings
{
    internal static class ReplaceTupleWithStructRefactoring
    {
        public static void ComputeRefactoring(RefactoringContext context, TupleTypeSyntax tupleType)
        {
            if (tupleType.ContainsDiagnostics)
                return;

            SyntaxNode parent = tupleType.Parent;

            TypeSyntax type = GetReturnType();

            if (type != tupleType)
                return;

            if (!parent.IsParentKind(SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration))
                return;

            Debug.Assert(parent.Parent.FirstAncestor(SyntaxKind.NamespaceDeclaration, SyntaxKind.CompilationUnit) != null);

            if (parent.Parent.FirstAncestor(SyntaxKind.NamespaceDeclaration, SyntaxKind.CompilationUnit) == null)
                return;

            context.RegisterRefactoring(
                "Replace tuple with struct",
                ct => RefactorAsync(context.Document, tupleType, ct),
                RefactoringIdentifiers.ReplaceTupleWithStruct);

            TypeSyntax GetReturnType()
            {
                switch (parent.Kind())
                {
                    case SyntaxKind.DelegateDeclaration:
                        return ((DelegateDeclarationSyntax)parent).ReturnType;
                    case SyntaxKind.IndexerDeclaration:
                        return ((IndexerDeclarationSyntax)parent).Type;
                    case SyntaxKind.MethodDeclaration:
                        return ((MethodDeclarationSyntax)parent).ReturnType;
                    case SyntaxKind.PropertyDeclaration:
                        return ((PropertyDeclarationSyntax)parent).Type;
                    default:
                        return null;
                }
            }
        }

        private static async Task<Document> RefactorAsync(
            Document document,
            TupleTypeSyntax tupleType,
            CancellationToken cancellationToken)
        {
            var containingMember = (MemberDeclarationSyntax)tupleType.Parent;

            var containingType = (TypeDeclarationSyntax)containingMember.Parent;

            SyntaxNode namespaceOrCompilationUnit = containingType.FirstAncestor(SyntaxKind.NamespaceDeclaration, SyntaxKind.CompilationUnit);

            SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            string structName = GetStructName();

            var tupleSymbol = (INamedTypeSymbol)semanticModel.GetSymbol(tupleType, cancellationToken);

            StructDeclarationSyntax structDeclaration = GenerateStructFromTupleType(structName, tupleSymbol).WithFormatterAnnotation();

            var rewriter = new ReplaceTupleWithStructRewriter(IdentifierName(structName), tupleType, tupleSymbol, semanticModel, cancellationToken);

            SyntaxNode newContainingMember = rewriter.Visit(containingMember);

            MemberDeclarationInserter inserter = MemberDeclarationInserter.Default;

            SyntaxNode newNode = namespaceOrCompilationUnit.ReplaceNode(containingMember, newContainingMember);

            newNode = (newNode.IsKind(SyntaxKind.NamespaceDeclaration))
                ? inserter.Insert((NamespaceDeclarationSyntax)newNode, structDeclaration)
                : (SyntaxNode)inserter.Insert((CompilationUnitSyntax)newNode, structDeclaration);

            return await document.ReplaceNodeAsync(namespaceOrCompilationUnit, newNode, cancellationToken).ConfigureAwait(false);

            string GetStructName()
            {
                int position = (namespaceOrCompilationUnit.IsKind(SyntaxKind.NamespaceDeclaration))
                    ? ((NamespaceDeclarationSyntax)namespaceOrCompilationUnit).OpenBraceToken.Span.End
                    : ((CompilationUnitSyntax)namespaceOrCompilationUnit).Members.First().SpanStart;

                const string name = DefaultNames.Struct;

                if (!semanticModel.LookupNamespacesAndTypes(containingMember.SpanStart, name: name).Any()
                    && !semanticModel.LookupNamespacesAndTypes(position, name: name).Any())
                {
                    return name;
                }

                ImmutableArray<ISymbol> symbols = semanticModel
                    .LookupNamespacesAndTypes(containingMember.SpanStart)
                    .AddRange(semanticModel.LookupNamespacesAndTypes(position, name: name));

                return NameGenerator.Default.EnsureUniqueName(name, symbols);
            }
        }

        private static StructDeclarationSyntax GenerateStructFromTupleType(string structName, INamedTypeSymbol tupleSymbol)
        {
            return StructDeclaration(
                Modifiers.Public(),
                Identifier(structName).WithRenameAnnotation(),
                GenerateMembers().ToSyntaxList());

            IEnumerable<MemberDeclarationSyntax> GenerateMembers()
            {
                ImmutableArray<IFieldSymbol> tupleElements = tupleSymbol.TupleElements;

                yield return ConstructorDeclaration(
                    Modifiers.Public(),
                    Identifier(structName),
                    ParameterList(tupleElements.Select(f => Parameter(f.Type.ToTypeSyntax().WithSimplifierAnnotation(), f.Name)).ToSeparatedSyntaxList()),
                    Block(tupleElements.Select(f => SimpleAssignmentStatement(IdentifierName(StringUtility.FirstCharToUpperInvariant(f.Name)), IdentifierName(f.Name)))));

                foreach (IFieldSymbol tupleElement in tupleElements)
                {
                    yield return PropertyDeclaration(
                        Modifiers.Public(),
                        tupleElement.Type.ToTypeSyntax().WithSimplifierAnnotation(),
                        Identifier(StringUtility.FirstCharToUpperInvariant(tupleElement.Name)),
                        AccessorList(AutoGetAccessorDeclaration()));
                }
            }
        }

        private class ReplaceTupleWithStructRewriter : CSharpSyntaxRewriter
        {
            private readonly IdentifierNameSyntax _structName;
            private readonly TupleTypeSyntax _tupleType;
            private readonly INamedTypeSymbol _tupleSymbol;
            private readonly SemanticModel _semanticModel;
            private readonly CancellationToken _cancellationToken;

            public ReplaceTupleWithStructRewriter(IdentifierNameSyntax structName, TupleTypeSyntax tupleType, INamedTypeSymbol tupleSymbol, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                _structName = structName;
                _tupleType = tupleType;
                _tupleSymbol = tupleSymbol;
                _semanticModel = semanticModel;
                _cancellationToken = cancellationToken;
            }

            public override SyntaxNode VisitTupleType(TupleTypeSyntax node)
            {
                if (node == _tupleType)
                {
                    return _structName.WithTriviaFrom(node);
                }
                else if (node.IsParentKind(SyntaxKind.DefaultExpression))
                {
                    var symbol = (INamedTypeSymbol)_semanticModel.GetSymbol(node, _cancellationToken);

                    if (TupleTypeEquals(symbol))
                    {
                        return _structName.WithTriviaFrom(node);
                    }
                }

                return base.VisitTupleType(node);
            }

            public override SyntaxNode VisitTupleExpression(TupleExpressionSyntax node)
            {
                INamedTypeSymbol symbol2 = _semanticModel.GetDeclaredSymbol(node, _cancellationToken);

                if (TupleTypeEquals(symbol2))
                {
                    return ObjectCreationExpression(_structName, ArgumentList(node.Arguments)).WithTriviaFrom(node);
                }

                return base.VisitTupleExpression(node);
            }

            private bool TupleTypeEquals(INamedTypeSymbol tupleSymbol)
            {
                return _tupleSymbol.TupleElements.Select(f => f.Type).SequenceEqual(tupleSymbol.TupleElements.Select(f => f.Type));
            }
        }
    }
}
