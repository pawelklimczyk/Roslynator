﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
#endregion usings

#pragma warning disable RCS1079, RCS1024, CA1822, RCS1169, IDE0044, RCS1213, RCS1018

namespace Roslynator.Tests
{
    class C
    {
        void M()
        {
        }
    }
}
