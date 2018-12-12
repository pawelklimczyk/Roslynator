// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Roslynator.CSharp.CSharpFactory;

namespace Roslynator.CSharp.Refactorings
{
    internal static class GenerateStructFromTupleRefactoring
    {
        public static async Task ComputeRefactoringAsync(RefactoringContext context, TupleTypeSyntax tupleType)
        {
            if (tupleType.ContainsDiagnostics)
                return;

            SyntaxNode parent = tupleType.Parent;

            TypeSyntax type = GetReturnType(parent);

            if (type != tupleType)
            {
                if (!(parent is TypeArgumentListSyntax typeArgumentList))
                    return;

                if (typeArgumentList.Arguments.SingleOrDefault(shouldThrow: false) != tupleType)
                    return;

                if (!(typeArgumentList.Parent is GenericNameSyntax genericName))
                    return;

                parent = genericName.Parent;

                type = GetReturnType(parent);

                if (type == null)
                    return;

                SemanticModel semanticModel = await context.GetSemanticModelAsync().ConfigureAwait(false);

                ITypeSymbol typeSymbol = semanticModel.GetTypeSymbol(genericName, context.CancellationToken)?.OriginalDefinition;

                if (typeSymbol == null)
                    return;

                if (!typeSymbol.HasMetadataName(MetadataNames.System_Collections_Generic_IEnumerable_T))
                {
                    if (!(parent is MethodDeclarationSyntax methodDeclaration)
                        || !methodDeclaration.Modifiers.Contains(SyntaxKind.AsyncKeyword)
                        || !typeSymbol.HasMetadataName(MetadataNames.System_Threading_Tasks_Task_T, MetadataNames.System_Threading_Tasks_ValueTask_T))
                    {
                        return;
                    }
                }
            }

            SyntaxNode node = parent.Parent;

            if (node.IsKind(SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration))
            {
                var containingMember = (MemberDeclarationSyntax)parent;
                var containingType = (TypeDeclarationSyntax)node;

                context.RegisterRefactoring(
                    "Generate nested struct from tuple",
                    ct => RefactorAsync(context.Document, tupleType, containingMember, containingType, null, ct),
                    EquivalenceKey.Join(RefactoringIdentifiers.GenerateStructFromTuple, "Nested"));

                node = node.Parent;

                while (node != null)
                {
                    if (node.IsKind(SyntaxKind.NamespaceDeclaration, SyntaxKind.CompilationUnit))
                    {
                        SyntaxNode namespaceOrCompilationUnit = node;

                        context.RegisterRefactoring(
                            "Generate struct from tuple",
                            ct => RefactorAsync(context.Document, tupleType, containingMember, containingType, namespaceOrCompilationUnit, ct),
                            RefactoringIdentifiers.GenerateStructFromTuple);
                    }
                    else if (!node.IsKind(SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration))
                    {
                        return;
                    }

                    node = node.Parent;
                }
            }
        }

        private static TypeSyntax GetReturnType(SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.DelegateDeclaration:
                    return ((DelegateDeclarationSyntax)node).ReturnType;
                case SyntaxKind.IndexerDeclaration:
                    return ((IndexerDeclarationSyntax)node).Type;
                case SyntaxKind.MethodDeclaration:
                    return ((MethodDeclarationSyntax)node).ReturnType;
                case SyntaxKind.PropertyDeclaration:
                    return ((PropertyDeclarationSyntax)node).Type;
            }

            return null;
        }

        private static async Task<Document> RefactorAsync(
            Document document,
            TupleTypeSyntax tupleType,
            MemberDeclarationSyntax containingMember,
            TypeDeclarationSyntax containingType,
            SyntaxNode namespaceOrCompilationUnit,
            CancellationToken cancellationToken)
        {
            SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            string structName = GetStructName();

            var tupleSymbol = (INamedTypeSymbol)semanticModel.GetSymbol(tupleType, cancellationToken);

            SyntaxTokenList modifiers = (namespaceOrCompilationUnit != null) ? Modifiers.Internal() : Modifiers.Private();

            StructDeclarationSyntax structDeclaration = GenerateStructFromTupleType(tupleSymbol, modifiers, structName).WithFormatterAnnotation();

            var rewriter = new GenerateStructFromTupleRewriter(IdentifierName(structName), tupleType, tupleSymbol, semanticModel, cancellationToken);

            SyntaxNode newContainingMember = rewriter.Visit(containingMember);

            MemberDeclarationInserter inserter = MemberDeclarationInserter.Default;

            if (namespaceOrCompilationUnit != null)
            {
                SyntaxNode newNode = namespaceOrCompilationUnit.ReplaceNode(containingMember, newContainingMember);

                newNode = (newNode.IsKind(SyntaxKind.NamespaceDeclaration))
                    ? inserter.Insert((NamespaceDeclarationSyntax)newNode, structDeclaration)
                    : (SyntaxNode)inserter.Insert((CompilationUnitSyntax)newNode, structDeclaration);

                return await document.ReplaceNodeAsync(namespaceOrCompilationUnit, newNode, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                TypeDeclarationSyntax newNode = containingType.ReplaceNode(containingMember, newContainingMember);

                newNode = inserter.Insert(newNode, structDeclaration);

                return await document.ReplaceNodeAsync(containingType, newNode, cancellationToken).ConfigureAwait(false);
            }

            string GetStructName()
            {
                int position;

                if (namespaceOrCompilationUnit == null)
                {
                    position = containingType.OpenBraceToken.Span.End;
                }
                else if (namespaceOrCompilationUnit.IsKind(SyntaxKind.NamespaceDeclaration))
                {
                    position = ((NamespaceDeclarationSyntax)namespaceOrCompilationUnit).OpenBraceToken.Span.End;
                }
                else
                {
                    position = ((CompilationUnitSyntax)namespaceOrCompilationUnit).Members.First().SpanStart;
                }

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

        private static StructDeclarationSyntax GenerateStructFromTupleType(INamedTypeSymbol tupleSymbol, SyntaxTokenList modifiers, string structName)
        {
            return StructDeclaration(
                modifiers,
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

        private class GenerateStructFromTupleRewriter : CSharpSyntaxRewriter
        {
            private readonly IdentifierNameSyntax _structName;
            private readonly TupleTypeSyntax _tupleType;
            private readonly INamedTypeSymbol _tupleSymbol;
            private readonly SemanticModel _semanticModel;
            private readonly CancellationToken _cancellationToken;

            public GenerateStructFromTupleRewriter(IdentifierNameSyntax structName, TupleTypeSyntax tupleType, INamedTypeSymbol tupleSymbol, SemanticModel semanticModel, CancellationToken cancellationToken)
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
