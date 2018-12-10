// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Roslynator.CSharp.Analysis.Tests
{
    public class RCSX005ReorderElementsInDocumentationCommentTests : AbstractCSharpCodeFixVerifier
    {
        public override DiagnosticDescriptor Descriptor { get; } = DiagnosticDescriptors.ReorderElementsInDocumentationComment;

        public override DiagnosticAnalyzer Analyzer { get; } = new SingleLineDocumentationCommentTriviaAnalyzer();

        public override CodeFixProvider FixProvider { get; }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.ReorderElementsInDocumentationComment)]
        public async Task Test()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    /// <summary></summary>
    /// <param name=""c""></param>
    /// <param name=""b""></param>
    /// <param name=""a""></param>
    void M(object a, object b, object c)
    {
    }
}
", @"
class C
{
    /// <summary></summary>
    /// <param name=""a""></param>
    /// <param name=""b""></param>
    /// <param name=""c""></param>
    void M(object a, object b, object c)
    {
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.ReorderElementsInDocumentationComment)]
        public async Task Test2()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    /// <summary></summary>
    /// <param name=""b""></param>
    /// <param name=""a""></param>
    /// <param name=""c""></param>
    void M(object a, object b, object c)
    {
    }
}
", @"
class C
{
    /// <summary></summary>
    /// <param name=""a""></param>
    /// <param name=""b""></param>
    /// <param name=""c""></param>
    void M(object a, object b, object c)
    {
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.ReorderElementsInDocumentationComment)]
        public async Task Test3()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    /// <summary></summary>
    /// <param name=""a""></param>
    /// <param name=""c""></param>
    /// <param name=""b""></param>
    void M(object a, object b, object c)
    {
    }
}
", @"
class C
{
    /// <summary></summary>
    /// <param name=""a""></param>
    /// <param name=""b""></param>
    /// <param name=""c""></param>
    void M(object a, object b, object c)
    {
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.ReorderElementsInDocumentationComment)]
        public async Task Test4()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    /// <summary></summary>
    /// <param name=""c""></param>
    /// <param name=""b""></param>
    void M(object a, object b, object c)
    {
    }
}
", @"
class C
{
    /// <summary></summary>
    /// <param name=""b""></param>
    /// <param name=""c""></param>
    void M(object a, object b, object c)
    {
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.ReorderElementsInDocumentationComment)]
        public async Task TestNoDiagnostic_Sorted()
        {
            await VerifyNoDiagnosticAsync(@"
class C
{
    /// <summary></summary>
    /// <param name=""a""></param>
    /// <param name=""b""></param>
    /// <param name=""c""></param>
    void M(object a, object b, object c)
    {
    }
}
");
        }
    }
}
