// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslynator.CSharp.Syntax;
using Roslynator.CSharp.SyntaxRewriters;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Roslynator.CSharp.CSharpFactory;

namespace Roslynator.CSharp.Refactorings.ExpandLinqMethodOperation
{
    internal static class ExpandLinqMethodOperationRefactoring
    {
        internal static void ComputeRefactorings(
            RefactoringContext context,
            in SimpleMemberInvocationExpressionInfo invocationInfo,
            SemanticModel semanticModel)
        {
            string methodName = invocationInfo.NameText;

            switch (methodName)
            {
                case "Any":
                case "All":
                case "FirstOrDefault":
                    {
                        ExpressionSyntax argumentExpression = invocationInfo.Arguments.SingleOrDefault(shouldThrow: false)?.Expression?.WalkDownParentheses();

                        if (argumentExpression == null)
                            return;

                        InvocationExpressionSyntax invocationExpression = invocationInfo.InvocationExpression;

                        if (invocationExpression.IsParentKind(SyntaxKind.ConditionalAccessExpression))
                            return;

                        SyntaxNode containingBody = FindContainingBodyOrExpressionBody(invocationExpression);

                        if (containingBody == null)
                            return;

                        ExtensionMethodSymbolInfo extensionMethodSymbolInfo = semanticModel.GetReducedExtensionMethodInfo(invocationExpression, context.CancellationToken);

                        IMethodSymbol reducedSymbol = extensionMethodSymbolInfo.ReducedSymbol;

                        if (reducedSymbol == null)
                            return;

                        ITypeSymbol typeArgument = reducedSymbol.TypeArguments[0];

                        bool? supportsExplicitDeclaration = null;

                        if (methodName == "FirstOrDefault")
                        {
                            supportsExplicitDeclaration = typeArgument.SupportsExplicitDeclaration();

                            if (supportsExplicitDeclaration == false)
                                return;
                        }

                        if (!SymbolUtility.IsLinqExtensionOfIEnumerableOfTWithPredicate(extensionMethodSymbolInfo.Symbol, methodName, allowImmutableArrayExtension: true))
                            return;

                        AnonymousFunctionAnalysis analysis = AnonymousFunctionAnalysis.Create(argumentExpression, semanticModel);

                        if (analysis.Body.IsKind(SyntaxKind.Block))
                        {
                            supportsExplicitDeclaration = supportsExplicitDeclaration ?? typeArgument.SupportsExplicitDeclaration();

                            if (supportsExplicitDeclaration == false)
                                return;
                        }

                        if (analysis.Captured.IsDefaultOrEmpty)
                            return;

                        if (!analysis.Body.IsKind(SyntaxKind.Block)
                            && analysis.IsLambda
                            && invocationExpression.WalkUpParentheses().IsParentKind(SyntaxKind.ReturnStatement, SyntaxKind.ArrowExpressionClause))
                        {
                            context.RegisterRefactoring(
                                $"Replace '{methodName}' with foreach",
                                ct => ReplaceLinqWithForEachAsync(context.Document, invocationExpression, reducedSymbol, semanticModel, ct),
                                RefactoringIdentifiers.ExpandLinqMethodOperation);
                        }
                        else
                        {
                            context.RegisterRefactoring(
                                $"Extract '{methodName}' to local function",
                                ct => CreateRefactoring(invocationExpression, analysis.Body, containingBody, typeArgument, analysis.Captured).RefactorAsync(ct),
                                RefactoringIdentifiers.ExpandLinqMethodOperation);
                        }

                        break;
                    }
                case "Select":
                case "Where":
                    {
                        InvocationExpressionSyntax invocationExpression = invocationInfo.InvocationExpression;

                        ExpressionSyntax argumentExpression = invocationExpression.ArgumentList.Arguments.SingleOrDefault(shouldThrow: false)?.Expression;

                        if (argumentExpression == null)
                            return;

                        if (!CSharpFacts.IsForEachExpression(invocationExpression.WalkUpParentheses()))
                            return;

                        if (!(SyntaxInfo.LambdaExpressionInfo(argumentExpression).Body is ExpressionSyntax expressionBody))
                            return;

                        ExtensionMethodSymbolInfo extensionMethodSymbolInfo = semanticModel.GetReducedExtensionMethodInfo(invocationExpression, context.CancellationToken);

                        IMethodSymbol reducedSymbol = extensionMethodSymbolInfo.ReducedSymbol;

                        if (reducedSymbol == null)
                            return;

                        switch (methodName)
                        {
                            case "Select":
                                {
                                    if (!SymbolUtility.IsLinqSelect(extensionMethodSymbolInfo.Symbol, allowImmutableArrayExtension: true))
                                        return;

                                    break;
                                }
                            case "Where":
                                {
                                    if (!SymbolUtility.IsLinqExtensionOfIEnumerableOfTWithPredicate(extensionMethodSymbolInfo.Symbol, methodName, allowImmutableArrayExtension: true))
                                        return;

                                    break;
                                }
                        }

                        ImmutableArray<ISymbol> captured = semanticModel.AnalyzeDataFlow(expressionBody).Captured;

                        if (captured.IsDefaultOrEmpty)
                            return;

                        context.RegisterRefactoring(
                            $"Expand '{methodName}' operation",
                            ct => ExpandLinqOperationAsync(context.Document, invocationExpression, reducedSymbol, semanticModel, ct),
                            RefactoringIdentifiers.ExpandLinqMethodOperation);

                        break;
                    }
                case "OfType":
                    {
                        InvocationExpressionSyntax invocationExpression = invocationInfo.InvocationExpression;

                        if (invocationInfo.Arguments.Any())
                            return;

                        if (!CSharpFacts.IsForEachExpression(invocationExpression.WalkUpParentheses()))
                            return;

                        if (!invocationInfo.Name.IsKind(SyntaxKind.GenericName))
                            return;

                        if (((GenericNameSyntax)invocationInfo.Name)
                            .TypeArgumentList?
                            .Arguments
                            .SingleOrDefault(shouldThrow: false) == null)
                        {
                            return;
                        }

                        ExtensionMethodSymbolInfo extensionMethodSymbolInfo = semanticModel.GetReducedExtensionMethodInfo(invocationExpression, context.CancellationToken);

                        IMethodSymbol reducedSymbol = extensionMethodSymbolInfo.ReducedSymbol;

                        if (reducedSymbol == null)
                            return;

                        if (!SymbolUtility.IsLinqOfType(extensionMethodSymbolInfo.Symbol))
                            return;

                        context.RegisterRefactoring(
                            $"Expand '{methodName}' operation",
                            ct => ExpandLinqOperationAsync(context.Document, invocationExpression, reducedSymbol, semanticModel, ct),
                            RefactoringIdentifiers.ExpandLinqMethodOperation);

                        break;
                    }
            }

            ExtractLinqToLocalFunctionRefactoring CreateRefactoring(InvocationExpressionSyntax invocationExpression, SyntaxNode body, SyntaxNode containingBody, ITypeSymbol typeArgument, ImmutableArray<ISymbol> captured)
            {
                switch (methodName)
                {
                    case "Any":
                        return new ExtractAnyToLocalFunctionRefactoring(context.Document, invocationExpression, body, containingBody, typeArgument, captured, semanticModel);
                    case "All":
                        return new ExtractAllToLocalFunctionRefactoring(context.Document, invocationExpression, body, containingBody, typeArgument, captured, semanticModel);
                    case "FirstOrDefault":
                        return new ExtractFirstOrDefaultToLocalFunctionRefactoring(context.Document, invocationExpression, body, containingBody, typeArgument, captured, semanticModel);
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        private static Task<Document> ExpandLinqOperationAsync(
            Document document,
            InvocationExpressionSyntax invocationExpression,
            IMethodSymbol methodSymbol,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            SimpleMemberInvocationExpressionInfo invocationInfo = SyntaxInfo.SimpleMemberInvocationExpressionInfo(invocationExpression);

            var forEachStatement = (ForEachStatementSyntax)invocationExpression.WalkUpParentheses().Parent;

            TypeSyntax type = forEachStatement.Type;

            SyntaxToken identifier = forEachStatement.Identifier;

            StatementSyntax statement = forEachStatement.Statement;

            switch (methodSymbol.Name)
            {
                case "Select":
                    {
                        ITypeSymbol typeArgument = methodSymbol.TypeArguments[0];

                        type = (typeArgument.SupportsExplicitDeclaration())
                            ? typeArgument.ToTypeSyntax().WithSimplifierAnnotation()
                            : VarType();

                        type = type.WithTriviaFrom(forEachStatement.Type);

                        ExpressionSyntax expression = invocationExpression.ArgumentList.Arguments.Single().Expression;

                        LambdaExpressionInfo lambdaInfo = SyntaxInfo.LambdaExpressionInfo(expression);

                        identifier = lambdaInfo
                            .FirstParameter
                            .Identifier
                            .WithTriviaFrom(forEachStatement.Identifier)
                            .WithRenameAnnotation();

                        LocalDeclarationStatementSyntax localDeclarationStatement = LocalDeclarationStatement(
                            forEachStatement.Type.WithoutTrivia(),
                            forEachStatement.Identifier.WithoutTrivia(),
                            ((ExpressionSyntax)lambdaInfo.Body).WithoutTrivia()).WithFormatterAnnotation();

                        if (statement.IsKind(SyntaxKind.Block))
                        {
                            var block = (BlockSyntax)statement;
                            statement = block.WithStatements(block.Statements.Insert(0, localDeclarationStatement));
                        }
                        else
                        {
                            statement = Block(statement, localDeclarationStatement);
                        }

                        break;
                    }
                case "Where":
                    {
                        ExpressionSyntax expression = invocationExpression.ArgumentList.Arguments.Single().Expression;

                        LambdaExpressionInfo lambdaInfo = SyntaxInfo.LambdaExpressionInfo(expression);

                        ParameterSyntax parameter = lambdaInfo.FirstParameter;

                        IParameterSymbol parameterSymbol = semanticModel.GetDeclaredSymbol(parameter, cancellationToken);

                        var rewriter = new LocalOrParameterSymbolRenamer(parameterSymbol, forEachStatement.Identifier.ValueText, semanticModel, cancellationToken);

                        var newExpression = (ExpressionSyntax)rewriter.Visit(lambdaInfo.Body);

                        statement = IfStatement(
                            newExpression,
                            (statement.IsKind(SyntaxKind.Block)) ? statement : Block(statement));

                        statement = Block(statement).WithFormatterAnnotation();

                        break;
                    }
                case "OfType":
                    {
                        type = VarType();

                        string name = NameGenerator.Default.EnsureUniqueLocalName(DefaultNames.ForEachVariable, semanticModel, statement.SpanStart, cancellationToken: cancellationToken);

                        identifier = Identifier(name).WithRenameAnnotation();

                        statement = IfStatement(
                            IsPatternExpression(
                                IdentifierName(name),
                                DeclarationPattern(
                                    ((GenericNameSyntax)invocationInfo.Name).TypeArgumentList.Arguments.Single().WithoutTrivia(),
                                    SingleVariableDesignation(forEachStatement.Identifier.WithoutTrivia()))),
                            (statement.IsKind(SyntaxKind.Block)) ? statement : Block(statement));

                        statement = Block(statement).WithFormatterAnnotation();

                        break;
                    }
                default:
                    {
                        throw new InvalidOperationException();
                    }
            }

            ForEachStatementSyntax newForEachStatement = forEachStatement.Update(
                forEachStatement.ForEachKeyword,
                forEachStatement.OpenParenToken,
                type,
                identifier.WithTriviaFrom(forEachStatement.Identifier),
                forEachStatement.InKeyword,
                invocationInfo.Expression,
                forEachStatement.CloseParenToken,
                statement);

            return document.ReplaceNodeAsync(forEachStatement, newForEachStatement, cancellationToken);
        }

        private static Task<Document> ReplaceLinqWithForEachAsync(
            Document document,
            InvocationExpressionSyntax invocationExpression,
            IMethodSymbol methodSymbol,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            SimpleMemberInvocationExpressionInfo invocationInfo = SyntaxInfo.SimpleMemberInvocationExpressionInfo(invocationExpression);

            string name = null;
            ExpressionSyntax condition = null;

            switch (invocationInfo.Arguments.Single().Expression.WalkDownParentheses())
            {
                case SimpleLambdaExpressionSyntax simpleLambda:
                    {
                        name = simpleLambda.Parameter.Identifier.ValueText;
                        condition = (ExpressionSyntax)simpleLambda.Body;
                        break;
                    }
                case ParenthesizedLambdaExpressionSyntax parenthesizedLambda:
                    {
                        name = parenthesizedLambda.ParameterList.Parameters[0].Identifier.ValueText;
                        condition = (ExpressionSyntax)parenthesizedLambda.Body;
                        break;
                    }
                default:
                    {
                        throw new InvalidOperationException();
                    }
            }

            if (methodSymbol.Name == "All")
                condition = Inverter.LogicallyNegate(condition, semanticModel, cancellationToken);

            IfStatementSyntax ifStatement = IfStatement(
                condition,
                Block(ReturnStatement(GetFirstReturnExpression())));

            ITypeSymbol typeArgument = methodSymbol.TypeArguments[0];

            TypeSyntax elementType = (typeArgument.SupportsExplicitDeclaration())
                    ? typeArgument.ToTypeSyntax().WithSimplifierAnnotation()
                    : VarType();

            ForEachStatementSyntax forEachStatement = ForEachStatement(
                elementType,
                Identifier(name).WithRenameAnnotation(),
                invocationInfo.Expression,
                Block(ifStatement));

            forEachStatement = forEachStatement.WithFormatterAnnotation();

            SyntaxNode parent = invocationExpression.WalkUpParentheses().Parent;

            ReturnStatementSyntax lastReturnStatement = ReturnStatement(GetLastReturnExpression());

            if (parent is ReturnStatementSyntax returnStatement)
            {
                forEachStatement = forEachStatement.WithLeadingTrivia(returnStatement.GetLeadingTrivia());
                lastReturnStatement = lastReturnStatement.WithTrailingTrivia(returnStatement.GetTrailingTrivia());

                return document.ReplaceNodeAsync(
                    returnStatement,
                    new StatementSyntax[] { forEachStatement, lastReturnStatement },
                    cancellationToken);
            }
            else
            {
                var expressionBody = (ArrowExpressionClauseSyntax)parent;

                (SyntaxNode node, BlockSyntax body) = ExpandExpressionBodyRefactoring.Refactor(expressionBody, semanticModel, cancellationToken);

                returnStatement = (ReturnStatementSyntax)body.Statements.Single();

                BlockSyntax newBody = body
                    .ReplaceNode(returnStatement, new StatementSyntax[] { forEachStatement, lastReturnStatement })
                    .WithFormatterAnnotation();

                SyntaxNode newNode = node.ReplaceNode(body, newBody);

                return document.ReplaceNodeAsync(expressionBody.Parent, newNode, cancellationToken);
            }

            ExpressionSyntax GetFirstReturnExpression()
            {
                switch (methodSymbol.Name)
                {
                    case "Any":
                        return TrueLiteralExpression();
                    case "All":
                        return FalseLiteralExpression();
                    case "FirstOrDefault":
                        return IdentifierName(name);
                    default:
                        throw new InvalidOperationException();
                }
            }

            ExpressionSyntax GetLastReturnExpression()
            {
                switch (methodSymbol.Name)
                {
                    case "Any":
                        return FalseLiteralExpression();
                    case "All":
                        return TrueLiteralExpression();
                    case "FirstOrDefault":
                        return typeArgument.GetDefaultValueSyntax(document.GetDefaultSyntaxOptions(), elementType);
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        private static SyntaxNode FindContainingBodyOrExpressionBody(SyntaxNode node)
        {
            for (SyntaxNode parent = node.Parent; parent != null; parent = parent.Parent)
            {
                switch (parent.Kind())
                {
                    case SyntaxKind.Block:
                    case SyntaxKind.ArrowExpressionClause:
                        {
                            switch (parent.Parent.Kind())
                            {
                                case SyntaxKind.MethodDeclaration:
                                case SyntaxKind.OperatorDeclaration:
                                case SyntaxKind.ConversionOperatorDeclaration:
                                case SyntaxKind.ConstructorDeclaration:
                                case SyntaxKind.DestructorDeclaration:
                                case SyntaxKind.PropertyDeclaration:
                                case SyntaxKind.EventDeclaration:
                                case SyntaxKind.IndexerDeclaration:
                                case SyntaxKind.GetAccessorDeclaration:
                                case SyntaxKind.SetAccessorDeclaration:
                                case SyntaxKind.AddAccessorDeclaration:
                                case SyntaxKind.RemoveAccessorDeclaration:
                                case SyntaxKind.UnknownAccessorDeclaration:
                                case SyntaxKind.LocalFunctionStatement:
                                    return parent;
                            }

                            Debug.Assert(!parent.IsKind(SyntaxKind.ArrowExpressionClause));
                            Debug.Assert(!(parent.Parent is MemberDeclarationSyntax), parent.Parent.Kind().ToString());
                            break;
                        }
                    case SyntaxKind.FieldDeclaration:
                        {
                            return null;
                        }
                    default:
                        {
                            Debug.Assert(!(parent is MemberDeclarationSyntax), parent.Kind().ToString());
                            break;
                        }
                }
            }

            return null;
        }
    }
}
