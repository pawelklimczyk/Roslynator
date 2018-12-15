// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Roslynator.CSharp.Refactorings.Tests
{
    public class RR0057GenerateEnumValuesTests : AbstractCSharpCodeRefactoringVerifier
    {
        public override string RefactoringId { get; } = RefactoringIdentifiers.GenerateEnumValues;

        [Fact, Trait(Traits.Refactoring, RefactoringIdentifiers.GenerateEnumValues)]
        public async Task Test()
        {
            await VerifyRefactoringAsync(@"
using System;

[Flags]
enum Foo
{
    None = 0,
    A,
    B,
    [||]C,
}
", @"
using System;

[Flags]
enum Foo
{
    None = 0,
    A = 1,
    B = 2,
    C = 4,
}
", equivalenceKey: RefactoringId);
        }
    }
}
