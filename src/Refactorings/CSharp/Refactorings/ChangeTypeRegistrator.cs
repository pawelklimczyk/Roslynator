// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Roslynator.CSharp.Refactorings
{
    internal static class ChangeTypeRegistrator
    {
        public static void ChangeExplicitTypeToVar(
            RefactoringContext context,
            TypeSyntax type,
            string title = null,
            string equivalenceKey = null)
        {
            CodeAction codeAction = CodeActionFactory.ChangeTypeToVar(context.Document, type, title, equivalenceKey ?? RefactoringIdentifiers.ChangeExplicitTypeToVar);

            context.RegisterRefactoring(codeAction);
        }

        public static void ChangeVarToExplicitType(
            RefactoringContext context,
            TypeSyntax type,
            ITypeSymbol typeSymbol,
            SemanticModel semanticModel,
            string title = null,
            string equivalenceKey = null)
        {
            CodeAction codeAction = CodeActionFactory.ChangeType(context.Document, type, typeSymbol, semanticModel, title, equivalenceKey ?? RefactoringIdentifiers.ChangeVarToExplicitType);

            context.RegisterRefactoring(codeAction);
        }
    }
}
