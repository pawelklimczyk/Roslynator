// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Roslynator.CSharp.Refactorings
{
    internal static class TupleTypeRefactoring
    {
        public static void ComputeRefactorings(RefactoringContext context, TupleTypeSyntax tupleType)
        {
            if (context.IsRefactoringEnabled(RefactoringIdentifiers.ReplaceTupleWithStruct)
                && context.Span.IsBetweenSpans(tupleType))
            {
                ReplaceTupleWithStructRefactoring.ComputeRefactoring(context, tupleType);
            }
        }
    }
}
