//  Copyright (c) 2021 Alachisoft
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
using System;
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Common.DataStructures
{
    /// <summary>
    /// To be implemented by binary data structures.
    /// </summary>
    public interface IStreamItem
    {
        /// <summary>
        /// Copies the data from stream item into the Virtual buffer.
        /// </summary>
        /// <param name="offset">offset in the stream item.</param>
        /// <param name="length">length of the data to be read.</param>
        /// <returns></returns>
        VirtualArray Read(int offset, int length);

        /// <summary>
        /// Copies data from the virutal buffer into the stream item.
        /// </summary>
        /// <param name="vBuffer">Data to be written to the stream item.</param>
        /// <param name="srcOffset">Offset in the source buffer.</param>
        /// <param name="dstOffset">Offset in the stream item.</param>
        /// <param name="length">Length of data to be copied.</param>
        void Write(VirtualArray vBuffer, int srcOffset,int dstOffset, int length);

        /// <summary>
        /// Gets/Sets the length of stram item.
        /// </summary>
        int Length { get; set;}
    }
}
