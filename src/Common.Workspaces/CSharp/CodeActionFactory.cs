// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslynator.CSharp.Refactorings;

namespace Roslynator.CSharp
{
    internal static class CodeActionFactory
    {
        public static CodeAction ChangeTypeToVar(
            Document document,
            TypeSyntax type,
            string title = null,
            string equivalenceKey = null)
        {
            return CodeAction.Create(
                title ?? "Change type to 'var'",
                ct => ChangeTypeRefactoring.ChangeTypeToVarAsync(document, type, ct),
                equivalenceKey);
        }

        public static CodeAction ChangeType(
            Document document,
            TypeSyntax type,
            ITypeSymbol newTypeSymbol,
            SemanticModel semanticModel,
            string title = null,
            string equivalenceKey = null)
        {
            title = title ?? $"Change type to '{SymbolDisplay.ToMinimalDisplayString(newTypeSymbol, semanticModel, type.SpanStart)}'";

            return ChangeType(document, type, newTypeSymbol, title, equivalenceKey);
        }

        public static CodeAction ChangeType(
            Document document,
            TypeSyntax type,
            ITypeSymbol newTypeSymbol,
            string title,
            string equivalenceKey = null)
        {
            return CodeAction.Create(
                title,
                ct => ChangeTypeRefactoring.ChangeTypeAsync(document, type, newTypeSymbol, ct),
                equivalenceKey);
        }
    }
}