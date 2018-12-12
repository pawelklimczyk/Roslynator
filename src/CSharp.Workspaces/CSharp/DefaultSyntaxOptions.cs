// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp;

namespace Roslynator.CSharp
{
    [Flags]
    internal enum DefaultSyntaxOptions
    {
        /// <summary>
        /// No option specified.
        /// </summary>
        None = 0,

        /// <summary>
        /// Always use <see cref="SyntaxKind.DefaultExpression"/>.
        /// </summary>
        UseDefaultExpression = 1,

        /// <summary>
        /// Always use <see cref="SyntaxKind.DefaultLiteralExpression"/>.
        /// </summary>
        UseDefaultLiteral = 2,
    }
}
