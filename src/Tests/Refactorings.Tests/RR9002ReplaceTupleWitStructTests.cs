// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Roslynator.CSharp.Refactorings.Tests
{
    public class RR9002ReplaceTupleWitStructTests : AbstractCSharpCodeRefactoringVerifier
    {
        public override string RefactoringId { get; } = RefactoringIdentifiers.ReplaceTupleWithStruct;

        [Fact, Trait(Traits.Refactoring, RefactoringIdentifiers.ReplaceTupleWithStruct)]
        public async Task Test_CompilationUnit()
        {
            await VerifyRefactoringAsync(@"
using System;

class C
{
    [|(object value, DateTime date)|] M()
    {
        bool f = false;

        if (f)
        {
            return (null, DateTime.Now);
        }
        else
        {
            return default((object, DateTime));
        }
    }
}
", @"
using System;

public struct MyStruct
{
    public MyStruct(object value, DateTime date)
    {
        Value = value;
        Date = date;
    }

    public object Value { get; }
    public DateTime Date { get; }
}

class C
{
    MyStruct M()
    {
        bool f = false;

        if (f)
        {
            return new MyStruct(null, DateTime.Now);
        }
        else
        {
            return default(MyStruct);
        }
    }
}
", equivalenceKey: RefactoringId);
        }

        [Fact, Trait(Traits.Refactoring, RefactoringIdentifiers.ReplaceTupleWithStruct)]
        public async Task Test_Namespace()
        {
            await VerifyRefactoringAsync(@"
using System;

namespace N
{
    class C
    {
        [|(object value, DateTime date)|] M()
        {
            return (null, DateTime.Now);
        }
    }
}
", @"
using System;

namespace N
{
    public struct MyStruct
    {
        public MyStruct(object value, DateTime date)
        {
            Value = value;
            Date = date;
        }

        public object Value { get; }
        public DateTime Date { get; }
    }

    class C
    {
        MyStruct M()
        {
            return new MyStruct(null, DateTime.Now);
        }
    }
}
", equivalenceKey: RefactoringId);
        }
    }
}
