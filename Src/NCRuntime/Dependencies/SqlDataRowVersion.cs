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
    public enum SqlDataRowVersion

    {
        // Summary:
        //     The row contains its original values.
        Original = 256,
        //
        // Summary:
        //     The row contains current values.
        Current = 512,
        //
        // Summary:
        //     The row contains a proposed value.
        Proposed = 1024,
        //
        // Summary:
        //     The default version of System.Data.DataRowState. For a DataRowState value
        //     of Added, Modified or Deleted, the default version is Current. For a System.Data.DataRowState
        //     value of Detached, the version is Proposed.
        Default = 1536,
    }
}