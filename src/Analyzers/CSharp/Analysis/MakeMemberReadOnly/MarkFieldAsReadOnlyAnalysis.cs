// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslynator.CSharp.Analysis.MakeMemberReadOnly
{
    internal sealed class MarkFieldAsReadOnlyAnalysis : MakeMemberReadOnlyAnalysis<VariableDeclaratorSyntax>
    {
        [ThreadStatic]
        private static MarkFieldAsReadOnlyAnalysis _cachedInstance;

        private MarkFieldAsReadOnlyAnalysis()
        {
        }

        public static MarkFieldAsReadOnlyAnalysis GetInstance()
        {
            MarkFieldAsReadOnlyAnalysis walker = _cachedInstance;

            if (walker != null)
            {
                _cachedInstance = null;
                return walker;
            }

            return new MarkFieldAsReadOnlyAnalysis();
        }

        public static void Free(MarkFieldAsReadOnlyAnalysis analysis)
        {
            analysis.Clear();
            _cachedInstance = analysis;
        }

        public override void CollectAnalyzableSymbols()
        {
            foreach (MemberDeclarationSyntax memberDeclaration in TypeDeclaration.Members)
            {
                if (memberDeclaration.IsKind(SyntaxKind.FieldDeclaration))
                {
                    var fieldDeclaration = (FieldDeclarationSyntax)memberDeclaration;

                    VariableDeclarationSyntax variableDeclaration = fieldDeclaration.Declaration;

                    foreach (VariableDeclaratorSyntax declarator in variableDeclaration.Variables)
                    {
                        ISymbol symbol = SemanticModel.GetDeclaredSymbol(declarator, CancellationToken);

                        if (symbol.IsKind(SymbolKind.Field))
                        {
                            var fieldSymbol = (IFieldSymbol)symbol;

                            if (!fieldSymbol.IsConst
                                && fieldSymbol.DeclaredAccessibility == Accessibility.Private
                                && !fieldSymbol.IsReadOnly
                                && !fieldSymbol.IsVolatile
                                && !fieldSymbol.IsImplicitlyDeclared
                                && (fieldSymbol.Type.IsReferenceType
                                    || CSharpFacts.IsSimpleType(fieldSymbol.Type.SpecialType)
                                    || fieldSymbol.Type.TypeKind == TypeKind.Enum))
                            {
                                Symbols.Add(fieldSymbol, declarator);
                            }
                        }
                    }
                }
            }
        }

        protected override bool ValidateSymbol(ISymbol symbol)
        {
            return symbol?.Kind == SymbolKind.Field;
        }

        public override void ReportFixableSymbols(SyntaxNodeAnalysisContext context)
        {
            foreach (IGrouping<VariableDeclarationSyntax, VariableDeclaratorSyntax> grouping in Symbols
                .Select(f => f.Value)
                .GroupBy(f => (VariableDeclarationSyntax)f.Parent))
            {
                int count = grouping.Count();
                VariableDeclarationSyntax declaration = grouping.Key;

                int variablesCount = declaration.Variables.Count;

                if (variablesCount == 1
                    || variablesCount == count)
                {
                    DiagnosticHelpers.ReportDiagnostic(context,
                        DiagnosticDescriptors.MarkFieldAsReadOnly,
                        declaration.Parent);
                }
            }
        }
    }
}
