// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Roslynator.CodeAnalysis.CSharp.Tests
{
    public class ROS0004UsePatternMatchingTests : AbstractCSharpCodeFixVerifier
    {
        public override DiagnosticDescriptor Descriptor { get; } = DiagnosticDescriptors.UsePatternMatching;

        public override DiagnosticAnalyzer Analyzer { get; } = new SwitchStatementAnalyzer();

        public override CodeFixProvider FixProvider { get; } = new SwitchStatementCodeFixProvider();

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.UsePatternMatching)]
        public async Task Test()
        {
            await VerifyDiagnosticAndFixAsync(@"
using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

class C
{
    void M()
    {
        SyntaxNode node = null;

        [|switch|] (node.Kind())
        {
            case SyntaxKind.IdentifierName:
                {
                    var identifierName = (IdentifierNameSyntax)node;
                    break;
                }
            case SyntaxKind.GenericName:
                var genericName = (GenericNameSyntax)node;
                break;
            default:
                {
                    throw new InvalidOperationException();
                }
        }
    }
}
", @"
using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

class C
{
    void M()
    {
        SyntaxNode node = null;

        switch (node)
        {
            case IdentifierNameSyntax identifierName:
                {
                    break;
                }

            case GenericNameSyntax genericName:
                break;
            default:
                {
                    throw new InvalidOperationException();
                }
        }
    }
}
");
        }
    }
}
