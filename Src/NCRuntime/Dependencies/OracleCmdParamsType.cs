// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License

namespace Alachisoft.NCache.Runtime.Dependencies
{
    /// <summary>
    /// Describes the type of the parameters passed to the Oracle command.
    /// </summary>

    public enum OracleCmdParamsType

    {
        BFile,
        Blob,
        Byte,
        Char,
        Clob,
        Date,
        Decimal,
        Double,
        Int16,
        Int32,
        Int64,
        IntervalDS,
        IntervalYM,
        Long,
        LongRaw,
        NChar,
        NClob,
        NVarchar2,
        Raw,
        RefCursor,
        Single,
        TimeStamp,
        TimeStampLTZ,
        TimeStampTZ,
        Varchar2,
        XmlType

    }
}