// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Roslynator.CSharp.Analysis
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AddCommaAfterLastItemInListAnalyzer : BaseDiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(DiagnosticDescriptors.AddCommaAfterLastItemInList); }
        }

        public override void Initialize(AnalysisContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            base.Initialize(context);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeEnumDeclaration, SyntaxKind.EnumDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeInitializerExpression, SyntaxKind.ArrayInitializerExpression);
            context.RegisterSyntaxNodeAction(AnalyzeInitializerExpression, SyntaxKind.CollectionInitializerExpression);
            context.RegisterSyntaxNodeAction(AnalyzeInitializerExpression, SyntaxKind.ObjectInitializerExpression);
        }

        private static void AnalyzeInitializerExpression(SyntaxNodeAnalysisContext context)
        {
            var initializer = (InitializerExpressionSyntax)context.Node;

            SeparatedSyntaxList<ExpressionSyntax> expressions = initializer.Expressions;

            int count = expressions.Count;

            if (count == 0)
                return;

            if (count - expressions.SeparatorCount != 1)
                return;

            if (count == 1)
            {
                if (initializer.IsSingleLine(cancellationToken: context.CancellationToken))
                    return;
            }
            else if (!expressions.GetSeparator(count - 2).TrailingTrivia.Contains(SyntaxKind.EndOfLineTrivia))
            {
                return;
            }

            DiagnosticHelpers.ReportDiagnostic(context, DiagnosticDescriptors.AddCommaAfterLastItemInList, Location.Create(initializer.SyntaxTree, new TextSpan(expressions.Last().Span.End, 0)));
        }

        public static void AnalyzeEnumDeclaration(SyntaxNodeAnalysisContext context)
        {
            var enumDeclaration = (EnumDeclarationSyntax)context.Node;

            SeparatedSyntaxList<EnumMemberDeclarationSyntax> members = enumDeclaration.Members;

            int count = members.Count;

            if (count <= 1)
                return;

            if (count - members.SeparatorCount != 1)
                return;

            if (count == 1)
            {
                if (enumDeclaration.SyntaxTree.IsSingleLineSpan(enumDeclaration.BracesSpan(), context.CancellationToken))
                    return;
            }
            else if (!members.GetSeparator(count - 2).TrailingTrivia.Contains(SyntaxKind.EndOfLineTrivia))
            {
                return;
            }

            DiagnosticHelpers.ReportDiagnostic(context, DiagnosticDescriptors.AddCommaAfterLastItemInList, Location.Create(enumDeclaration.SyntaxTree, new TextSpan(members.Last().Span.End, 0)));
        }
    }
}
