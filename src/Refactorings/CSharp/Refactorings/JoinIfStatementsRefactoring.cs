// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Roslynator.CSharp.Refactorings
{
    internal static class JoinIfStatementsRefactoring
    {
        public static void ComputeRefactorings(RefactoringContext context, StatementListSelection selectedStatements)
        {
            if (selectedStatements.Count < 2)
                return;

            for (int i = 0; i < selectedStatements.Count - 1; i++)
            {
                if (!(selectedStatements[i] is IfStatementSyntax ifStatement))
                    return;

                foreach (IfStatementOrElseClause ifOrElse in ifStatement.AsCascade())
                {
                    if (ifOrElse.IsElse)
                        return;

                    StatementSyntax statement = ifOrElse.Statement;

                    if (statement is BlockSyntax block)
                        statement = block.Statements.LastOrDefault();

                    if (statement == null)
                        return;

                    if (!statement.IsKind(
                        SyntaxKind.ReturnStatement,
                        SyntaxKind.ContinueStatement,
                        SyntaxKind.BreakStatement,
                        SyntaxKind.ThrowStatement))
                    {
                        return;
                    }
                }
            }

            Document document = context.Document;

            context.RegisterRefactoring(
                "Join if statements",
                ct => RefactorAsync(document, selectedStatements, ct),
                RefactoringIdentifiers.JoinIfStatements);
        }

        private static Task<Document> RefactorAsync(
            Document document,
            StatementListSelection selectedStatements,
            CancellationToken cancellationToken)
        {
            IfStatementSyntax newIfStatement = null;

            for (int i = selectedStatements.Count - 2; i >= 0; i--)
            {
                var ifStatement = (IfStatementSyntax)selectedStatements[i];

                IfStatementSyntax lastIf = ifStatement.GetCascadeInfo().Last.AsIf();

                ElseClauseSyntax elseClause = SyntaxFactory.ElseClause(newIfStatement ?? selectedStatements.Last());

                IfStatementSyntax newLastIf = lastIf.WithElse(elseClause);

                newIfStatement = ifStatement.ReplaceNode(lastIf, newLastIf);
            }

            SyntaxList<StatementSyntax> newStatements = selectedStatements.UnderlyingList
                .Replace(selectedStatements.First(), newIfStatement.WithFormatterAnnotation())
                .RemoveRange(selectedStatements.FirstIndex + 1, selectedStatements.Count - 1);

            return document.ReplaceStatementsAsync(SyntaxInfo.StatementListInfo(selectedStatements), newStatements, cancellationToken);
        }
    }
}
