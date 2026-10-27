//  Copyright (c) 2018 Alachisoft
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

namespace Alachisoft.NCache.SocketServer.Resp.Compatibility
{
    /// <summary>
    /// The geospatial commands - 10 of them, on 8 members: all 8 declared abstract; see RespCompatibilityBase for which this build serves.
    /// </summary>
    internal interface IRespGeoCommands
    {
        /// <summary>GEOADD.</summary>
        [RespCommand("GEOADD")]
        long GeoAdd(string key, RespArgs args);

        /// <summary>GEOPOS.</summary>
        [RespCommand("GEOPOS")]
        RespReply GeoPosition(string key, IList<byte[]> members);

        /// <summary>GEODIST.</summary>
        [RespCommand("GEODIST")]
        double? GeoDistance(string key, byte[] first, byte[] second, string unit);

        /// <summary>GEOHASH.</summary>
        [RespCommand("GEOHASH")]
        IList<byte[]> GeoHash(string key, IList<byte[]> members);

        /// <summary>GEOSEARCH.</summary>
        [RespCommand("GEOSEARCH")]
        RespReply GeoSearch(string key, RespArgs args);

        /// <summary>GEOSEARCHSTORE.</summary>
        [RespCommand("GEOSEARCHSTORE")]
        long GeoSearchAndStore(string destination, string key, RespArgs args);

        /// <summary>GEORADIUS and GEORADIUS_RO.</summary>
        [RespCommand("GEORADIUS", "GEORADIUS_RO")]
        RespReply GeoRadius(string key, RespArgs args, bool readOnly);

        /// <summary>GEORADIUSBYMEMBER and GEORADIUSBYMEMBER_RO.</summary>
        [RespCommand("GEORADIUSBYMEMBER", "GEORADIUSBYMEMBER_RO")]
        RespReply GeoRadiusByMember(string key, RespArgs args, bool readOnly);
    }
}
