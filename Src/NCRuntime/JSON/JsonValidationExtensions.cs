//  Copyright (c) 2026 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License

using Alachisoft.NCache.Runtime.ErrorHandling;
using Alachisoft.NCache.Runtime.Exceptions;
using System;
using System.Collections.Generic;
namespace Alachisoft.NCache.Runtime.JSON
{
    internal static class JsonValidationExtensions
    {
        internal static void InvalidateParentReference(this JsonValueBase jsonValue, IDictionary<JsonValueBase, byte> backRefs = default(IDictionary<JsonValueBase, byte>))
        {
            if (backRefs == default(IDictionary<JsonValueBase, byte>))
                backRefs = new Dictionary<JsonValueBase, byte>();

            if (backRefs.ContainsKey(jsonValue))
                throw new OperationFailedException(ErrorCodes.Json.REFERENCE_TO_PARENT, ErrorMessages.GetErrorMessage(ErrorCodes.Json.REFERENCE_TO_PARENT));

            backRefs.Add(jsonValue, 0);

            switch (jsonValue.DataType)
            {
                case Enum.JsonDataType.Object:
                    var jsonObject = jsonValue as JsonObject;

                    if (jsonObject == null)
                        return;

                    foreach (var attribute in jsonObject)
                        InvalidateParentReference(attribute.Value, backRefs);

                    break;

                case Enum.JsonDataType.Array:
                    var jsonArray = jsonValue as JsonArray;

                    if (jsonArray == null)
                        return;

                    foreach (var item in jsonArray)
                        InvalidateParentReference(item, backRefs);

                    break;

                case Enum.JsonDataType.Null:
                default:
                    backRefs.Remove(jsonValue);
                    break;
            }
        }
    }
}
