// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslynator.CSharp.Analysis.MakeMemberReadOnly
{
    internal sealed class UseReadOnlyAutoPropertyAnalysis : MakeMemberReadOnlyAnalysis<PropertyDeclarationSyntax>
    {
        [ThreadStatic]
        private static UseReadOnlyAutoPropertyAnalysis _cachedInstance;

        private UseReadOnlyAutoPropertyAnalysis()
        {
        }

        public static UseReadOnlyAutoPropertyAnalysis GetInstance()
        {
            UseReadOnlyAutoPropertyAnalysis walker = _cachedInstance;

            if (walker != null)
            {
                _cachedInstance = null;
                return walker;
            }

            return new UseReadOnlyAutoPropertyAnalysis();
        }

        public static void Free(UseReadOnlyAutoPropertyAnalysis analysis)
        {
            analysis.Clear();
            _cachedInstance = analysis;
        }

        public override void CollectAnalyzableSymbols()
        {
            foreach (MemberDeclarationSyntax memberDeclaration in TypeDeclaration.Members)
            {
                if (memberDeclaration.IsKind(SyntaxKind.PropertyDeclaration))
                {
                    var propertyDeclaration = (PropertyDeclarationSyntax)memberDeclaration;

                    ISymbol symbol = SemanticModel.GetDeclaredSymbol(propertyDeclaration, CancellationToken);

                    if (symbol.IsKind(SymbolKind.Property))
                    {
                        var propertySymbol = (IPropertySymbol)symbol;

                        if (!propertySymbol.IsIndexer
                            && !propertySymbol.IsReadOnly
                            && !propertySymbol.IsImplicitlyDeclared
                            && propertySymbol.ExplicitInterfaceImplementations.IsDefaultOrEmpty
                            && !propertySymbol.HasAttribute(MetadataNames.System_Runtime_Serialization_DataMemberAttribute))
                        {
                            IMethodSymbol setMethod = propertySymbol.SetMethod;

                            if (setMethod?.DeclaredAccessibility == Accessibility.Private
                                && setMethod.GetAttributes().IsEmpty
                                && setMethod.GetSyntaxOrDefault(CancellationToken) is AccessorDeclarationSyntax accessor
                                && accessor.BodyOrExpressionBody() == null)
                            {
                                Symbols.Add(propertySymbol, propertyDeclaration);
                            }
                        }
                    }
                }
            }
        }

        protected override bool ValidateSymbol(ISymbol symbol)
        {
            return symbol?.Kind == SymbolKind.Property;
        }

        public override void ReportFixableSymbols(SyntaxNodeAnalysisContext context)
        {
            foreach (KeyValuePair<ISymbol, PropertyDeclarationSyntax> kvp in Symbols)
            {
                AccessorDeclarationSyntax setter = kvp.Value.Setter();

                if (!setter.SpanContainsDirectives())
                    DiagnosticHelpers.ReportDiagnostic(context, DiagnosticDescriptors.UseReadOnlyAutoProperty, setter);
            }
        }
    }
}
