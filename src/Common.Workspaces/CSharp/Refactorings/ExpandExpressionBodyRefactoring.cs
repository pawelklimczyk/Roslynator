// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Roslynator.CSharp.CSharpFactory;

namespace Roslynator.CSharp.Refactorings
{
    internal static class ExpandExpressionBodyRefactoring
    {
        public const string Title = "Expand expression body";

        public static async Task<Document> RefactorAsync(
            Document document,
            MemberDeclarationListSelection selectedMembers,
            CancellationToken cancellationToken)
        {
            SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            IEnumerable<MemberDeclarationSyntax> newMembers = selectedMembers
                .UnderlyingList
                .ModifyRange(
                    selectedMembers.FirstIndex,
                    selectedMembers.Count,
                    f =>
                    {
                        (SyntaxNode node, BlockSyntax body) = Refactor(CSharpUtility.GetExpressionBody(f), semanticModel, cancellationToken);

                        return (MemberDeclarationSyntax)node.WithFormatterAnnotation();
                    });

            return await document.ReplaceMembersAsync(SyntaxInfo.MemberDeclarationListInfo(selectedMembers.Parent), newMembers, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<Document> RefactorAsync(
            Document document,
            ArrowExpressionClauseSyntax expressionBody,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            (SyntaxNode newNode, BlockSyntax body) = Refactor(expressionBody, semanticModel, cancellationToken);

            newNode = newNode.WithFormatterAnnotation();

            return await document.ReplaceNodeAsync(expressionBody.Parent, newNode, cancellationToken).ConfigureAwait(false);
        }

        public static (SyntaxNode node, BlockSyntax body) Refactor(
            ArrowExpressionClauseSyntax expressionBody,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            SyntaxNode node = expressionBody.Parent;

            ExpressionSyntax expression = expressionBody.Expression;

            switch (node.Kind())
            {
                case SyntaxKind.MethodDeclaration:
                    {
                        var method = (MethodDeclarationSyntax)node;

                        BlockSyntax body = CreateBlock(method.ReturnType, expression, method.SemicolonToken, semanticModel, cancellationToken);

                        MethodDeclarationSyntax newMethod = method
                            .WithExpressionBody(null)
                            .WithSemicolonToken(default(SyntaxToken))
                            .WithBody(body);

                        return (newMethod, newMethod.Body);
                    }
                case SyntaxKind.ConstructorDeclaration:
                    {
                        var constructor = (ConstructorDeclarationSyntax)node;

                        BlockSyntax body = Block(ExpressionStatement(expression, constructor.SemicolonToken));

                        ConstructorDeclarationSyntax newConstructor = constructor
                            .WithExpressionBody(null)
                            .WithSemicolonToken(default(SyntaxToken))
                            .WithBody(body);

                        return (newConstructor, newConstructor.Body);
                    }
                case SyntaxKind.DestructorDeclaration:
                    {
                        var destructor = (DestructorDeclarationSyntax)node;

                        BlockSyntax body = Block(ExpressionStatement(expression, destructor.SemicolonToken));

                        DestructorDeclarationSyntax newDestructor = destructor
                            .WithExpressionBody(null)
                            .WithSemicolonToken(default(SyntaxToken))
                            .WithBody(body);

                        return (newDestructor, newDestructor.Body);
                    }
                case SyntaxKind.OperatorDeclaration:
                    {
                        var @operator = (OperatorDeclarationSyntax)node;

                        BlockSyntax body = CreateBlock(expression, @operator.SemicolonToken);

                        OperatorDeclarationSyntax newOperator = @operator
                            .WithExpressionBody(null)
                            .WithSemicolonToken(default(SyntaxToken))
                            .WithBody(body);

                        return (newOperator, newOperator.Body);
                    }
                case SyntaxKind.ConversionOperatorDeclaration:
                    {
                        var conversionOperator = (ConversionOperatorDeclarationSyntax)node;

                        BlockSyntax body = CreateBlock(expression, conversionOperator.SemicolonToken);

                        ConversionOperatorDeclarationSyntax newConversionOperator = conversionOperator
                            .WithExpressionBody(null)
                            .WithSemicolonToken(default(SyntaxToken))
                            .WithBody(body);

                        return (newConversionOperator, newConversionOperator.Body);
                    }
                case SyntaxKind.PropertyDeclaration:
                    {
                        var property = (PropertyDeclarationSyntax)node;

                        AccessorListSyntax accessorList = CreateAccessorList(expression, property.SemicolonToken);

                        PropertyDeclarationSyntax newProperty = property
                            .WithAccessorList(accessorList)
                            .WithExpressionBody(null)
                            .WithSemicolonToken(default(SyntaxToken));

                        return (newProperty, newProperty.AccessorList.Accessors[0].Body);
                    }
                case SyntaxKind.IndexerDeclaration:
                    {
                        var indexer = (IndexerDeclarationSyntax)node;

                        AccessorListSyntax accessorList = CreateAccessorList(expression, indexer.SemicolonToken);

                        IndexerDeclarationSyntax newIndexer = indexer
                            .WithAccessorList(accessorList)
                            .WithExpressionBody(null)
                            .WithSemicolonToken(default(SyntaxToken));

                        return (newIndexer, newIndexer.AccessorList.Accessors[0].Body);
                    }
                case SyntaxKind.GetAccessorDeclaration:
                    {
                        var accessor = (AccessorDeclarationSyntax)node;

                        BlockSyntax body = CreateBlock(expression, accessor.SemicolonToken);

                        AccessorDeclarationSyntax newAccessor = accessor
                            .WithExpressionBody(null)
                            .WithSemicolonToken(default(SyntaxToken))
                            .WithBody(body);

                        return (newAccessor, newAccessor.Body);
                    }
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                    {
                        var accessor = (AccessorDeclarationSyntax)node;

                        BlockSyntax body = Block(ExpressionStatement(expression, accessor.SemicolonToken));

                        AccessorDeclarationSyntax newAccessor = accessor
                            .WithExpressionBody(null)
                            .WithSemicolonToken(default(SyntaxToken))
                            .WithBody(body);

                        return (newAccessor, newAccessor.Body);
                    }
                case SyntaxKind.LocalFunctionStatement:
                    {
                        var localFunction = (LocalFunctionStatementSyntax)node;

                        BlockSyntax body = CreateBlock(localFunction.ReturnType, expression, localFunction.SemicolonToken, semanticModel, cancellationToken);

                        LocalFunctionStatementSyntax newLocalFunction = localFunction
                            .WithExpressionBody(null)
                            .WithSemicolonToken(default(SyntaxToken))
                            .WithBody(body);

                        return (newLocalFunction, newLocalFunction.Body);
                    }
                default:
                    {
                        throw new InvalidOperationException();
                    }
            }
        }

        private static BlockSyntax CreateBlock(ExpressionSyntax expression, SyntaxToken semicolon)
        {
            if (expression.Kind() == SyntaxKind.ThrowExpression)
            {
                return CreateBlockWithExpressionStatement(expression, semicolon);
            }
            else
            {
                return CreateBlockWithReturnStatement(expression, semicolon);
            }
        }

        private static BlockSyntax CreateBlock(
            TypeSyntax returnType,
            ExpressionSyntax expression,
            SyntaxToken semicolon,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (ShouldCreateExpressionStatement(returnType, expression, semanticModel, cancellationToken))
            {
                return CreateBlockWithExpressionStatement(expression, semicolon);
            }
            else
            {
                return CreateBlockWithReturnStatement(expression, semicolon);
            }
        }

        private static bool ShouldCreateExpressionStatement(
            TypeSyntax returnType,
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (returnType == null)
                return true;

            if (returnType.IsVoid())
                return true;

            SyntaxKind kind = expression.Kind();

            if (kind == SyntaxKind.ThrowExpression)
                return true;

            if (kind == SyntaxKind.AwaitExpression
                && !semanticModel
                    .GetTypeSymbol(returnType, cancellationToken)
                    .OriginalDefinition
                    .EqualsOrInheritsFrom(MetadataNames.System_Threading_Tasks_Task_T))
            {
                return true;
            }

            return false;
        }

        private static AccessorListSyntax CreateAccessorList(ExpressionSyntax expression, SyntaxToken semicolon)
        {
            BlockSyntax body = CreateBlock(expression, semicolon);

            AccessorListSyntax accessorList = AccessorList(GetAccessorDeclaration(body));

            if (expression.IsSingleLine())
            {
                accessorList = accessorList
                    .RemoveWhitespace()
                    .WithCloseBraceToken(accessorList.CloseBraceToken.WithLeadingTrivia(NewLine()));
            }

            return accessorList;
        }

        private static BlockSyntax CreateBlockWithExpressionStatement(ExpressionSyntax expression, SyntaxToken semicolon)
        {
            return Block(ExpressionStatement(expression, semicolon));
        }

        private static BlockSyntax CreateBlockWithReturnStatement(ExpressionSyntax expression, SyntaxToken semicolon)
        {
            ReturnStatementSyntax returnStatement = ReturnStatement(
                Token(SyntaxKind.ReturnKeyword).WithLeadingTrivia(expression.GetLeadingTrivia()),
                expression.WithoutLeadingTrivia(),
                semicolon);

            return Block(returnStatement);
        }
    }
}
