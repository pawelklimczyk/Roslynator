// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Roslynator.CSharp.CSharpFactory;

namespace Roslynator.CSharp
{
    internal static class WorkspaceSymbolExtensions
    {
        // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/default-values-table
        /// <summary>
        /// Creates a new <see cref="ExpressionSyntax"/> that represents default value of the specified type symbol.
        /// </summary>
        /// <param name="typeSymbol"></param>
        /// <param name="type"></param>
        /// <param name="options"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static ExpressionSyntax GetDefaultValueSyntax(
            this ITypeSymbol typeSymbol,
            DefaultSyntaxOptions options = DefaultSyntaxOptions.None,
            TypeSyntax type = null,
            SymbolDisplayFormat format = null)
        {
            if (typeSymbol == null)
                throw new ArgumentNullException(nameof(typeSymbol));

            if (typeSymbol.IsReferenceTypeOrNullableType())
            {
                return ((options & DefaultSyntaxOptions.AlwaysUseDefault) != 0)
                    ? CreateDefault()
                    : NullLiteralExpression();
            }

            if (typeSymbol.TypeKind == TypeKind.Enum)
            {
                if ((options & DefaultSyntaxOptions.AlwaysUseDefault) != 0)
                    return CreateDefault();

                if ((options & DefaultSyntaxOptions.EnumAlwaysAsNumber) == 0)
                {
                    IFieldSymbol fieldSymbol = CSharpUtility.FindEnumDefaultField((INamedTypeSymbol)typeSymbol);

                    if (fieldSymbol != null)
                    {
                        type = type ?? typeSymbol.ToTypeSyntax(format).WithFormatterAnnotation();

                        return SimpleMemberAccessExpression(type, IdentifierName(fieldSymbol.Name));
                    }
                }

                return NumericLiteralExpression(0);
            }

            switch (typeSymbol.SpecialType)
            {
                case SpecialType.System_Boolean:
                    {
                        return ((options & DefaultSyntaxOptions.AlwaysUseDefault) != 0)
                            ? CreateDefault()
                            : FalseLiteralExpression();
                    }
                case SpecialType.System_Char:
                    {
                        return ((options & DefaultSyntaxOptions.AlwaysUseDefault) != 0)
                            ? CreateDefault()
                            : CharacterLiteralExpression('\0');
                    }
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Decimal:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                    {
                        return ((options & DefaultSyntaxOptions.AlwaysUseDefault) != 0)
                            ? CreateDefault()
                            : NumericLiteralExpression(0);
                    }
            }

            return CreateDefault();

            ExpressionSyntax CreateDefault()
            {
                if ((options & DefaultSyntaxOptions.PreferDefaultLiteral) != 0)
                {
                    return DefaultLiteralExpression();
                }
                else
                {
                    type = type ?? typeSymbol.ToTypeSyntax(format).WithSimplifierAnnotation();

                    return DefaultExpression(type);
                }
            }
        }
    }
}
