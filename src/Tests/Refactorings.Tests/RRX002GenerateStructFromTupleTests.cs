// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Roslynator.CSharp.Refactorings.Tests
{
    public class RRX002GenerateStructFromTupleTests : AbstractCSharpCodeRefactoringVerifier
    {
        public override string RefactoringId { get; } = RefactoringIdentifiers.GenerateStructFromTuple;

        [Fact, Trait(Traits.Refactoring, RefactoringIdentifiers.GenerateStructFromTuple)]
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

internal struct MyStruct
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

        [Fact, Trait(Traits.Refactoring, RefactoringIdentifiers.GenerateStructFromTuple)]
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
    internal struct MyStruct
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

        [Fact, Trait(Traits.Refactoring, RefactoringIdentifiers.GenerateStructFromTuple)]
        public async Task Test_Nested()
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
    class C
    {
        MyStruct M()
        {
            return new MyStruct(null, DateTime.Now);
        }

        private struct MyStruct
        {
            public MyStruct(object value, DateTime date)
            {
                Value = value;
                Date = date;
            }

            public object Value { get; }
            public DateTime Date { get; }
        }
    }
}
", equivalenceKey: EquivalenceKey.Join(RefactoringId, "Nested"));
        }

        [Fact, Trait(Traits.Refactoring, RefactoringIdentifiers.GenerateStructFromTuple)]
        public async Task Test_IEnumerableOfTuple()
        {
            await VerifyRefactoringAsync(@"
using System;
using System.Collections.Generic;

namespace N
{
    class C
    {
        IEnumerable<[|(object value, DateTime date)|]> M()
        {
            yield return (null, DateTime.Now);
        }
    }
}
", @"
using System;
using System.Collections.Generic;

namespace N
{
    internal struct MyStruct
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
        IEnumerable<MyStruct> M()
        {
            yield return new MyStruct(null, DateTime.Now);
        }
    }
}
", equivalenceKey: RefactoringId);
        }

        [Fact, Trait(Traits.Refactoring, RefactoringIdentifiers.GenerateStructFromTuple)]
        public async Task Test_TaskOfTuple()
        {
            await VerifyRefactoringAsync(@"
using System;
using System.Threading.Tasks;

namespace N
{
    class C
    {
        async Task<[|(object value, DateTime date)|]> M()
        {
            await Task.CompletedTask;
            return (null, DateTime.Now);
        }
    }
}
", @"
using System;
using System.Threading.Tasks;

namespace N
{
    internal struct MyStruct
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
        async Task<MyStruct> M()
        {
            await Task.CompletedTask;
            return new MyStruct(null, DateTime.Now);
        }
    }
}
", equivalenceKey: RefactoringId);
        }
    }
}
