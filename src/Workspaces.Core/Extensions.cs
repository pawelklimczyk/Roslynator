// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.Text;
using Roslynator.CodeMetrics;

namespace Roslynator
{
    internal static class Extensions
    {
        public static bool StartsWith(this string s, string value1, string value2, StringComparison comparisonType)
        {
            return s.StartsWith(value1, comparisonType) || s.StartsWith(value2, comparisonType);
        }

        public static bool HasFixAllProvider(this CodeFixProvider codeFixProvider, FixAllScope fixAllScope)
        {
            return codeFixProvider.GetFixAllProvider()?.GetSupportedFixAllScopes().Contains(fixAllScope) == true;
        }

        public static Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync(
            this Compilation compilation,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            CompilationWithAnalyzersOptions analysisOptions,
            CancellationToken cancellationToken = default)
        {
            var compilationWithAnalyzers = new CompilationWithAnalyzers(compilation, analyzers, analysisOptions);

            return compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync(cancellationToken);
        }

        public static bool IsAnalyzerExceptionDiagnostic(this Diagnostic diagnostic)
        {
            if (diagnostic.Id == "AD0001"
                || diagnostic.Id == "AD0002")
            {
                foreach (string tag in diagnostic.Descriptor.CustomTags)
                {
                    if (tag == WellKnownDiagnosticTags.AnalyzerException)
                        return true;
                }
            }

            return false;
        }

        public static void Add(this AnalyzerTelemetryInfo telemetryInfo, AnalyzerTelemetryInfo telemetryInfoToAdd)
        {
            telemetryInfo.CodeBlockActionsCount += telemetryInfoToAdd.CodeBlockActionsCount;
            telemetryInfo.CodeBlockEndActionsCount += telemetryInfoToAdd.CodeBlockEndActionsCount;
            telemetryInfo.CodeBlockStartActionsCount += telemetryInfoToAdd.CodeBlockStartActionsCount;
            telemetryInfo.CompilationActionsCount += telemetryInfoToAdd.CompilationActionsCount;
            telemetryInfo.CompilationEndActionsCount += telemetryInfoToAdd.CompilationEndActionsCount;
            telemetryInfo.CompilationStartActionsCount += telemetryInfoToAdd.CompilationStartActionsCount;
            telemetryInfo.ExecutionTime += telemetryInfoToAdd.ExecutionTime;
            telemetryInfo.OperationActionsCount += telemetryInfoToAdd.OperationActionsCount;
            telemetryInfo.OperationBlockActionsCount += telemetryInfoToAdd.OperationBlockActionsCount;
            telemetryInfo.OperationBlockEndActionsCount += telemetryInfoToAdd.OperationBlockEndActionsCount;
            telemetryInfo.OperationBlockStartActionsCount += telemetryInfoToAdd.OperationBlockStartActionsCount;
            telemetryInfo.SemanticModelActionsCount += telemetryInfoToAdd.SemanticModelActionsCount;
            telemetryInfo.SymbolActionsCount += telemetryInfoToAdd.SymbolActionsCount;
            telemetryInfo.SyntaxNodeActionsCount += telemetryInfoToAdd.SyntaxNodeActionsCount;
            telemetryInfo.SyntaxTreeActionsCount += telemetryInfoToAdd.SyntaxTreeActionsCount;
        }

        public static ReportDiagnostic ToReportDiagnostic(this DiagnosticSeverity diagnosticSeverity)
        {
            switch (diagnosticSeverity)
            {
                case DiagnosticSeverity.Hidden:
                    return ReportDiagnostic.Hidden;
                case DiagnosticSeverity.Info:
                    return ReportDiagnostic.Info;
                case DiagnosticSeverity.Warning:
                    return ReportDiagnostic.Warn;
                case DiagnosticSeverity.Error:
                    return ReportDiagnostic.Error;
                default:
                    throw new ArgumentException("", nameof(diagnosticSeverity));
            }
        }

        public static ConsoleColor GetColor(this DiagnosticSeverity diagnosticSeverity)
        {
            switch (diagnosticSeverity)
            {
                case DiagnosticSeverity.Hidden:
                    return ConsoleColor.DarkGray;
                case DiagnosticSeverity.Info:
                    return ConsoleColor.Cyan;
                case DiagnosticSeverity.Warning:
                    return ConsoleColor.Yellow;
                case DiagnosticSeverity.Error:
                    return ConsoleColor.Red;
                default:
                    throw new InvalidOperationException($"Unknown diagnostic severity '{diagnosticSeverity}'.");
            }
        }

        public static DiagnosticSeverity ToDiagnosticSeverity(this ReportDiagnostic reportDiagnostic)
        {
            switch (reportDiagnostic)
            {
                case ReportDiagnostic.Error:
                    return DiagnosticSeverity.Error;
                case ReportDiagnostic.Warn:
                    return DiagnosticSeverity.Warning;
                case ReportDiagnostic.Info:
                    return DiagnosticSeverity.Info;
                case ReportDiagnostic.Hidden:
                    return DiagnosticSeverity.Hidden;
                default:
                    throw new ArgumentException("", nameof(reportDiagnostic));
            }
        }

        public static async Task<CodeMetricsInfo> CountLinesAsync(
            this CodeMetricsCounter counter,
            Project project,
            CodeMetricsOptions options = null,
            CancellationToken cancellationToken = default)
        {
            CodeMetricsInfo codeMetrics = default;

            foreach (Document document in project.Documents)
            {
                if (!document.SupportsSyntaxTree)
                    continue;

                CodeMetricsInfo documentMetrics = await counter.CountLinesAsync(document, options, cancellationToken).ConfigureAwait(false);

                codeMetrics = codeMetrics.Add(documentMetrics);
            }

            return codeMetrics;
        }

        public static async Task<CodeMetricsInfo> CountLinesAsync(
            this CodeMetricsCounter counter,
            Document document,
            CodeMetricsOptions options = null,
            CancellationToken cancellationToken = default)
        {
            SyntaxTree tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            if (tree == null)
                return default;

            if (!options.IncludeGeneratedCode
                && GeneratedCodeUtility.IsGeneratedCode(tree, counter.SyntaxFacts.IsComment, cancellationToken))
            {
                return default;
            }

            SyntaxNode root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            SourceText sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            return counter.CountLines(root, sourceText, options, cancellationToken);
        }
    }
}
