// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslynator.CSharp.CodeFixes;
using Xunit;

namespace Roslynator.CSharp.Analysis.Tests
{
    public class RCS1238OptimizeMethodCallTests : AbstractCSharpCodeFixVerifier
    {
        public override DiagnosticDescriptor Descriptor { get; } = DiagnosticDescriptors.OptimizeMethodCall;

        public override DiagnosticAnalyzer Analyzer { get; } = new InvocationExpressionAnalyzer();

        public override CodeFixProvider FixProvider { get; } = new OptimizeMethodCallCodeFixProvider();

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.OptimizeMethodCall)]
        public async Task Test_ReplaceCompareWithCompareOrdinal()
        {
            await VerifyDiagnosticAndFixAsync(@"
using System;

class C
{
    void M()
    {
        string x = null;
        string y = null;

        var result = [|string.Compare(x, y, StringComparison.Ordinal)|];
    }
}
", @"
using System;

class C
{
    void M()
    {
        string x = null;
        string y = null;

        var result = string.CompareOrdinal(x, y);
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.OptimizeMethodCall)]
        public async Task Test_ReplaceCompareWithEquals()
        {
            await VerifyDiagnosticAndFixAsync(@"
using System;

class C
{
    void M()
    {
        string x = null;
        string y = null;

        if ([|string.Compare(x, y, StringComparison.Ordinal) == 0|])
        {
        }
    }
}
", @"
using System;

class C
{
    void M()
    {
        string x = null;
        string y = null;

        if (string.Equals(x, y, StringComparison.Ordinal))
        {
        }
    }
}
");
        }
    }
}
