// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Roslynator.CSharp.Refactorings.Tests
{
    public class RR9001ImplementCustomEnumeratorTests : AbstractCSharpCodeRefactoringVerifier
    {
        public override string RefactoringId { get; } = RefactoringIdentifiers.ImplementCustomEnumerator;

        [Fact, Trait(Traits.Refactoring, RefactoringIdentifiers.ImplementCustomEnumerator)]
        public async Task Test()
        {
            await VerifyRefactoringAsync(@"
using System;
using System.Collections;
using System.Collections.Generic;

class [||]C<T> : IEnumerable<T>
{
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        throw new System.NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
}
", @"
using System;
using System.Collections;
using System.Collections.Generic;

class C<T> : IEnumerable<T>
{
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        throw new System.NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new System.NotImplementedException();
    }

    public struct Enumerator
    {
        private readonly C<T> _c;
        private int _index;

        internal Enumerator(C<T> c)
        {
            _c = c;
            _index = -1;
        }

        public bool MoveNext()
        {
            throw new System.NotImplementedException();
        }

        public T Current
        {
            get
            {
                throw new System.NotImplementedException();
            }
        }

        public void Reset()
        {
            throw new System.NotImplementedException();
        }

        public override bool Equals(object obj)
        {
            throw new System.NotSupportedException();
        }

        public override int GetHashCode()
        {
            throw new System.NotSupportedException();
        }
    }

    private class EnumeratorImpl : IEnumerator<T>
    {
        private Enumerator _e;

        internal EnumeratorImpl(C<T> c)
        {
            _e = new Enumerator(c);
        }

        public T Current
        {
            get
            {
                return _e.Current;
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return _e.Current;
            }
        }

        public bool MoveNext()
        {
            return _e.MoveNext();
        }

        void IEnumerator.Reset()
        {
            _e.Reset();
        }

        void IDisposable.Dispose()
        {
        }
    }
}
", equivalenceKey: RefactoringId);
        }

        //[Theory, Trait(Traits.Refactoring, RefactoringIdentifiers.ImplementCustomEnumerator)]
        //[InlineData("", "")]
        public async Task Test2(string fromData, string toData)
        {
            await VerifyRefactoringAsync(@"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class C
{
    void M()
    {
    }
}
", fromData, toData, equivalenceKey: RefactoringId);
        }

        //[Fact, Trait(Traits.Refactoring, RefactoringIdentifiers.ImplementCustomEnumerator)]
        public async Task TestNoRefactoring()
        {
            await VerifyNoRefactoringAsync(@"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class C
{
    void M()
    {
    }
}
", equivalenceKey: RefactoringId);
        }
    }
}
