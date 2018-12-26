// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslynator.CSharp.CodeFixes;
using Xunit;

namespace Roslynator.CSharp.Analysis.Tests
{
    public class RCS1234RemoveUnnecessaryNewLineTests : AbstractCSharpCodeFixVerifier
    {
        public override DiagnosticDescriptor Descriptor { get; } = DiagnosticDescriptors.RemoveUnnecessaryNewLine;

        public override DiagnosticAnalyzer Analyzer { get; } = new FormattingAnalyzer();

        public override CodeFixProvider FixProvider { get; } = new TokenCodeFixProvider();

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.RemoveUnnecessaryNewLine)]
        public async Task Test_Class()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
}   
    [|;|]
", @"
class C
{
};
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.RemoveUnnecessaryNewLine)]
        public async Task Test_LocalDeclarationStatement()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M()
    {
        string s = null
[|;|]
    }
}   
", @"
class C
{
    void M()
    {
        string s = null;
    }
}   
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.RemoveUnnecessaryNewLine)]
        public async Task Test_NameColon()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M(string p)
    {
        M(p
[|:|] null);
    }
}   
", @"
class C
{
    void M(string p)
    {
        M(p: null);
    }
}   
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.RemoveUnnecessaryNewLine)]
        public async Task TestNoDiagnostic_NoSemicolon()
        {
            await VerifyNoDiagnosticAsync(@"
class C
{
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.RemoveUnnecessaryNewLine)]
        public async Task TestNoDiagnostic_NoNewLine()
        {
            await VerifyNoDiagnosticAsync(@"
class C
{
};
");
        }
    }
}
