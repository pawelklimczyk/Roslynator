// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslynator.CSharp.Syntax;

namespace Roslynator.CSharp.Analysis.ReplaceEnumeratorWithForEach
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ReplaceEnumeratorWithForEachAnalyzer : BaseDiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(DiagnosticDescriptors.ReplaceEnumeratorWithForEach); }
        }

        public override void Initialize(AnalysisContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            base.Initialize(context);

            context.RegisterSyntaxNodeAction(AnalyzeUsingStatement, SyntaxKind.UsingStatement);
        }

        public static void AnalyzeUsingStatement(SyntaxNodeAnalysisContext context)
        {
            var usingStatement = (UsingStatementSyntax)context.Node;

            VariableDeclaratorSyntax declarator = usingStatement.Declaration?.Variables.SingleOrDefault(shouldThrow: false);

            if (declarator == null)
                return;

            if (!(usingStatement.Statement?.SingleNonBlockStatementOrDefault() is WhileStatementSyntax whileStatement))
                return;

            SimpleMemberInvocationExpressionInfo invocationInfo = SyntaxInfo.SimpleMemberInvocationExpressionInfo(whileStatement.Condition);

            if (!invocationInfo.Success)
                return;

            if (invocationInfo.Arguments.Any())
                return;

            if (!string.Equals(invocationInfo.NameText, WellKnownMemberNames.MoveNextMethodName, StringComparison.Ordinal))
                return;

            if (!string.Equals((invocationInfo.Expression as IdentifierNameSyntax)?.Identifier.ValueText, declarator.Identifier.ValueText, StringComparison.Ordinal))
                return;

            SimpleMemberInvocationExpressionInfo invocationInfo2 = SyntaxInfo.SimpleMemberInvocationExpressionInfo(declarator.Initializer.Value);

            if (!invocationInfo2.Success)
                return;

            if (invocationInfo2.Arguments.Any())
                return;

            if (!string.Equals(invocationInfo2.NameText, WellKnownMemberNames.GetEnumeratorMethodName, StringComparison.Ordinal))
                return;

            ReplaceEnumeratorWithForEachWalker walker = ReplaceEnumeratorWithForEachWalkerCache.GetInstance();

            walker.SetValues(declarator, context.SemanticModel, context.CancellationToken);

            walker.Visit(whileStatement.Statement);

            if (!ReplaceEnumeratorWithForEachWalkerCache.GetIsFixableAndFree(walker))
                return;

            DiagnosticHelpers.ReportDiagnostic(context, DiagnosticDescriptors.ReplaceEnumeratorWithForEach, usingStatement.UsingKeyword);
        }
    }
}
