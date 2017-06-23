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

using System;
using System.Text;

namespace Alachisoft.NGroups
{
    public interface MessageResponder
    {
        /// <summary>
        /// Gets the HashMaps of Distribution Manager for Data Distribution and Mirror Maps for 
        /// Dynamic Mirroring within Partitioned Replica Topology. This is required at the time of joining.
        /// </summary>
        /// <param name="data">object array containg the ArrayList of Address of nodes, 
        /// true in node joining, string subGroupID, and true if node started as Mirror.</param>
        /// <returns>Returns object array containg DistributionMap and the MirrorMapping. 
        /// First object is the DistributionMap and second is the MirrorMapping table.</returns>
        object GetDistributionAndMirrorMaps(object data);
    }
}
