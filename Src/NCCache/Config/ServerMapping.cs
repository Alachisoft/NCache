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
using Alachisoft.NCache.Common.Configuration;
using System.Collections;
using Alachisoft.NCache.Runtime.Serialization;
using System.Net;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Config
{
    [Serializable]
    public class ServerMapping : ICloneable, ICompactSerializable
    {
        List<Mapping> actualMappingList = new List<Mapping>();

        public ServerMapping() { }

        public ServerMapping(Mapping[] serverMapping)
        {
            MappingServers = serverMapping;
        }

        public ServerMapping(Hashtable mappings)
        {
            IEnumerator itor = mappings.GetEnumerator();
            while (itor.MoveNext())
            {
                DictionaryEntry entry = (DictionaryEntry)itor.Current;
                Hashtable values = (Hashtable)entry.Value;

                Mapping _mapping = new Mapping();
                _mapping.PrivateIP = values["private-ip"] as string;
                _mapping.PrivatePort = Convert.ToInt32(values["private-port"]);
                _mapping.PublicIP = values["public-ip"] as string;
                _mapping.PublicPort = Convert.ToInt32(values["public-port"]);
                actualMappingList.Add(_mapping);
            }

        }

        [ConfigurationSection("end-point")]
        public Mapping[] MappingServers
        {
            get 
            {

                if (actualMappingList != null)
                    return actualMappingList.ToArray();

                return null;
            }
            set 
            {
                if (actualMappingList == null)
                    actualMappingList = new List<Mapping>();

                if (value != null)
                {
                    actualMappingList.AddRange(value);
                }                
            }
        }

      
        //public override int GetHashCode()
        //{
        //    return _serverName.ToLower().GetHashCode();
        //}

        #region ICloneable Members

        public Object Clone()
        {
            ServerMapping mapp = new ServerMapping();
            mapp.actualMappingList = actualMappingList;
            return mapp;
        }

        #endregion


        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            this.actualMappingList = reader.ReadObject() as List<Mapping>;
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(this.actualMappingList);
        }

        #endregion
    }
}
