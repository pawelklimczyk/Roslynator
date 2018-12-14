// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Roslynator
{
    internal static class FlagsUtility
    {
        public static Optional<object> GetUniquePowerOfTwo(
            SpecialType underlyingType,
            IEnumerable<object> reservedValues,
            bool startFromHighestExistingValue = false)
        {
            switch (underlyingType)
            {
                case SpecialType.System_SByte:
                    {
                        Optional<sbyte> result = FlagsUtility<sbyte>.Instance.GetUniquePowerOfTwo(reservedValues.Cast<sbyte>(), startFromHighestExistingValue);

                        return (result.HasValue) ? result.Value : default(Optional<object>);
                    }
                case SpecialType.System_Byte:
                    {
                        Optional<byte> result = FlagsUtility<byte>.Instance.GetUniquePowerOfTwo(reservedValues.Cast<byte>(), startFromHighestExistingValue);

                        return (result.HasValue) ? result.Value : default(Optional<object>);
                    }
                case SpecialType.System_Int16:
                    {
                        Optional<short> result = FlagsUtility<short>.Instance.GetUniquePowerOfTwo(reservedValues.Cast<short>(), startFromHighestExistingValue);

                        return (result.HasValue) ? result.Value : default(Optional<object>);
                    }
                case SpecialType.System_UInt16:
                    {
                        Optional<ushort> result = FlagsUtility<ushort>.Instance.GetUniquePowerOfTwo(reservedValues.Cast<ushort>(), startFromHighestExistingValue);

                        return (result.HasValue) ? result.Value : default(Optional<object>);
                    }
                case SpecialType.System_Int32:
                    {
                        Optional<int> result = FlagsUtility<int>.Instance.GetUniquePowerOfTwo(reservedValues.Cast<int>(), startFromHighestExistingValue);

                        return (result.HasValue) ? result.Value : default(Optional<object>);
                    }
                case SpecialType.System_UInt32:
                    {
                        Optional<uint> result = FlagsUtility<uint>.Instance.GetUniquePowerOfTwo(reservedValues.Cast<uint>(), startFromHighestExistingValue);

                        return (result.HasValue) ? result.Value : default(Optional<object>);
                    }
                case SpecialType.System_Int64:
                    {
                        Optional<long> result = FlagsUtility<long>.Instance.GetUniquePowerOfTwo(reservedValues.Cast<long>(), startFromHighestExistingValue);

                        return (result.HasValue) ? result.Value : default(Optional<object>);
                    }
                case SpecialType.System_UInt64:
                    {
                        Optional<ulong> result = FlagsUtility<ulong>.Instance.GetUniquePowerOfTwo(reservedValues.Cast<ulong>(), startFromHighestExistingValue);

                        return (result.HasValue) ? result.Value : default(Optional<object>);
                    }
            }

            return default(Optional<object>);
        }

        public static Optional<ulong> GetUniquePowerOfTwo(
            IEnumerable<ulong> reservedValues,
            bool startFromHighestExistingValue = false)
        {
            Optional<ulong> result = FlagsUtility<ulong>.Instance.GetUniquePowerOfTwo(reservedValues, startFromHighestExistingValue);

            if (result.HasValue)
            {
                return result.Value;
            }
            else
            {
                return default(Optional<ulong>);
            }
        }
    }
}