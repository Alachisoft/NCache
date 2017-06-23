// Copyright (c) 2017 Alachisoft
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
// limitations under the License.


namespace Alachisoft.NCache.Common.DataReader
{
    public interface IRecordSet
    {
        /// <summary>
        /// Adds <see cref="Alachisoft.NCache.Common.DataStructures.RecordColumn"/> in current <see cref="Alachisoft.NCache.Common.DataStructures.RecorSet"/>
        /// </summary>
        /// <param name="column"><see cref="Alachisoft.NCache.Common.DataStructures.RecordColumn"/> to be added</param>
        void AddColumn(RecordColumn column);

        /// <summary>
        /// Returns new <see cref="Alachisoft.NCache.Common.DataStructures.RecordRow"/> with column matadata of current <see cref="Alachisoft.NCache.Common.DataStructures.RecordSet"/>
        /// </summary>
        /// <returns>Newly created <see cref="Alachisoft.NCache.Common.DataStructures.RecordRow"/></returns>
        RecordRow CreateRow();

        /// <summary>
        /// Adds row to current <see cref="Alachisoft.NCache.Common.DataStructures.RecorSet"/>
        /// </summary>
        /// <param name="row"><see cref="Alachisoft.NCache.Common.DataStructures.RecordRow"/> to be added in current <see cref="Alachisoft.NCache.Common.DataStructures.RecorSet"/></param>
        void AddRow(RecordRow row);

        /// <summary>
        /// Gets <see cref="Alachisoft.NCache.Common.DataStructures.RecordRow"/> associated with rowID
        /// </summary>
        /// <param name="rowID">Index of <see cref="Alachisoft.NCache.Common.DataStructures.RecordRow"/> required</param>
        /// <returns>Required <see cref="Alachisoft.NCache.Common.DataStructures.RecordRow"/> accoring to rowID</returns>
        RecordRow GetRow(int rowID);

        bool ContainsRow(int rowID);

        /// <summary>
        /// Removes <see cref="Alachisoft.NCache.Common.DataStructures.RecordRow"/> associated with rowID
        /// </summary>
        /// <param name="rowID">Index of <see cref="Alachisoft.NCache.Common.DataStructures.RecordRow"/> to be removed</param>
        void RemoveRow(int rowID);

        /// <summary>
        /// Removes specified rows range from current <see cref="Alachisoft.NCache.Common.DataStructures.RecordSet"/>
        /// </summary>
        /// <param name="startingIndex">Starting index of row's range to be removed.</param>
        /// <param name="count">Total number of rows to be removed.</param>
        /// <returns>Number of rows removed</returns>
        int RemoveRows(int startingRowID, int count);

        
        ColumnCollection GetColumnMetaData();


        /// <summary>
        /// Gets number of rows in current <see cref="Alachisoft.NCache.Common.DataStructures.RecordSet"/>
        /// </summary>
        int RowCount { get; }

        /// <summary>
        /// Gets <see cref="Alachisoft.NCache.Common.DataStructures.IRecordSetEnumerator"/> for current <see cref="Alachisoft.NCache.Common.DataStructures.RecordSet"/>
        /// </summary>
        /// <returns></returns>
        IRecordSetEnumerator GetEnumerator();

        SubsetInfo SubsetInfo
        {
            get;
            set;
        }
    }
}
