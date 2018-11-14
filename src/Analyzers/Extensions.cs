// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

#pragma warning disable RS1012

namespace Roslynator
{
    internal static class Extensions
    {
        public static bool IsDiagnosticEnabled(this CompilationStartAnalysisContext context, DiagnosticDescriptor descriptor)
        {
            return IsDiagnosticEnabled(context.Compilation, descriptor);
        }

        public static bool IsDiagnosticEnabled(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor)
        {
            return IsDiagnosticEnabled(context.Compilation, descriptor);
        }

        public static bool IsDiagnosticEnabled(this Compilation compilation, DiagnosticDescriptor descriptor)
        {
            ReportDiagnostic reportDiagnostic = compilation
                .Options
                .SpecificDiagnosticOptions
                .GetValueOrDefault(descriptor.Id);

            switch (reportDiagnostic)
            {
                case ReportDiagnostic.Default:
                    return descriptor.IsEnabledByDefault;
                case ReportDiagnostic.Suppress:
                    return false;
                default:
                    return true;
            }
        }

        public static bool IsAnyDiagnosticEnabled(this CompilationStartAnalysisContext context, DiagnosticDescriptor descriptor1, DiagnosticDescriptor descriptor2)
        {
            return IsDiagnosticEnabled(context, descriptor1)
                || IsDiagnosticEnabled(context, descriptor2);
        }

        public static bool IsAnyDiagnosticEnabled(this CompilationStartAnalysisContext context, DiagnosticDescriptor descriptor1, DiagnosticDescriptor descriptor2, DiagnosticDescriptor descriptor3)
        {
            return IsDiagnosticEnabled(context, descriptor1)
                || IsDiagnosticEnabled(context, descriptor2)
                || IsDiagnosticEnabled(context, descriptor3);
        }
    }
}
