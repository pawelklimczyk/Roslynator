// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslynator.CSharp;
using Roslynator.CSharp.Syntax;

namespace Roslynator.CodeAnalysis.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class InvocationExpressionAnalyzer : BaseDiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(DiagnosticDescriptors.UnnecessaryNullCheck); }
        }

        public override void Initialize(AnalysisContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            base.Initialize(context);

            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (invocation.ContainsDiagnostics)
                return;

            SimpleMemberInvocationExpressionInfo invocationInfo = SyntaxInfo.SimpleMemberInvocationExpressionInfo(invocation);

            if (!invocationInfo.Success)
                return;

            string methodName = invocationInfo.NameText;

            switch (invocationInfo.Arguments.Count)
            {
                case 1:
                    {
                        switch (methodName)
                        {
                            case "IsKind":
                            case "Any":
                                {
                                    if (!context.IsAnalyzerSuppressed(DiagnosticDescriptors.UnnecessaryNullCheck))
                                        AnalyzeUnnecessaryNullCheck(context, invocationInfo);

                                    break;
                                }
                        }

                        break;
                    }
            }
        }

        private static void AnalyzeUnnecessaryNullCheck(SyntaxNodeAnalysisContext context, SimpleMemberInvocationExpressionInfo invocationInfo)
        {
            ExpressionSyntax expression = invocationInfo.InvocationExpression.WalkUpParentheses();

            SyntaxNode parent = expression.Parent;

            if (parent.IsKind(SyntaxKind.LogicalNotExpression))
            {
                expression = (ExpressionSyntax)parent;

                expression = expression.WalkUpParentheses();

                parent = expression.Parent;
            }

            if (!parent.IsKind(SyntaxKind.LogicalAndExpression))
                return;

            var binaryExpression = (BinaryExpressionSyntax)parent;

            if (expression != binaryExpression.Right)
                return;

            if (binaryExpression.Left.ContainsDirectives)
                return;

            if (binaryExpression.OperatorToken.ContainsDirectives)
                return;

            NullCheckExpressionInfo nullCheckInfo = SyntaxInfo.NullCheckExpressionInfo(binaryExpression.Left, NullCheckStyles.CheckingNotNull & ~NullCheckStyles.HasValue);

            if (!nullCheckInfo.Success)
                return;

            if (!CSharpFactory.AreEquivalent(invocationInfo.Expression, nullCheckInfo.Expression))
                return;

            TextSpan span = TextSpan.FromBounds(binaryExpression.Left.SpanStart, binaryExpression.OperatorToken.Span.End);

            context.ReportDiagnostic(
                DiagnosticDescriptors.UnnecessaryNullCheck,
                Location.Create(invocationInfo.InvocationExpression.SyntaxTree, span));
        }
    }
}
