// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslynator.CSharp;
using Roslynator.CSharp.Syntax;

namespace Roslynator.CodeAnalysis.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UsePropertySyntaxNodeRawKindAnalyzer : BaseDiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(DiagnosticDescriptors.UsePropertySyntaxNodeRawKind); }
        }

        public override void Initialize(AnalysisContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            base.Initialize(context);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeBinaryExpression, SyntaxKind.EqualsExpression);
            context.RegisterSyntaxNodeAction(AnalyzeBinaryExpression, SyntaxKind.NotEqualsExpression);
        }

        private static void AnalyzeBinaryExpression(SyntaxNodeAnalysisContext context)
        {
            var binaryExpression = (BinaryExpressionSyntax)context.Node;

            BinaryExpressionInfo binaryExpressionInfo = SyntaxInfo.BinaryExpressionInfo(binaryExpression);

            if (!binaryExpressionInfo.Success)
                return;

            ExpressionSyntax left = binaryExpressionInfo.Left;

            if (!IsFixableExpression(left))
                return;

            ExpressionSyntax right = binaryExpressionInfo.Right;

            if (!IsFixableExpression(right))
                return;

            ISymbol leftSymbol = context.SemanticModel.GetSymbol(left, context.CancellationToken);

            if (!IsFixableMethod(leftSymbol))
                return;

            ISymbol rightSymbol = context.SemanticModel.GetSymbol(right, context.CancellationToken);

            if (!IsFixableMethod(rightSymbol))
                return;

            context.ReportDiagnostic(DiagnosticDescriptors.UsePropertySyntaxNodeRawKind, binaryExpression);
        }

        private static bool IsFixableExpression(ExpressionSyntax expression)
        {
            if (!expression.IsKind(SyntaxKind.InvocationExpression))
                return false;

            SimpleMemberInvocationExpressionInfo invocationInfo = SyntaxInfo.SimpleMemberInvocationExpressionInfo(expression);

            if (!invocationInfo.Success)
                return false;

            if (invocationInfo.Arguments.Any())
                return false;

            SimpleNameSyntax name = invocationInfo.Name;

            if (!name.IsKind(SyntaxKind.IdentifierName))
                return false;

            var identifierName = (IdentifierNameSyntax)name;

            return string.Equals(identifierName.Identifier.ValueText, "Kind", StringComparison.Ordinal);
        }

        private static bool IsFixableMethod(ISymbol symbol)
        {
            if (!symbol.IsKind(SymbolKind.Method))
                return false;

            var methodSymbol = (IMethodSymbol)symbol;

            if (methodSymbol.MethodKind != MethodKind.ReducedExtension)
                return false;

            if (!methodSymbol.ReturnType.HasMetadataName(RoslynMetadataNames.Microsoft_CodeAnalysis_CSharp_SyntaxKind))
                return false;

            if (methodSymbol.ContainingType?.HasMetadataName(RoslynMetadataNames.Microsoft_CodeAnalysis_CSharp_CSharpExtensions) != true)
                return false;

            if (methodSymbol
                .ReducedFrom
                .Parameters
                .SingleOrDefault(shouldThrow: false)?
                .Type
                .HasMetadataName(RoslynMetadataNames.Microsoft_CodeAnalysis_SyntaxNode) != true)
            {
                return false;
            }

            return true;
        }
    }
}
