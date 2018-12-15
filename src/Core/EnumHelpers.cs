// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;

namespace Roslynator
{
    internal static class EnumHelpers
    {
        public static bool IsAllowedValue(ulong value, SpecialType specialType)
        {
            switch (specialType)
            {
                case SpecialType.System_SByte:
                    return value <= (ulong)sbyte.MaxValue;
                case SpecialType.System_Byte:
                    return value <= byte.MaxValue;
                case SpecialType.System_Int16:
                    return value <= (ulong)short.MaxValue;
                case SpecialType.System_UInt16:
                    return value <= ushort.MaxValue;
                case SpecialType.System_Int32:
                    return value <= int.MaxValue;
                case SpecialType.System_UInt32:
                    return value <= uint.MaxValue;
                case SpecialType.System_Int64:
                    return value <= long.MaxValue;
                case SpecialType.System_UInt64:
                    return true;
                default:
                    throw new ArgumentException("", nameof(specialType));
            }
        }
    }
}