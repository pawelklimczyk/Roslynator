// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslynator.CSharp.Analysis.MakeMemberReadOnly
{
    internal abstract class MakeMemberReadOnlyAnalysis<TNode> where TNode : SyntaxNode
    {
        public TypeDeclarationSyntax TypeDeclaration { get; private set; }

        public SemanticModel SemanticModel { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public MakeMemberReadOnlyWalker Walker { get; } = new MakeMemberReadOnlyWalker();

        public Dictionary<ISymbol, TNode> Symbols { get; } = new Dictionary<ISymbol, TNode>();

        public void Clear()
        {
            TypeDeclaration = null;
            SemanticModel = null;
            CancellationToken = default;
            Walker.Clear();
            Symbols.Clear();
        }

        public abstract void CollectAnalyzableSymbols();

        public abstract void ReportFixableSymbols(SyntaxNodeAnalysisContext context);

        public virtual void CollectFixableSymbols()
        {
            foreach (MemberDeclarationSyntax memberDeclaration in TypeDeclaration.Members)
            {
                Walker.Clear();

                Walker.Visit(memberDeclaration);

                HashSet<AssignedInfo> assigned = Walker.Assigned;

                if (assigned != null)
                {
                    foreach (AssignedInfo assignedInfo in assigned)
                    {
                        foreach (KeyValuePair<ISymbol, TNode> kvp in Symbols)
                        {
                            ISymbol symbol = kvp.Key;

                            if (symbol.Name == assignedInfo.NameText
                                && ((symbol.IsStatic) ? !assignedInfo.IsInStaticConstructor : !assignedInfo.IsInInstanceConstructor))
                            {
                                ISymbol nameSymbol = SemanticModel.GetSymbol(assignedInfo.Name, CancellationToken);

                                if (ValidateSymbol(nameSymbol)
                                    && Symbols.Remove(nameSymbol.OriginalDefinition))
                                {
                                    if (Symbols.Count == 0)
                                        return;

                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        protected virtual bool ValidateSymbol(ISymbol symbol)
        {
            return symbol != null;
        }

        public void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context)
        {
            TypeDeclaration = (TypeDeclarationSyntax)context.Node;

            if (TypeDeclaration.Modifiers.Contains(SyntaxKind.PartialKeyword))
                return;

            CollectAnalyzableSymbols();

            if (Symbols != null)
            {
                CollectFixableSymbols();

                if (Symbols.Count > 0)
                    ReportFixableSymbols(context);
            }

            Clear();
        }
    }
}
