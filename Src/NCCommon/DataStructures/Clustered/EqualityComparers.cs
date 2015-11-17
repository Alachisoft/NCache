﻿// ==++==
// 
//   Copyright (c). 2015. Microsoft Corporation.
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// ==--==

using System;
using System.Collections.Generic;
using System.Security;
using System.Collections;
using System.Runtime.InteropServices;

namespace Alachisoft.NCache.Common.DataStructures.Clustered
{
    public interface IWellKnownStringEqualityComparer
    {
        IEqualityComparer GetRandomizedEqualityComparer();
        IEqualityComparer GetEqualityComparerForSerialization();
    }
    public class RandomizedObjectEqualityComparer : IEqualityComparer, IWellKnownStringEqualityComparer
    {
        private long _entropy;
        public RandomizedObjectEqualityComparer()
        {
            this._entropy = HashHelpers.GetEntropy();
        }
        public new bool Equals(object x, object y)
        {
            if (x != null)
            {
                return y != null && x.Equals(y);
            }
            return y == null;
        }

        [SecuritySafeCritical]
        public int GetHashCode(object obj)
        {
            if (obj == null)
            {
                return 0;
            }
            string text = obj as string;
            if (text != null)
            {

#if FEATURE_RANDOMIZED_STRING_HASHING
                return string.InternalMarvin32HashString(text, text.Length, this._entropy);
#endif
            }
            return obj.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            RandomizedObjectEqualityComparer randomizedObjectEqualityComparer = obj as RandomizedObjectEqualityComparer;
            return randomizedObjectEqualityComparer != null && this._entropy == randomizedObjectEqualityComparer._entropy;
        }
        public override int GetHashCode()
        {
            return base.GetType().Name.GetHashCode() ^ (int)(this._entropy & 2147483647L);
        }
        IEqualityComparer IWellKnownStringEqualityComparer.GetRandomizedEqualityComparer()
        {
            return new RandomizedObjectEqualityComparer();
        }
        IEqualityComparer IWellKnownStringEqualityComparer.GetEqualityComparerForSerialization()
        {
            return null;
        }
    }
    public class RandomizedStringEqualityComparer : IEqualityComparer<string>, IEqualityComparer, IWellKnownStringEqualityComparer
    {

        private long _entropy;
        public RandomizedStringEqualityComparer()
        {
            this._entropy = HashHelpers.GetEntropy();
        }
        public new bool Equals(object x, object y)
        {
            if (x == y)
            {
                return true;
            }
            if (x == null || y == null)
            {
                return false;
            }
            if (x is string && y is string)
            {
                return this.Equals((string)x, (string)y);
            }
            throw new ArgumentException(ResourceHelper.GetResourceString(GetResourceName(ExceptionResource.Argument_InvalidArgumentForComparison)));
            return false;
        }

        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport("QCall", CharSet = CharSet.Unicode)]
        public static extern bool InternalUseRandomizedHashing();

        internal static string GetResourceName(ExceptionResource resource)
        {
            string result;
            switch (resource)
            {
                case ExceptionResource.Argument_ImplementIComparable:
                    result = "Argument_ImplementIComparable";
                    break;
                case ExceptionResource.Argument_InvalidType:
                    result = "Argument_InvalidType";
                    break;
                case ExceptionResource.Argument_InvalidArgumentForComparison:
                    result = "Argument_InvalidArgumentForComparison";
                    break;
                case ExceptionResource.Argument_InvalidRegistryKeyPermissionCheck:
                    result = "Argument_InvalidRegistryKeyPermissionCheck";
                    break;
                case ExceptionResource.ArgumentOutOfRange_NeedNonNegNum:
                    result = "ArgumentOutOfRange_NeedNonNegNum";
                    break;
                case ExceptionResource.Arg_ArrayPlusOffTooSmall:
                    result = "Arg_ArrayPlusOffTooSmall";
                    break;
                case ExceptionResource.Arg_NonZeroLowerBound:
                    result = "Arg_NonZeroLowerBound";
                    break;
                case ExceptionResource.Arg_RankMultiDimNotSupported:
                    result = "Arg_RankMultiDimNotSupported";
                    break;
                case ExceptionResource.Arg_RegKeyDelHive:
                    result = "Arg_RegKeyDelHive";
                    break;
                case ExceptionResource.Arg_RegKeyStrLenBug:
                    result = "Arg_RegKeyStrLenBug";
                    break;
                case ExceptionResource.Arg_RegSetStrArrNull:
                    result = "Arg_RegSetStrArrNull";
                    break;
                case ExceptionResource.Arg_RegSetMismatchedKind:
                    result = "Arg_RegSetMismatchedKind";
                    break;
                case ExceptionResource.Arg_RegSubKeyAbsent:
                    result = "Arg_RegSubKeyAbsent";
                    break;
                case ExceptionResource.Arg_RegSubKeyValueAbsent:
                    result = "Arg_RegSubKeyValueAbsent";
                    break;
                case ExceptionResource.Argument_AddingDuplicate:
                    result = "Argument_AddingDuplicate";
                    break;
                case ExceptionResource.Serialization_InvalidOnDeser:
                    result = "Serialization_InvalidOnDeser";
                    break;
                case ExceptionResource.Serialization_MissingKeys:
                    result = "Serialization_MissingKeys";
                    break;
                case ExceptionResource.Serialization_NullKey:
                    result = "Serialization_NullKey";
                    break;
                case ExceptionResource.Argument_InvalidArrayType:
                    result = "Argument_InvalidArrayType";
                    break;
                case ExceptionResource.NotSupported_KeyCollectionSet:
                    result = "NotSupported_KeyCollectionSet";
                    break;
                case ExceptionResource.NotSupported_ValueCollectionSet:
                    result = "NotSupported_ValueCollectionSet";
                    break;
                case ExceptionResource.ArgumentOutOfRange_SmallCapacity:
                    result = "ArgumentOutOfRange_SmallCapacity";
                    break;
                case ExceptionResource.ArgumentOutOfRange_Index:
                    result = "ArgumentOutOfRange_Index";
                    break;
                case ExceptionResource.Argument_InvalidOffLen:
                    result = "Argument_InvalidOffLen";
                    break;
                case ExceptionResource.Argument_ItemNotExist:
                    result = "Argument_ItemNotExist";
                    break;
                case ExceptionResource.ArgumentOutOfRange_Count:
                    result = "ArgumentOutOfRange_Count";
                    break;
                case ExceptionResource.ArgumentOutOfRange_InvalidThreshold:
                    result = "ArgumentOutOfRange_InvalidThreshold";
                    break;
                case ExceptionResource.ArgumentOutOfRange_ListInsert:
                    result = "ArgumentOutOfRange_ListInsert";
                    break;
                case ExceptionResource.NotSupported_ReadOnlyCollection:
                    result = "NotSupported_ReadOnlyCollection";
                    break;
                case ExceptionResource.InvalidOperation_CannotRemoveFromStackOrQueue:
                    result = "InvalidOperation_CannotRemoveFromStackOrQueue";
                    break;
                case ExceptionResource.InvalidOperation_EmptyQueue:
                    result = "InvalidOperation_EmptyQueue";
                    break;
                case ExceptionResource.InvalidOperation_EnumOpCantHappen:
                    result = "InvalidOperation_EnumOpCantHappen";
                    break;
                case ExceptionResource.InvalidOperation_EnumFailedVersion:
                    result = "InvalidOperation_EnumFailedVersion";
                    break;
                case ExceptionResource.InvalidOperation_EmptyStack:
                    result = "InvalidOperation_EmptyStack";
                    break;
                case ExceptionResource.ArgumentOutOfRange_BiggerThanCollection:
                    result = "ArgumentOutOfRange_BiggerThanCollection";
                    break;
                case ExceptionResource.InvalidOperation_EnumNotStarted:
                    result = "InvalidOperation_EnumNotStarted";
                    break;
                case ExceptionResource.InvalidOperation_EnumEnded:
                    result = "InvalidOperation_EnumEnded";
                    break;
                case ExceptionResource.NotSupported_SortedListNestedWrite:
                    result = "NotSupported_SortedListNestedWrite";
                    break;
                case ExceptionResource.InvalidOperation_NoValue:
                    result = "InvalidOperation_NoValue";
                    break;
                case ExceptionResource.InvalidOperation_RegRemoveSubKey:
                    result = "InvalidOperation_RegRemoveSubKey";
                    break;
                case ExceptionResource.Security_RegistryPermission:
                    result = "Security_RegistryPermission";
                    break;
                case ExceptionResource.UnauthorizedAccess_RegistryNoWrite:
                    result = "UnauthorizedAccess_RegistryNoWrite";
                    break;
                case ExceptionResource.ObjectDisposed_RegKeyClosed:
                    result = "ObjectDisposed_RegKeyClosed";
                    break;
                case ExceptionResource.NotSupported_InComparableType:
                    result = "NotSupported_InComparableType";
                    break;
                case ExceptionResource.Argument_InvalidRegistryOptionsCheck:
                    result = "Argument_InvalidRegistryOptionsCheck";
                    break;
                case ExceptionResource.Argument_InvalidRegistryViewCheck:
                    result = "Argument_InvalidRegistryViewCheck";
                    break;
                default:
                    return string.Empty;
            }
            return result;
        }

        public bool Equals(string x, string y)
        {
            if (x != null)
            {
                return y != null && x.Equals(y);
            }
            return y == null;
        }
        [SecuritySafeCritical]
        public int GetHashCode(string obj)
        {
            if (obj == null)
            {
                return 0;
            }
            return InternalMarvin32HashString(obj, obj.Length, this._entropy);
        }
        [DllImport("QCall", CharSet = CharSet.Unicode)]
        internal static extern int InternalMarvin32HashString(string s, int sLen, long additionalEntropy);

        [SecuritySafeCritical]
        public int GetHashCode(object obj)
        {
            if (obj == null)
            {
                return 0;
            }
            string text = obj as string;
            if (text != null)
            {
                return InternalMarvin32HashString(text, text.Length, this._entropy);
            }
            return obj.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            RandomizedStringEqualityComparer randomizedStringEqualityComparer = obj as RandomizedStringEqualityComparer;
            return randomizedStringEqualityComparer != null && this._entropy == randomizedStringEqualityComparer._entropy;
        }
        public override int GetHashCode()
        {
            return base.GetType().Name.GetHashCode() ^ (int)(this._entropy & 2147483647L);
        }
        IEqualityComparer IWellKnownStringEqualityComparer.GetRandomizedEqualityComparer()
        {
            return new RandomizedStringEqualityComparer();
        }
        IEqualityComparer IWellKnownStringEqualityComparer.GetEqualityComparerForSerialization()
        {
            return EqualityComparer<string>.Default;
        }
    }
}
