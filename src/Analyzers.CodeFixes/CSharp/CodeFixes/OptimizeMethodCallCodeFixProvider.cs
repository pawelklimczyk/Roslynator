// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslynator.CodeFixes;
using Roslynator.CSharp.Syntax;

namespace Roslynator.CSharp.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(OptimizeMethodCallCodeFixProvider))]
    [Shared]
    public class OptimizeMethodCallCodeFixProvider : BaseCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(DiagnosticIdentifiers.OptimizeMethodCall); }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.GetSyntaxRootAsync().ConfigureAwait(false);

            if (!TryFindFirstAncestorOrSelf(root, context.Span, out SyntaxNode node, predicate: f => f.IsKind(SyntaxKind.InvocationExpression, SyntaxKind.EqualsExpression)))
                return;

            Diagnostic diagnostic = context.Diagnostics[0];

            switch (node.Kind())
            {
                case SyntaxKind.InvocationExpression:
                    {
                        var invocationExpression = (InvocationExpressionSyntax)node;

                        SimpleMemberInvocationExpressionInfo invocationInfo = SyntaxInfo.SimpleMemberInvocationExpressionInfo(invocationExpression);

                        CodeAction codeAction = CodeAction.Create(
                            "Call 'CompareOrdinal' instead of 'Compare'",
                            ct => CallCompareOrdinalInsteadOfCompareAsync(context.Document, invocationInfo, ct),
                            GetEquivalenceKey(diagnostic));

                        context.RegisterCodeFix(codeAction, diagnostic);
                        break;
                    }
                case SyntaxKind.EqualsExpression:
                    {
                        var equalsExpression = (BinaryExpressionSyntax)node;

                        CodeAction codeAction = CodeAction.Create(
                            "Call 'Equals' instead of 'Compare'",
                            ct => CallEqualsInsteadOfCompareAsync(context.Document, equalsExpression, ct),
                            GetEquivalenceKey(diagnostic));

                        context.RegisterCodeFix(codeAction, diagnostic);
                        break;
                    }
            }
        }

        private static Task<Document> CallCompareOrdinalInsteadOfCompareAsync(
            Document document,
            in SimpleMemberInvocationExpressionInfo invocationInfo,
            CancellationToken cancellationToken)
        {
            InvocationExpressionSyntax invocationExpression = invocationInfo.InvocationExpression;

            MemberAccessExpressionSyntax memberAccessExpression = invocationInfo.MemberAccessExpression;

            MemberAccessExpressionSyntax newMemberAccessExpression = memberAccessExpression.WithName(SyntaxFactory.IdentifierName("CompareOrdinal").WithTriviaFrom(memberAccessExpression.Name));

            ArgumentListSyntax argumentList = invocationExpression.ArgumentList;

            ArgumentListSyntax newArgumentList = argumentList.WithArguments(argumentList.Arguments.RemoveAt(2));

            InvocationExpressionSyntax newInvocationExpression = invocationExpression.Update(newMemberAccessExpression, newArgumentList);

            return document.ReplaceNodeAsync(invocationExpression, newInvocationExpression, cancellationToken);
        }

        private static Task<Document> CallEqualsInsteadOfCompareAsync(
            Document document,
            BinaryExpressionSyntax equalsExpression,
            CancellationToken cancellationToken)
        {
            if (!(equalsExpression.Left.WalkDownParentheses() is InvocationExpressionSyntax invocationExpression))
                invocationExpression = (InvocationExpressionSyntax)equalsExpression.Right.WalkDownParentheses();

            InvocationExpressionSyntax newInvocationExpression = RefactoringUtility.ChangeInvokedMethodName(invocationExpression, "Equals");

            newInvocationExpression = newInvocationExpression.WithTriviaFrom(equalsExpression);

            return document.ReplaceNodeAsync(equalsExpression, newInvocationExpression, cancellationToken);
        }
    }
}
