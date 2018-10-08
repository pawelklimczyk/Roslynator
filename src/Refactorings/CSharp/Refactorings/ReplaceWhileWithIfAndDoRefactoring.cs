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
    internal static class ReplaceWhileWithIfAndDoRefactoring
    {
        public static Task<Document> RefactorAsync(
            Document document,
            WhileStatementSyntax whileStatement,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            DoStatementSyntax doStatement = DoStatement(
                Token(
                    default(SyntaxTriviaList),
                    SyntaxKind.DoKeyword,
                    whileStatement.CloseParenToken.TrailingTrivia),
                whileStatement.Statement.WithoutTrailingTrivia(),
                Token(SyntaxKind.WhileKeyword),
                OpenParenToken(),
                whileStatement.Condition,
                CloseParenToken(),
                SemicolonToken());

            IfStatementSyntax ifStatement = IfStatement(whileStatement.Condition, Block(doStatement))
                .WithTriviaFrom(whileStatement)
                .WithFormatterAnnotation();

            return document.ReplaceNodeAsync(whileStatement, ifStatement, cancellationToken);
        }
    }
}
