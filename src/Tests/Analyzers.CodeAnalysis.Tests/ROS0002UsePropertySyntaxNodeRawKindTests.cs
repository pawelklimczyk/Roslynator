// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Roslynator.CodeAnalysis.CSharp.Tests
{
    public class ROS0002UsePropertySyntaxNodeRawKindTests : AbstractCSharpCodeFixVerifier
    {
        public override DiagnosticDescriptor Descriptor { get; } = DiagnosticDescriptors.UsePropertySyntaxNodeRawKind;

        public override DiagnosticAnalyzer Analyzer { get; } = new UsePropertySyntaxNodeRawKindAnalyzer();

        public override CodeFixProvider FixProvider { get; } = new EqualsExpressionCodeFixProvider();

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.UsePropertySyntaxNodeRawKind)]
        public async Task Test_EqualsExpression()
        {
            await VerifyDiagnosticAndFixAsync(@"
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

class C
{
    void M()
    {
        SyntaxNode n1 = null;
        SyntaxNode n2 = null;

        if ([|n1.Kind() == n2.Kind()|]) { }
    }
}
", @"
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

class C
{
    void M()
    {
        SyntaxNode n1 = null;
        SyntaxNode n2 = null;

        if (n1.RawKind == n2.RawKind) { }
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.UsePropertySyntaxNodeRawKind)]
        public async Task Test_NotEqualsExpression()
        {
            await VerifyDiagnosticAndFixAsync(@"
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

class C
{
    void M()
    {
        SyntaxNode n1 = null;
        SyntaxNode n2 = null;

        if ([|n1.Kind() != n2.Kind()|]) { }
    }
}
", @"
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

class C
{
    void M()
    {
        SyntaxNode n1 = null;
        SyntaxNode n2 = null;

        if (n1.RawKind != n2.RawKind) { }
    }
}
");
        }
    }
}
