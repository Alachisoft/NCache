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
    /// Describes the type of the command passed to the SqlDependency.
    /// </summary>
    /// <remark>
    /// This Feature is Not Available in Express
    /// </remark>

    public enum SqlCommandType

    {
        // Summary:
        //     An SQL text command. (Default.)
        Text = 1,
        //
        // Summary:
        //     The name of a stored procedure.
        StoredProcedure = 4,
        //
        // Summary:
        //     The name of a table.
        TableDirect = 512,
    }
}