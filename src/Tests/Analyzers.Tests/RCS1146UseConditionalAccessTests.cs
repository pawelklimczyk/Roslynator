﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslynator.CSharp.CodeFixes;
using Xunit;

#pragma warning disable RCS1090

namespace Roslynator.CSharp.Analysis.Tests
{
    public class RCS1146UseConditionalAccessTests : AbstractCSharpCodeFixVerifier
    {
        public override DiagnosticDescriptor Descriptor { get; } = DiagnosticDescriptors.UseConditionalAccess;

        public override DiagnosticAnalyzer Analyzer { get; } = new UseConditionalAccessAnalyzer();

        public override CodeFixProvider FixProvider { get; } = new UseConditionalAccessCodeFixProvider();

        [Fact]
        public async Task Test_IfStatement_ReferenceType()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M()
    {
        C x = null;

        [|if (x != null)
            x.M();|]

        [|if (x != null)
        {
            x.M();
        }|]
    }
}
", @"
class C
{
    void M()
    {
        C x = null;

        x?.M();

        x?.M();
    }
}
");
        }

        [Fact]
        public async Task Test_IfStatement_ValueType()
        {
            await VerifyDiagnosticAndFixAsync(@"
struct S
{
    void M()
    {
        S? x = null;

        [|if (x != null)
            x.Value.M();|]

        [|if (x != null)
        {
            x.Value.M();
        }|]
    }
}
", @"
struct S
{
    void M()
    {
        S? x = null;

        x?.M();

        x?.M();
    }
}
");
        }

        [Fact]
        public async Task Test_LogicalAnd_ReferenceType()
        {
            await VerifyDiagnosticAndFixAsync(@"
class Foo
{
    const string NonNullConst = ""x"";

    string P { get; }

    void M()
    {
        bool f = false;

        Foo x = null;

        if ([|x != null && x.Equals(x)|]) { }

        if ([|null != x && x.Equals(x)|]) { }

        if ([|x != null && (x.Equals(x)|])) { }

        if ([|x != null && x.Equals(x)|] && f) { }

        if (f && [|x != null && x.Equals(x)|]) { }

        if ([|x != null && x.P.Length > 1|]) { }

        if ([|x != null && !x.Equals(x)|]) { }

        if ([|x != null && (!x.Equals(x)|])) { }

        if ([|x != null && x.P == ""x""|]) { }

        if ([|x != null && x.P == NonNullConst|]) { }

        if ([|x != null && x.P != null|]) { }

        if ([|x != null && x.P is object|]) { }

        if ([|x != null && x.P is object _|]) { }

        if (f &&
     /*lt*/ [|x != null &&
            x.Equals(""x"")|] /*tt*/
            && f) { }
    }
}
", @"
class Foo
{
    const string NonNullConst = ""x"";

    string P { get; }

    void M()
    {
        bool f = false;

        Foo x = null;

        if (x?.Equals(x) == true) { }

        if (x?.Equals(x) == true) { }

        if ((x?.Equals(x) == true)) { }

        if (x?.Equals(x) == true && f) { }

        if (f && x?.Equals(x) == true) { }

        if (x?.P.Length > 1) { }

        if (x?.Equals(x) == false) { }

        if ((x?.Equals(x) == false)) { }

        if (x?.P == ""x"") { }

        if (x?.P == NonNullConst) { }

        if (x?.P != null) { }

        if (x?.P is object) { }

        if (x?.P is object _) { }

        if (f &&
     /*lt*/ x?.Equals(""x"") == true /*tt*/
            && f) { }
    }
}
");
        }

        [Fact]
        public async Task Test_LogicalOr_ReferenceType()
        {
            await VerifyDiagnosticAndFixAsync(@"
class Foo
{
    void M()
    {
        Foo x = null;

        if ([|x == null || x.Equals(x)|]) { }

        if ([|x == null || (x.Equals(x)|])) { }

        if ([|x == null || !x.Equals(x)|]) { }

        if ([|x == null || (!x.Equals(x)|])) { }
    }
}
", @"
class Foo
{
    void M()
    {
        Foo x = null;

        if (x?.Equals(x) != false) { }

        if ((x?.Equals(x) != false)) { }

        if (x?.Equals(x) != true) { }

        if ((x?.Equals(x) != true)) { }
    }
}
");
        }

        [Fact]
        public async Task Test_LogicalAnd_ElementAccess()
        {
            await VerifyDiagnosticAndFixAsync(@"
using System.Collections.Generic;

class Foo
{
    void M()
    {
        Dictionary<int, string> dic = null;

        if ([|dic != null && dic[0].Equals(""x"")|]) { }

        if ([|dic != null && dic[0].Length > 1|]) { }

        if ([|dic != null && !dic[0].Equals(""x"")|]) { }
    }
}
", @"
using System.Collections.Generic;

class Foo
{
    void M()
    {
        Dictionary<int, string> dic = null;

        if (dic?[0].Equals(""x"") == true) { }

        if (dic?[0].Length > 1) { }

        if (dic?[0].Equals(""x"") == false) { }
    }
}
");
        }

        [Fact]
        public async Task Test_LogicalAnd_Nested()
        {
            await VerifyDiagnosticAndFixAsync(@"
class Foo
{
    Foo M()
    {
        Foo x = null;

        if (x != null && x.M() != null && [|x.M().M2() != null && x.M().M2().M3() != null|]) { }

        if (x != null && (x.M() != null && ([|x.M().M2() != null && x.M().M2().M3() != null|]))) { }

        if (((x != null) && (x.M() != null)) && ((([|x.M().M2() != null)) && (x.M().M2().M3() != null|]))) { }

        return null;
    }

    Foo M2() => null;
    Foo M3() => null;
}
", @"
class Foo
{
    Foo M()
    {
        Foo x = null;

        if (x?.M()?.M2()?.M3() != null) { }

        if (((x?.M()?.M2()?.M3() != null))) { }

        if ((((x?.M()?.M2()?.M3() != null)))) { }

        return null;
    }

    Foo M2() => null;
    Foo M3() => null;
}
");
        }

        [Fact]
        public async Task Test_LogicalAnd_NullableType()
        {
            await VerifyDiagnosticAndFixAsync(@"
struct Foo
{
    const string NonNullConst = ""x"";

    string P { get; }

    void M()
    {
        Foo? x = null;

        if ([|x != null && x.Value.Equals(x)|]) { }

        if ([|x != null && x.Value.P.Length > 1|]) { }

        if ([|x != null && !x.Value.Equals(x)|]) { }

        if ([|x != null && x.Value.P == ""x""|]) { }

        if ([|x != null && x.Value.P == NonNullConst|]) { }

        if ([|x != null && x.Value.P != null|]) { }

        if ([|x != null && x.Value.P is object|]) { }

        if ([|x != null && x.Value.P is object _|]) { }

        if (x != null && [|x.Value.ToString() != null && x.Value.ToString().ToString() != null|]) { }
    }
}
", @"
struct Foo
{
    const string NonNullConst = ""x"";

    string P { get; }

    void M()
    {
        Foo? x = null;

        if (x?.Equals(x) == true) { }

        if (x?.P.Length > 1) { }

        if (x?.Equals(x) == false) { }

        if (x?.P == ""x"") { }

        if (x?.P == NonNullConst) { }

        if (x?.P != null) { }

        if (x?.P is object) { }

        if (x?.P is object _) { }

        if (x?.ToString()?.ToString() != null) { }
    }
}
");
        }

        [Fact]
        public async Task Test_LogicalOr_NullableType()
        {
            await VerifyDiagnosticAndFixAsync(@"
struct Foo
{
    void M()
    {
        Foo? x = null;

        if ([|x == null || x.Value.Equals(x)|]) { }

        if ([|x == null || !x.Value.Equals(x)|]) { }
    }
}
", @"
struct Foo
{
    void M()
    {
        Foo? x = null;

        if (x?.Equals(x) != false) { }

        if (x?.Equals(x) != true) { }
    }
}
");
        }

        [Fact]
        public async Task Test_LogicalAnd_Nested_NullableType()
        {
            await VerifyDiagnosticAndFixAsync(@"
struct Foo
{
    void M()
    {
        Foo? x = null;

        if (x != null
            && x.Value.ToString() != null
            && [|x.Value.ToString().ToString() != null
            && x.Value.ToString().ToString().ToString() != null|]) { }
    }
}
", @"
struct Foo
{
    void M()
    {
        Foo? x = null;

        if (x?.ToString()?.ToString()?.ToString() != null) { }
    }
}
");
        }

        [Fact]
        public async Task TestNoDiagnostic_LogicalAnd_ReferenceType()
        {
            await VerifyNoDiagnosticAsync(@"
class Foo
{
    const string NullConst = null;
    const string NonNullConst = ""x"";

    string P { get; }

    void M()
    {
        bool f = false;

        string s = null;

        Foo x = null;

        if (x != null && x.P == null && f) { }

        if (x != null && x.P == NullConst && f) { }

        if (x != null && x.P == s && f) { }

        if (x != null && x.P != ""x"" && f) { }

        if (x != null && x.P != NonNullConst && f) { }

        if (x != null && x.P != s && f) { }

        if (x != null && (x.P != null) is object _) { }
    }
}
");
        }

        [Fact]
        public async Task TestNoDiagnostic_LogicalOr_ReferenceType()
        {
            await VerifyNoDiagnosticAsync(@"
class Foo
{
    const string NullConst = null;
    const string NonNullConst = ""x"";

    string P { get; }

    void M()
    {
        bool f = false;

        string s = null;

        Foo x = null;


        if (x == null || x.P.Length > 1) { }

        if (x == null || x.P == ""x"") { }

        if (x == null || x.P == NonNullConst) { }

        if (x == null || x.P != null) { }

        if (x == null || x.P is object) { }

        if (x == null || x.P is object _) { }

        if (x == null || x.P == null && f) { }

        if (x == null || x.P == NullConst && f) { }

        if (x == null || x.P == s && f) { }

        if (x == null || x.P != ""x"" && f) { }

        if (x == null || x.P != NonNullConst && f) { }

        if (x == null || x.P != s && f) { }

        if (x == null || (x.P != null) is object _) { }
    }
}
");
        }

        [Fact]
        public async Task TestNoDiagnostic_LogicalAnd_ValueType()
        {
            await VerifyNoDiagnosticAsync(@"
struct Foo
{
    public int P { get; }

    void M()
    {
        var x = new Foo();

        if (x != null && x.P > 0) { }
    }

    public static bool operator ==(Foo left, Foo right) => left.Equals(right);
    public static bool operator !=(Foo left, Foo right) => !(left == right);
}
");
        }

        [Fact]
        public async Task TestNoDiagnostic_LogicalAnd_NullableType()
        {
            await VerifyNoDiagnosticAsync(@"
struct Foo
{
    void M()
    {
        bool? f = null;

        if (f != null && f.Value) { }

        if (f != null && f.Value && f.Value) { }

        if (f != null && (f.Value)) { }

        if (f != null && !f.Value) { }

        if (f != null && !f.Value && !f.Value) { }

        if (f != null && (!f.Value)) { }

        Foo? x = null;

        var value = default(Foo);

        if (x != null && x.Value == null) { }

        if (x != null && x.Value == value) { }

        if (x != null && x.Value != null) { }

        if (x != null && x.Value != null) { }

        if (x != null && x.Value != value) { }

        if (x != null && x.HasValue.Equals(true)) { }

        if (x != null && (x.Value == null) is object _) { }
    }

    public static bool operator ==(Foo left, Foo right) => left.Equals(right);
    public static bool operator !=(Foo left, Foo right) => !(left == right);
}
");
        }

        [Fact]
        public async Task TestNoDiagnostic_LogicalOr_NullableType()
        {
            await VerifyNoDiagnosticAsync(@"
struct Foo
{
    const string NonNullConst = ""x"";

    string P { get; }

    void M()
    {
        bool? f = null;

        if (f == null || f.Value) { }

        if (f == null || f.Value && f.Value) { }

        if (f == null || (f.Value)) { }

        if (f == null || !f.Value) { }

        if (f == null || !f.Value && !f.Value) { }

        if (f == null || (!f.Value)) { }

        Foo? x = null;

        var value = default(Foo);

        if (x == null || x.Value.P.Length > 1) { }

        if (x == null || x.Value.P == ""x"") { }

        if (x == null || x.Value.P == NonNullConst) { }

        if (x == null || x.Value.P != null) { }

        if (x == null || x.Value.P is object) { }

        if (x == null || x.Value.P is object _) { }

        if (x == null || x.Value.ToString() == null || x.Value.ToString().ToString() == null) { }


        if (x == null || x.Value == null) { }

        if (x == null || x.Value == value) { }

        if (x == null || x.Value != null) { }

        if (x == null || x.Value != null) { }

        if (x == null || x.Value != value) { }

        if (x == null || x.HasValue.Equals(true)) { }

        if (x == null || (x.Value == null) is object _) { }
    }

    public static bool operator ==(Foo left, Foo right) => left.Equals(right);
    public static bool operator !=(Foo left, Foo right) => !(left == right);
}
");
        }

        [Fact]
        public async Task TestNoDiagnostic_LogicalAnd_OutParameter()
        {
            await VerifyNoDiagnosticAsync(@"
using System.Collections.Generic;

class C
{
    void M()
    {
        Dictionary<int, string> dic = null;

        string value;

if (dic != null && dic.TryGetValue(0, out value)) { }

        if (dic != null && dic.TryGetValue(0, out string value2)) { }
    }
}
");
        }

        [Fact]
        public async Task TestNoDiagnostic_LogicalOr_OutParameter()
        {
            await VerifyNoDiagnosticAsync(@"
using System.Collections.Generic;

class C
{
    void M()
    {
        Dictionary<int, string> dic = null;

        string value;

        if (dic == null || dic.TryGetValue(0, out value)) { }

        if (dic == null || dic.TryGetValue(0, out string value2)) { }
    }
}
");
        }

        [Fact]
        public async Task TestNoDiagnostic_LogicalAnd_ExpressionTree()
        {
            await VerifyNoDiagnosticAsync(@"
using System;
using System.Linq.Expressions;

class C
{
    public void M<T>(Expression<Func<T>> expression)
    {
        string s = null;

        M(() => s != null && s.GetHashCode() == 0);
    }
}
");
        }

        [Fact]
        public async Task TestNoDiagnostic_LogicalOr_ExpressionTree()
        {
            await VerifyNoDiagnosticAsync(@"
using System;
using System.Linq.Expressions;

class C
{
    public void M<T>(Expression<Func<T>> expression)
    {
        string s = null;

        M(() => s == null || s.Equals(s));
    }
}
");
        }

        [Fact]
        public async Task TestNoDiagnostic_LogicalOr_ExpressionTree()
        {
            await VerifyNoDiagnosticAsync(@"
using System;
using System.Linq.Expressions;

class C
{
    public void M<T>(Expression<Func<T>> expression)
    {
        string s = null;

        M(() => s == null || s.Equals(s));
    }
}
");
        }
    }
}
