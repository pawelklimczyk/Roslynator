﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Roslynator.CodeGeneration.CSharp;

namespace Roslynator.CodeGeneration
{
    internal class CodeGenerator : Generator
    {
        public CodeGenerator(string rootPath, StringComparer comparer = null)
            : base(rootPath, comparer)
        {
        }

        public void Generate()
        {
            WriteCompilationUnit(
                @"Refactorings\CSharp\RefactoringIdentifiers.Generated.cs",
                RefactoringIdentifiersGenerator.Generate(Refactorings, obsolete: false, comparer: Comparer));

            WriteCompilationUnit(
                @"Refactorings\CSharp\RefactoringIdentifiers.Deprecated.Generated.cs",
                RefactoringIdentifiersGenerator.Generate(Refactorings, obsolete: true, comparer: Comparer));

            WriteCompilationUnit(
                @"VisualStudio.Common\RefactoringsOptionsPage.Generated.cs",
                RefactoringsOptionsPageGenerator.Generate(Refactorings.Where(f => !f.IsObsolete), Comparer));

            WriteCompilationUnit(
                @"Analyzers\CSharp\DiagnosticDescriptors.Generated.cs",
                DiagnosticDescriptorsGenerator.Generate(Analyzers, obsolete: false, comparer: Comparer), normalizeWhitespace: false);

            WriteCompilationUnit(
                @"Analyzers\CSharp\DiagnosticDescriptors.Deprecated.Generated.cs",
                DiagnosticDescriptorsGenerator.Generate(Analyzers, obsolete: true, comparer: Comparer), normalizeWhitespace: false);

            WriteCompilationUnit(
                @"Analyzers\CSharp\DiagnosticIdentifiers.Generated.cs",
                DiagnosticIdentifiersGenerator.Generate(Analyzers, obsolete: false, comparer: Comparer));

            WriteCompilationUnit(
                @"Analyzers\CSharp\DiagnosticIdentifiers.Deprecated.Generated.cs",
                DiagnosticIdentifiersGenerator.Generate(Analyzers, obsolete: true, comparer: Comparer));

            WriteCompilationUnit(
                @"CodeFixes\CSharp\CodeFixIdentifiers.Generated.cs",
                CodeFixIdentifiersGenerator.Generate(CodeFixes, Comparer));

            WriteCompilationUnit(
                @"VisualStudio.Common\CodeFixesOptionsPage.Generated.cs",
                CodeFixesOptionsPageGenerator.Generate(CodeFixes, Comparer));

            WriteCompilationUnit(
                @"VisualStudio.Common\GlobalSuppressionsOptionsPage.Generated.cs",
                GlobalSuppressionsOptionsPageGenerator.Generate(Analyzers.Where(f => !f.IsObsolete), Comparer));

            WriteCompilationUnit(
                @"CSharp\CSharp\CompilerDiagnosticIdentifiers.Generated.cs",
                CompilerDiagnosticIdentifiersGenerator.Generate(CompilerDiagnostics, Comparer));

            WriteCompilationUnit(
                @"Tools\CodeGeneration\CSharp\Symbols.Generated.cs",
                SymbolsGetKindsGenerator.Generate());

            WriteCompilationUnit(
                @"CSharp\CSharp\SyntaxWalkers\CSharpSyntaxNodeWalker.cs",
                CSharpSyntaxNodeWalkerGenerator.Generate());
        }
    }
}
