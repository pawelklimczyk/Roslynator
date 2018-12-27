// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Roslynator.Tests;
using Xunit;

namespace Roslynator.CSharp.Refactorings.Tests
{
    public class RR0211GenerateMissingSwitchSectionsTests : AbstractCSharpCodeRefactoringVerifier
    {
        public override string RefactoringId { get; } = RefactoringIdentifiers.GenerateMissingSwitchSections;

        public override CodeVerificationOptions Options => base.Options.AddAllowedCompilerDiagnosticId(CompilerDiagnosticIdentifiers.EmptySwitchBlock);

        [Fact, Trait(Traits.Refactoring, RefactoringIdentifiers.GenerateMissingSwitchSections)]
        public async Task Test()
        {
            await VerifyRefactoringAsync(@"
using System;

class C
{
    void M()
    {
        DayOfWeek dayOfWeek = DayOfWeek.Monday;

        [||]switch (dayOfWeek)
        {
            case (DayOfWeek.Friday):
                break;
            case DayOfWeek.Monday:
                break;
            case DayOfWeek.Saturday:
                break;
            case (DayOfWeek)0:
                break;
            case DayOfWeek.Thursday:
                break;
        }
    }
}
", @"
using System;

class C
{
    void M()
    {
        DayOfWeek dayOfWeek = DayOfWeek.Monday;

        switch (dayOfWeek)
        {
            case (DayOfWeek.Friday):
                break;
            case DayOfWeek.Monday:
                break;
            case DayOfWeek.Saturday:
                break;
            case (DayOfWeek)0:
                break;
            case DayOfWeek.Thursday:
                break;
            case DayOfWeek.Tuesday:
                break;
            case DayOfWeek.Wednesday:
                break;
        }
    }
}
", equivalenceKey: RefactoringId);
        }

        [Fact, Trait(Traits.Refactoring, RefactoringIdentifiers.GenerateMissingSwitchSections)]
        public async Task Test_Flags()
        {
            await VerifyRefactoringAsync(@"
using System.Text.RegularExpressions;

class C
{
    void M()
    {
        RegexOptions options = RegexOptions.None;

        [||]switch (options)
        {
            case (RegexOptions.Compiled):
                break;
            case RegexOptions.CultureInvariant | RegexOptions.ECMAScript:
                break;
            case (RegexOptions)256:
                break;
            case System.Text.RegularExpressions.RegexOptions.ExplicitCapture:
                break;
            case RegexOptions.IgnoreCase:
                break;
            case RegexOptions.IgnorePatternWhitespace:
                break;
            case RegexOptions.Multiline:
                break;
            case RegexOptions.None:
                break;
            case RegexOptions.RightToLeft:
                break;
            case RegexOptions.Singleline:
                break;
            default:
                break;
        }
    }
}
", @"
using System.Text.RegularExpressions;

class C
{
    void M()
    {
        RegexOptions options = RegexOptions.None;

        switch (options)
        {
            case (RegexOptions.Compiled):
                break;
            case RegexOptions.CultureInvariant | RegexOptions.ECMAScript:
                break;
            case (RegexOptions)256:
                break;
            case System.Text.RegularExpressions.RegexOptions.ExplicitCapture:
                break;
            case RegexOptions.IgnoreCase:
                break;
            case RegexOptions.IgnorePatternWhitespace:
                break;
            case RegexOptions.Multiline:
                break;
            case RegexOptions.None:
                break;
            case RegexOptions.RightToLeft:
                break;
            case RegexOptions.Singleline:
                break;
            case RegexOptions.CultureInvariant:
                break;
            default:
                break;
        }
    }
}
", equivalenceKey: RefactoringId);
        }

        [Fact, Trait(Traits.Refactoring, RefactoringIdentifiers.GenerateMissingSwitchSections)]
        public async Task TestNoRefactoring_Empty()
        {
            await VerifyNoRefactoringAsync(@"
using System;

class C
{
    void M()
    {
        DayOfWeek dayOfWeek = DayOfWeek.Monday;

        [||]switch (dayOfWeek)
        {
        }
    }
}
", equivalenceKey: RefactoringId);
        }

        [Fact, Trait(Traits.Refactoring, RefactoringIdentifiers.GenerateMissingSwitchSections)]
        public async Task TestNoRefactoring_ContainsOnlyDefaultSection()
        {
            await VerifyNoRefactoringAsync(@"
using System;

class C
{
    void M()
    {
        DayOfWeek dayOfWeek = DayOfWeek.Monday;

        [||]switch (dayOfWeek)
        {
            default:
                break;
        }
    }
}
", equivalenceKey: RefactoringId);
        }

        [Fact, Trait(Traits.Refactoring, RefactoringIdentifiers.GenerateMissingSwitchSections)]
        public async Task TestNoRefactoring()
        {
            await VerifyNoRefactoringAsync(@"
using System;

class C
{
    void M()
    {
        DayOfWeek dayOfWeek = DayOfWeek.Monday;

        [||]switch (dayOfWeek)
        {
            case DayOfWeek.Friday:
                break;
            case DayOfWeek.Monday:
                break;
            case DayOfWeek.Saturday:
                break;
            case DayOfWeek.Sunday:
                break;
            case DayOfWeek.Thursday:
                break;
            case DayOfWeek.Tuesday:
                break;
            case DayOfWeek.Wednesday:
                break;
            default:
                break;
        }
    }
}
", equivalenceKey: RefactoringId);
        }
    }
}
