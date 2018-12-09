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
        /// Always use <see cref="SyntaxKind.DefaultExpression"/> or <see cref="SyntaxKind.DefaultLiteralExpression"/>.
        /// </summary>
        AlwaysUseDefault = 1,

        /// <summary>
        /// Prefer <see cref="SyntaxKind.DefaultLiteralExpression"/> over <see cref="SyntaxKind.DefaultExpression"/>. This option is relevant only in combination with <see cref="AlwaysUseDefault"/>.
        /// </summary>
        PreferDefaultLiteral = 2,

        /// <summary>
        /// Enum default value should be displayed as '0' and not as enum member whose value is equal to 0.
        /// </summary>
        EnumAlwaysAsNumber = 4,
    }
}
