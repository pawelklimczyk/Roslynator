// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Roslynator.CSharp.Refactorings.Tests
{
    public class RRX007JoinIfStatementsTests : AbstractCSharpCodeRefactoringVerifier
    {
        public override string RefactoringId { get; } = RefactoringIdentifiers.JoinIfStatements;

        [Fact, Trait(Traits.Refactoring, RefactoringIdentifiers.JoinIfStatements)]
        public async Task Test()
        {
            await VerifyRefactoringAsync(@"
class C
{
    int M()
    {
        bool f1 = false;

[|        if (f1)
            return 1;

        if (f1)
        {
            return 2;
        }
        else if (f1)
        {
            return 3;
        }

        if (f1)
        {
            return 4;
        }|]

        return 0;
    }
}
", @"
class C
{
    int M()
    {
        bool f1 = false;

        if (f1)
            return 1;
        else if (f1)
        {
            return 2;
        }
        else if (f1)
        {
            return 3;
        }
        else if (f1)
        {
            return 4;
        }

        return 0;
    }
}
", equivalenceKey: RefactoringId);
        }

        [Fact, Trait(Traits.Refactoring, RefactoringIdentifiers.JoinIfStatements)]
        public async Task Test_IfElse()
        {
            await VerifyRefactoringAsync(@"
class C
{
    int M()
    {
        bool f1 = false;

[|        if (f1)
            return 1;

        return 0;|]
    }
}
", @"
class C
{
    int M()
    {
        bool f1 = false;

        if (f1)
            return 1;
        else
            return 0;
    }
}
", equivalenceKey: RefactoringId);
        }
    }
}
