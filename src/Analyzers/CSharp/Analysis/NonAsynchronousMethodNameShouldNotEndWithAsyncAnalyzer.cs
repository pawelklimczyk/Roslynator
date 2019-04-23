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
    public class NonAsynchronousMethodNameShouldNotEndWithAsyncAnalyzer : BaseDiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(
                    DiagnosticDescriptors.NonAsynchronousMethodNameShouldNotEndWithAsync,
                    DiagnosticDescriptors.NonAsynchronousMethodNameShouldNotEndWithAsyncFadeOut);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            base.Initialize(context);

            context.RegisterCompilationStartAction(startContext =>
            {
                if (startContext.IsAnalyzerSuppressed(DiagnosticDescriptors.NonAsynchronousMethodNameShouldNotEndWithAsync))
                    return;

                INamedTypeSymbol asyncAction = startContext.Compilation.GetTypeByMetadataName("Windows.Foundation.IAsyncAction");

                bool shouldCheckWindowsRuntimeTypes = asyncAction != null;

                startContext.RegisterSyntaxNodeAction(nodeContext => AnalyzeMethodDeclaration(nodeContext, shouldCheckWindowsRuntimeTypes), SyntaxKind.MethodDeclaration);
            });
        }

        public static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context, bool shouldCheckWindowsRuntimeTypes)
        {
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;

            if (!methodDeclaration.Identifier.ValueText.EndsWith("Async", StringComparison.Ordinal))
                return;

            IMethodSymbol methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration, context.CancellationToken);

            if (methodSymbol == null)
                return;

            if (methodSymbol.IsAsync)
                return;

            if (!methodSymbol.Name.EndsWith("Async", StringComparison.Ordinal))
                return;

            if (SymbolUtility.IsAwaitable(methodSymbol.ReturnType, shouldCheckWindowsRuntimeTypes))
                return;

            SyntaxToken identifier = methodDeclaration.Identifier;

            DiagnosticHelpers.ReportDiagnostic(context,
                DiagnosticDescriptors.NonAsynchronousMethodNameShouldNotEndWithAsync,
                identifier);

            DiagnosticHelpers.ReportDiagnostic(context,
                DiagnosticDescriptors.NonAsynchronousMethodNameShouldNotEndWithAsyncFadeOut,
                Location.Create(identifier.SyntaxTree, TextSpan.FromBounds(identifier.Span.End - 5, identifier.Span.End)));
        }
    }
}
