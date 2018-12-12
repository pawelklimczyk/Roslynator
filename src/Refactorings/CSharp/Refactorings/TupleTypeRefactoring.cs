// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading.Tasks;

namespace Roslynator.CSharp.Refactorings
{
    internal static class TupleTypeRefactoring
    {
        public static async Task ComputeRefactoringsAsync(RefactoringContext context, TupleTypeSyntax tupleType)
        {
            if (context.IsRefactoringEnabled(RefactoringIdentifiers.GenerateStructFromTuple)
                && context.Span.IsBetweenSpans(tupleType))
            {
                await GenerateStructFromTupleRefactoring.ComputeRefactoringAsync(context, tupleType).ConfigureAwait(false);
            }
        }
    }
}
