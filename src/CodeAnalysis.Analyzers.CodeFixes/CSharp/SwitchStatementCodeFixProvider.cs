// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslynator.CSharp;
using Roslynator.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Roslynator.CodeAnalysis.CSharp
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SwitchStatementCodeFixProvider))]
    [Shared]
    public class SwitchStatementCodeFixProvider : BaseCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(DiagnosticIdentifiers.UsePatternMatching); }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.GetSyntaxRootAsync().ConfigureAwait(false);

            if (!TryFindFirstAncestorOrSelf(root, context.Span, out SwitchStatementSyntax switchStatement))
                return;

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                switch (diagnostic.Id)
                {
                    case DiagnosticIdentifiers.UsePatternMatching:
                        {
                            CodeAction codeAction = CodeAction.Create(
                                "Use pattern matching",
                                ct => UsePatternMatchingAsync(context.Document, switchStatement, ct),
                                GetEquivalenceKey(diagnostic));

                            context.RegisterCodeFix(codeAction, diagnostic);
                            break;
                        }
                }
            }
        }

        private static Task<Document> UsePatternMatchingAsync(
            Document document,
            SwitchStatementSyntax switchStatement,
            CancellationToken cancellationToken)
        {
            SimpleMemberInvocationExpressionInfo invocationInfo = SyntaxInfo.SimpleMemberInvocationExpressionInfo(switchStatement.Expression);

            SyntaxList<SwitchSectionSyntax> newSections = switchStatement.Sections.Select(section =>
            {
                if (!(section.Labels.Single() is CaseSwitchLabelSyntax label))
                    return section;

                SyntaxList<StatementSyntax> statements = section.Statements;

                StatementSyntax statement = statements.First();

                if (statement.IsKind(SyntaxKind.Block))
                {
                    var block = (BlockSyntax)statement;

                    statement = block.Statements.FirstOrDefault();
                }

                SingleLocalDeclarationStatementInfo localInfo = SyntaxInfo.SingleLocalDeclarationStatementInfo((LocalDeclarationStatementSyntax)statement);

                var castExpression = (CastExpressionSyntax)localInfo.Value;

                CasePatternSwitchLabelSyntax newLabel = CasePatternSwitchLabel(
                    DeclarationPattern(
                        castExpression.Type,
                        SingleVariableDesignation(localInfo.Identifier)),
                    label.ColonToken);

                SwitchSectionSyntax newSection = section.RemoveStatement(localInfo.Statement);

                newSection = newSection.WithLabels(newSection.Labels.ReplaceAt(0, newLabel));

                return newSection.WithFormatterAnnotation();
            })
            .ToSyntaxList();

            SwitchStatementSyntax newSwitchStatement = switchStatement
                .WithExpression(invocationInfo.Expression.WithTriviaFrom(switchStatement.Expression))
                .WithSections(newSections);

            return document.ReplaceNodeAsync(switchStatement, newSwitchStatement, cancellationToken);
        }
    }
}
