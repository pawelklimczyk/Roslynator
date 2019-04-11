// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Roslynator.CSharp.CSharpFactory;

namespace Roslynator.CSharp.Refactorings
{
    internal static class WhileStatementRefactoring
    {
        public static void ComputeRefactorings(RefactoringContext context, WhileStatementSyntax whileStatement)
        {
            SyntaxToken whileKeyword = whileStatement.WhileKeyword;

            bool spanIsEmptyAndContainedInWhileKeyword = context.Span.IsEmptyAndContainedInSpan(whileKeyword);

            if (context.IsRefactoringEnabled(RefactoringIdentifiers.ReplaceWhileWithDo)
                && spanIsEmptyAndContainedInWhileKeyword)
            {
                Document document = context.Document;

                context.RegisterRefactoring(
                    "Replace while with do",
                    ct => ReplaceWhileWithDoRefactoring.RefactorAsync(document, whileStatement, ct),
                    RefactoringIdentifiers.ReplaceWhileWithDo);
            }

            if (context.IsRefactoringEnabled(RefactoringIdentifiers.ReplaceWhileWithIfAndDo)
                && spanIsEmptyAndContainedInWhileKeyword
                && !whileStatement.Condition.IsKind(SyntaxKind.TrueLiteralExpression))
            {
                Document document = context.Document;

                context.RegisterRefactoring(
                    "Replace while with if + do",
                    ct => ReplaceWhileWithIfAndDoAsync(document, whileStatement, ct),
                    RefactoringIdentifiers.ReplaceWhileWithIfAndDo);
            }

            if (context.IsRefactoringEnabled(RefactoringIdentifiers.ReplaceWhileWithFor)
                && spanIsEmptyAndContainedInWhileKeyword)
            {
                Document document = context.Document;

                context.RegisterRefactoring(
                    ReplaceWhileWithForRefactoring.Title,
                    ct => ReplaceWhileWithForRefactoring.RefactorAsync(document, whileStatement, ct),
                    RefactoringIdentifiers.ReplaceWhileWithFor);
            }
        }

        private static Task<Document> ReplaceWhileWithIfAndDoAsync(
            Document document,
            WhileStatementSyntax whileStatement,
            CancellationToken cancellationToken = default)
        {
            DoStatementSyntax doStatement = DoStatement(
                Token(SyntaxKind.DoKeyword),
                whileStatement.Statement.WithoutTrailingTrivia(),
                Token(SyntaxKind.WhileKeyword),
                OpenParenToken(),
                whileStatement.Condition,
                CloseParenToken(),
                SemicolonToken());

            IfStatementSyntax ifStatement = IfStatement(
                Token(whileStatement.WhileKeyword.LeadingTrivia, SyntaxKind.IfKeyword, TriviaList()),
                OpenParenToken(),
                whileStatement.Condition,
                CloseParenToken(),
                Block(OpenBraceToken(), doStatement, Token(TriviaList(), SyntaxKind.CloseBraceToken, whileStatement.Statement.GetTrailingTrivia())),
                default(ElseClauseSyntax));

            ifStatement = ifStatement.WithFormatterAnnotation();

            return document.ReplaceNodeAsync(whileStatement, ifStatement, cancellationToken);
        }
    }
}