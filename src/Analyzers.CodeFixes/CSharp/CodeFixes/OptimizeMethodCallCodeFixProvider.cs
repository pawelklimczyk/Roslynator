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

            Document document = context.Document;

            Diagnostic diagnostic = context.Diagnostics[0];

            switch (node.Kind())
            {
                case SyntaxKind.InvocationExpression:
                    {
                        var invocationExpression = (InvocationExpressionSyntax)node;

                        SimpleMemberInvocationExpressionInfo invocationInfo = SyntaxInfo.SimpleMemberInvocationExpressionInfo(invocationExpression);

                        switch (invocationInfo.NameText)
                        {
                            case "Compare":
                                {
                                    CodeAction codeAction = CodeAction.Create(
                                        "Call 'CompareOrdinal' instead of 'Compare'",
                                        ct => CallCompareOrdinalInsteadOfCompareAsync(document, invocationInfo, ct),
                                        base.GetEquivalenceKey(diagnostic));

                                    context.RegisterCodeFix(codeAction, diagnostic);
                                    break;
                                }
                            case "Join":
                                {
                                    CodeAction codeAction = CodeAction.Create(
                                        "Call 'Concat' instead of 'Join'",
                                        cancellationToken => CallStringConcatInsteadOfStringJoinAsync(document, invocationExpression, cancellationToken),
                                        base.GetEquivalenceKey(diagnostic));

                                    context.RegisterCodeFix(codeAction, diagnostic);
                                    break;
                                }
                        }

                        break;
                    }
                case SyntaxKind.EqualsExpression:
                    {
                        var equalsExpression = (BinaryExpressionSyntax)node;

                        CodeAction codeAction = CodeAction.Create(
                            "Call 'Equals' instead of 'Compare'",
                            ct => CallEqualsInsteadOfCompareAsync(document, equalsExpression, ct),
                            base.GetEquivalenceKey(diagnostic));

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

        private static Task<Document> CallStringConcatInsteadOfStringJoinAsync(
            Document document,
            InvocationExpressionSyntax invocation,
            CancellationToken cancellationToken)
        {
            var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;

            MemberAccessExpressionSyntax newMemberAccess = memberAccess.WithName(SyntaxFactory.IdentifierName("Concat").WithTriviaFrom(memberAccess.Name));

            ArgumentListSyntax argumentList = invocation.ArgumentList;
            SeparatedSyntaxList<ArgumentSyntax> arguments = argumentList.Arguments;

            ArgumentListSyntax newArgumentList = argumentList
                .WithArguments(arguments.RemoveAt(0))
                .WithOpenParenToken(argumentList.OpenParenToken.AppendToTrailingTrivia(arguments[0].GetLeadingAndTrailingTrivia()));

            InvocationExpressionSyntax newInvocation = invocation
                .WithExpression(newMemberAccess)
                .WithArgumentList(newArgumentList);

            return document.ReplaceNodeAsync(invocation, newInvocation, cancellationToken);
        }
    }
}
