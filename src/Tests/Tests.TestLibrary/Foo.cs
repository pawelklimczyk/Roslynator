// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#region usings
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslynator;
using Roslynator.CSharp;
using Roslynator.CSharp.Syntax;
using System.Diagnostics.CodeAnalysis;
using System.Collections;
#endregion usings

#pragma warning disable RCS1079, RCS1024, CA1822, RCS1169, IDE0044

namespace Roslynator.Tests
{
    public class C<T> : IEnumerable<T>
    {
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new EnumeratorImpl(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new EnumeratorImpl(this);
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
}
