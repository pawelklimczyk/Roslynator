﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Pihrtsoft.CodeAnalysis.CSharp.Refactorings
{
    internal static class BlockRefactoring
    {
        public static async Task ComputeRefactoringAsync(RefactoringContext context, BlockSyntax block)
        {
            RemoveBracesRefactoring.ComputeRefactoring(context, block);

            if (SelectedStatementsRefactoring.IsAnyRefactoringEnabled(context))
            {
                SelectedStatementsInfo info = SelectedStatementsInfo.Create(block, context.Span);
                await SelectedStatementsRefactoring.ComputeRefactoringAsync(context, info);
            }
        }
    }
}
