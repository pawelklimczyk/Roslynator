// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslynator.CSharp.Analysis.RemoveAsyncAwait;
using Roslynator.CSharp.CodeFixes;

namespace Roslynator.CSharp.Refactorings
{
    internal static class RemoveAsyncAwaitRefactoring
    {
        public static async Task ComputeRefactoringsAsync(RefactoringContext context, SyntaxToken token)
        {
            SyntaxNode parent = token.Parent;

            switch (parent.Kind())
            {
                case SyntaxKind.MethodDeclaration:
                    {
                        var methodDeclaration = (MethodDeclarationSyntax)parent;

                        SemanticModel semanticModel = await context.GetSemanticModelAsync().ConfigureAwait(false);

                        if (RemoveRedundantAsyncAwaitAnalysis.AnalyzeMethodDeclaration(methodDeclaration, semanticModel, context.CancellationToken))
                            RegisterRefactoring(context, token);

                        return;
                    }
                case SyntaxKind.LocalFunctionStatement:
                    {
                        var localFunction = (LocalFunctionStatementSyntax)parent;

                        SemanticModel semanticModel = await context.GetSemanticModelAsync().ConfigureAwait(false);

                        if (RemoveRedundantAsyncAwaitAnalysis.AnalyzeLocalFunctionStatement(localFunction, semanticModel, context.CancellationToken))
                            RegisterRefactoring(context, token);

                        return;
                    }
                case SyntaxKind.ParenthesizedLambdaExpression:
                    {
                        var parenthesizedLambda = (ParenthesizedLambdaExpressionSyntax)parent;

                        SemanticModel semanticModel = await context.GetSemanticModelAsync().ConfigureAwait(false);

                        if (RemoveRedundantAsyncAwaitAnalysis.AnalyzeLambdaExpression(parenthesizedLambda, semanticModel, context.CancellationToken))
                            RegisterRefactoring(context, token);

                        return;
                    }
                case SyntaxKind.SimpleLambdaExpression:
                    {
                        var simpleLambda = (SimpleLambdaExpressionSyntax)parent;

                        SemanticModel semanticModel = await context.GetSemanticModelAsync().ConfigureAwait(false);

                        if (RemoveRedundantAsyncAwaitAnalysis.AnalyzeLambdaExpression(simpleLambda, semanticModel, context.CancellationToken))
                            RegisterRefactoring(context, token);

                        return;
                    }
                case SyntaxKind.AnonymousMethodExpression:
                    {
                        var anonymousMethod = (AnonymousMethodExpressionSyntax)parent;

                        SemanticModel semanticModel = await context.GetSemanticModelAsync().ConfigureAwait(false);

                        if (RemoveRedundantAsyncAwaitAnalysis.AnalyzeAnonymousMethodExpression(anonymousMethod, semanticModel, context.CancellationToken))
                            RegisterRefactoring(context, token);

                        return;
                    }
            }
        }

        private static void RegisterRefactoring(RefactoringContext context, SyntaxToken token)
        {
            context.RegisterRefactoring(
                "Remove async/await",
                ct => RemoveAsyncAwaitCodeFix.RefactorAsync(context.Document, token, ct),
                RefactoringIdentifiers.RemoveAsyncAwait);
        }
    }
}
