// ==++==
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
using System.Runtime.Serialization;

namespace Alachisoft.NCache.Common.DataStructures.Clustered
{
    public static class ThrowHelper
    {
        internal static void ThrowArgumentOutOfRangeException()
        {
            throw new ArgumentOutOfRangeException(ThrowHelper.GetArgumentName(ExceptionArgument.index), ResourceHelper.GetResourceString(ThrowHelper.GetResourceName(ExceptionResource.ArgumentOutOfRange_Index)));
        }
        internal static void ThrowWrongKeyTypeArgumentException(object key, Type targetType)
        {
            throw new ArgumentException(ResourceHelper.GetResourceString("Arg_WrongType"));
        }
        internal static void ThrowWrongValueTypeArgumentException(object value, Type targetType)
        {
            throw new ArgumentException(ResourceHelper.GetResourceString("Arg_WrongType"));
        }
        internal static void ThrowKeyNotFoundException()
        {
            throw new KeyNotFoundException();
        }
        internal static void ThrowArgumentException(ExceptionResource resource)
        {
            throw new ArgumentException(ResourceHelper.GetResourceString(ThrowHelper.GetResourceName(resource)));
        }
        internal static void ThrowArgumentException(ExceptionResource resource, ExceptionArgument argument)
        {
            throw new ArgumentException(ResourceHelper.GetResourceString(ThrowHelper.GetResourceName(resource)), ThrowHelper.GetArgumentName(argument));
        }
        internal static void ThrowArgumentNullException(ExceptionArgument argument)
        {
            throw new ArgumentNullException(ThrowHelper.GetArgumentName(argument));
        }
        internal static void ThrowArgumentOutOfRangeException(ExceptionArgument argument)
        {
            throw new ArgumentOutOfRangeException(ThrowHelper.GetArgumentName(argument));
        }
        internal static void ThrowArgumentOutOfRangeException(ExceptionArgument argument, ExceptionResource resource)
        {
            throw new ArgumentOutOfRangeException(ThrowHelper.GetArgumentName(argument), ResourceHelper.GetResourceString(ThrowHelper.GetResourceName(resource)));
        }
        internal static void ThrowInvalidOperationException(ExceptionResource resource)
        {
            throw new InvalidOperationException(ResourceHelper.GetResourceString(ThrowHelper.GetResourceName(resource)));
        }
        internal static void ThrowSerializationException(ExceptionResource resource)
        {
            throw new SerializationException(ResourceHelper.GetResourceString(ThrowHelper.GetResourceName(resource)));
        }
        internal static void ThrowSecurityException(ExceptionResource resource)
        {
            throw new SecurityException(ResourceHelper.GetResourceString(ThrowHelper.GetResourceName(resource)));
        }
        internal static void ThrowNotSupportedException(ExceptionResource resource)
        {
            throw new NotSupportedException(ResourceHelper.GetResourceString(ThrowHelper.GetResourceName(resource)));
        }
        internal static void ThrowUnauthorizedAccessException(ExceptionResource resource)
        {
            throw new UnauthorizedAccessException(ResourceHelper.GetResourceString(ThrowHelper.GetResourceName(resource)));
        }
        internal static void ThrowObjectDisposedException(string objectName, ExceptionResource resource)
        {
            throw new ObjectDisposedException(objectName, ResourceHelper.GetResourceString(ThrowHelper.GetResourceName(resource)));
        }
        internal static void IfNullAndNullsAreIllegalThenThrow<T>(object value, ExceptionArgument argName)
        {
            if (value == null && default(T) != null)
            {
                ThrowHelper.ThrowArgumentNullException(argName);
            }
        }
        internal static string GetArgumentName(ExceptionArgument argument)
        {
            string result;
            switch (argument)
            {
                case ExceptionArgument.obj:
                    result = "obj";
                    break;
                case ExceptionArgument.dictionary:
                    result = "dictionary";
                    break;
                case ExceptionArgument.dictionaryCreationThreshold:
                    result = "dictionaryCreationThreshold";
                    break;
                case ExceptionArgument.array:
                    result = "array";
                    break;
                case ExceptionArgument.info:
                    result = "info";
                    break;
                case ExceptionArgument.key:
                    result = "key";
                    break;
                case ExceptionArgument.collection:
                    result = "collection";
                    break;
                case ExceptionArgument.list:
                    result = "list";
                    break;
                case ExceptionArgument.match:
                    result = "match";
                    break;
                case ExceptionArgument.converter:
                    result = "converter";
                    break;
                case ExceptionArgument.queue:
                    result = "queue";
                    break;
                case ExceptionArgument.stack:
                    result = "stack";
                    break;
                case ExceptionArgument.capacity:
                    result = "capacity";
                    break;
                case ExceptionArgument.index:
                    result = "index";
                    break;
                case ExceptionArgument.startIndex:
                    result = "startIndex";
                    break;
                case ExceptionArgument.value:
                    result = "value";
                    break;
                case ExceptionArgument.count:
                    result = "count";
                    break;
                case ExceptionArgument.arrayIndex:
                    result = "arrayIndex";
                    break;
                case ExceptionArgument.name:
                    result = "name";
                    break;
                case ExceptionArgument.mode:
                    result = "mode";
                    break;
                case ExceptionArgument.item:
                    result = "item";
                    break;
                case ExceptionArgument.options:
                    result = "options";
                    break;
                case ExceptionArgument.view:
                    result = "view";
                    break;
                default:
                    return string.Empty;
            }
            return result;
        }
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
    }

    public enum ExceptionArgument
    {
        obj,
        dictionary,
        dictionaryCreationThreshold,
        array,
        info,
        key,
        collection,
        list,
        match,
        converter,
        queue,
        stack,
        capacity,
        index,
        startIndex,
        value,
        count,
        arrayIndex,
        name,
        mode,
        item,
        options,
        view
    }

    public enum ExceptionResource
    {
        Argument_ImplementIComparable,
        Argument_InvalidType,
        Argument_InvalidArgumentForComparison,
        Argument_InvalidRegistryKeyPermissionCheck,
        ArgumentOutOfRange_NeedNonNegNum,
        Arg_ArrayPlusOffTooSmall,
        Arg_NonZeroLowerBound,
        Arg_RankMultiDimNotSupported,
        Arg_RegKeyDelHive,
        Arg_RegKeyStrLenBug,
        Arg_RegSetStrArrNull,
        Arg_RegSetMismatchedKind,
        Arg_RegSubKeyAbsent,
        Arg_RegSubKeyValueAbsent,
        Argument_AddingDuplicate,
        Serialization_InvalidOnDeser,
        Serialization_MissingKeys,
        Serialization_NullKey,
        Argument_InvalidArrayType,
        NotSupported_KeyCollectionSet,
        NotSupported_ValueCollectionSet,
        ArgumentOutOfRange_SmallCapacity,
        ArgumentOutOfRange_Index,
        Argument_InvalidOffLen,
        Argument_ItemNotExist,
        ArgumentOutOfRange_Count,
        ArgumentOutOfRange_InvalidThreshold,
        ArgumentOutOfRange_ListInsert,
        NotSupported_ReadOnlyCollection,
        InvalidOperation_CannotRemoveFromStackOrQueue,
        InvalidOperation_EmptyQueue,
        InvalidOperation_EnumOpCantHappen,
        InvalidOperation_EnumFailedVersion,
        InvalidOperation_EmptyStack,
        ArgumentOutOfRange_BiggerThanCollection,
        InvalidOperation_EnumNotStarted,
        InvalidOperation_EnumEnded,
        NotSupported_SortedListNestedWrite,
        InvalidOperation_NoValue,
        InvalidOperation_RegRemoveSubKey,
        Security_RegistryPermission,
        UnauthorizedAccess_RegistryNoWrite,
        ObjectDisposed_RegKeyClosed,
        NotSupported_InComparableType,
        Argument_InvalidRegistryOptionsCheck,
        Argument_InvalidRegistryViewCheck
    }

    public static class ResId
    {
        internal const string Arg_ArrayLengthsDiffer = "Arg_ArrayLengthsDiffer";
        internal const string Arg_ArrayPlusOffTooSmall = "Arg_ArrayPlusOffTooSmall";
        internal const string Arg_HSCapacityOverflow = "Arg_HSCapacityOverflow";
        internal const string Argument_InvalidNumberOfMembers = "Argument_InvalidNumberOfMembers";
        internal const string Argument_UnequalMembers = "Argument_UnequalMembers";
        internal const string Argument_SpecifyValueSize = "Argument_SpecifyValueSize";
        internal const string Argument_UnmatchingSymScope = "Argument_UnmatchingSymScope";
        internal const string Argument_NotInExceptionBlock = "Argument_NotInExceptionBlock";
        internal const string Argument_NotExceptionType = "Argument_NotExceptionType";
        internal const string Argument_InvalidLabel = "Argument_InvalidLabel";
        internal const string Argument_UnclosedExceptionBlock = "Argument_UnclosedExceptionBlock";
        internal const string Argument_MissingDefaultConstructor = "Argument_MissingDefaultConstructor";
        internal const string Argument_TooManyFinallyClause = "Argument_TooManyFinallyClause";
        internal const string Argument_NotInTheSameModuleBuilder = "Argument_NotInTheSameModuleBuilder";
        internal const string Argument_BadCurrentLocalVariable = "Argument_BadCurrentLocalVariable";
        internal const string Argument_DuplicateModuleName = "Argument_DuplicateModuleName";
        internal const string Argument_BadPersistableModuleInTransientAssembly = "Argument_BadPersistableModuleInTransientAssembly";
        internal const string Argument_HasToBeArrayClass = "Argument_HasToBeArrayClass";
        internal const string Argument_InvalidDirectory = "Argument_InvalidDirectory";
        internal const string ArgumentOutOfRange_NeedNonNegNum = "ArgumentOutOfRange_NeedNonNegNum";
        internal const string MissingType = "MissingType";
        internal const string MissingModule = "MissingModule";
        internal const string ArgumentOutOfRange_Index = "ArgumentOutOfRange_Index";
        internal const string ArgumentOutOfRange_Range = "ArgumentOutOfRange_Range";
        internal const string ExecutionEngine_YoureHosed = "ExecutionEngine_YoureHosed";
        internal const string Format_NeedSingleChar = "Format_NeedSingleChar";
        internal const string Format_StringZeroLength = "Format_StringZeroLength";
        internal const string InvalidOperation_EnumEnded = "InvalidOperation_EnumEnded";
        internal const string InvalidOperation_EnumFailedVersion = "InvalidOperation_EnumFailedVersion";
        internal const string InvalidOperation_EnumNotStarted = "InvalidOperation_EnumNotStarted";
        internal const string InvalidOperation_EnumOpCantHappen = "InvalidOperation_EnumOpCantHappen";
        internal const string InvalidOperation_InternalState = "InvalidOperation_InternalState";
        internal const string InvalidOperation_ModifyRONumFmtInfo = "InvalidOperation_ModifyRONumFmtInfo";
        internal const string InvalidOperation_MethodBaked = "InvalidOperation_MethodBaked";
        internal const string InvalidOperation_NotADebugModule = "InvalidOperation_NotADebugModule";
        internal const string InvalidOperation_MethodHasBody = "InvalidOperation_MethodHasBody";
        internal const string InvalidOperation_OpenLocalVariableScope = "InvalidOperation_OpenLocalVariableScope";
        internal const string InvalidOperation_TypeHasBeenCreated = "InvalidOperation_TypeHasBeenCreated";
        internal const string InvalidOperation_RefedAssemblyNotSaved = "InvalidOperation_RefedAssemblyNotSaved";
        internal const string InvalidOperation_AssemblyHasBeenSaved = "InvalidOperation_AssemblyHasBeenSaved";
        internal const string InvalidOperation_ModuleHasBeenSaved = "InvalidOperation_ModuleHasBeenSaved";
        internal const string InvalidOperation_CannotAlterAssembly = "InvalidOperation_CannotAlterAssembly";
        internal const string NotSupported_CannotSaveModuleIndividually = "NotSupported_CannotSaveModuleIndividually";
        internal const string NotSupported_Constructor = "NotSupported_Constructor";
        internal const string NotSupported_Method = "NotSupported_Method";
        internal const string NotSupported_NYI = "NotSupported_NYI";
        internal const string NotSupported_DynamicModule = "NotSupported_DynamicModule";
        internal const string NotSupported_NotDynamicModule = "NotSupported_NotDynamicModule";
        internal const string NotSupported_NotAllTypesAreBaked = "NotSupported_NotAllTypesAreBaked";
        internal const string NotSupported_SortedListNestedWrite = "NotSupported_SortedListNestedWrite";
        internal const string Serialization_ArrayInvalidLength = "Serialization_ArrayInvalidLength";
        internal const string Serialization_ArrayNoLength = "Serialization_ArrayNoLength";
        internal const string Serialization_CannotGetType = "Serialization_CannotGetType";
        internal const string Serialization_InsufficientState = "Serialization_InsufficientState";
        internal const string Serialization_InvalidID = "Serialization_InvalidID";
        internal const string Serialization_MalformedArray = "Serialization_MalformedArray";
        internal const string Serialization_MultipleMembers = "Serialization_MultipleMembers";
        internal const string Serialization_NoID = "Serialization_NoID";
        internal const string Serialization_NoType = "Serialization_NoType";
        internal const string Serialization_MissingKeys = "Serialization_MissingKeys";
        internal const string Serialization_NoBaseType = "Serialization_NoBaseType";
        internal const string Serialization_NullSignature = "Serialization_NullSignature";
        internal const string Serialization_UnknownMember = "Serialization_UnknownMember";
        internal const string Serialization_BadParameterInfo = "Serialization_BadParameterInfo";
        internal const string Serialization_NoParameterInfo = "Serialization_NoParameterInfo";
        internal const string WeakReference_NoLongerValid = "WeakReference_NoLongerValid";
        internal const string Loader_InvalidPath = "Loader_InvalidPath";
    }

}
