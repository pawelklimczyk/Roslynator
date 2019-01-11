// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslynator.CSharp;
using Roslynator.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Roslynator.CodeAnalysis.CSharp
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EqualsExpressionCodeFixProvider))]
    [Shared]
    public class EqualsExpressionCodeFixProvider : BaseCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(DiagnosticIdentifiers.UsePropertySyntaxNodeRawKind); }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.GetSyntaxRootAsync().ConfigureAwait(false);

            if (!TryFindFirstAncestorOrSelf(root, context.Span, out BinaryExpressionSyntax binaryExpression))
                return;

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                switch (diagnostic.Id)
                {
                    case DiagnosticIdentifiers.UsePropertySyntaxNodeRawKind:
                        {
                            CodeAction codeAction = CodeAction.Create(
                                "Use property 'RawKind'",
                                ct => UsePropertySyntaxNodeRawKindAsync(context.Document, binaryExpression, ct),
                                GetEquivalenceKey(diagnostic));

                            context.RegisterCodeFix(codeAction, diagnostic);
                            break;
                        }
                }
            }
        }

        private static Task<Document> UsePropertySyntaxNodeRawKindAsync(
            Document document,
            BinaryExpressionSyntax equalsExpression,
            CancellationToken cancellationToken)
        {
            BinaryExpressionInfo equalsExpressionInfo = SyntaxInfo.BinaryExpressionInfo(equalsExpression);

            BinaryExpressionSyntax newEqualsExpression = equalsExpression.Update(
                CreateNewExpression(equalsExpressionInfo.Left),
                equalsExpression.OperatorToken,
                CreateNewExpression(equalsExpressionInfo.Right));

            return document.ReplaceNodeAsync(equalsExpression, newEqualsExpression, cancellationToken);
        }

        private static ExpressionSyntax CreateNewExpression(ExpressionSyntax expression)
        {
            SimpleMemberInvocationExpressionInfo invocationInfo = SyntaxInfo.SimpleMemberInvocationExpressionInfo(expression);

            MemberAccessExpressionSyntax memberAccessExpression = invocationInfo.MemberAccessExpression;

            IdentifierNameSyntax newName = IdentifierName("RawKind")
                .WithTriviaFrom(memberAccessExpression.Name)
                .AppendToTrailingTrivia(invocationInfo.ArgumentList.GetTrailingTrivia());

            return memberAccessExpression.WithName(newName);
        }
    }
}
