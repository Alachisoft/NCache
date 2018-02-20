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
// limitations under the License.

using System;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    public class Notifications: ICloneable,ICompactSerializable
    {
        bool itemRemove, itemAdd, itemUpdate, cacheClear;

        public Notifications() { }

        [ConfigurationAttribute("item-remove")]
        public bool ItemRemove
        {
            get { return itemRemove; }
            set { itemRemove = value; }
        }

        [ConfigurationAttribute("item-add")]
        public bool ItemAdd
        {
            get { return itemAdd; }
            set { itemAdd = value; }
        }

        [ConfigurationAttribute("item-update")]
        public bool ItemUpdate
        {
            get { return itemUpdate; }
            set { itemUpdate = value; }
        }
        public bool CacheClear
        {
            get { cacheClear = true; return true; }
            set { cacheClear = value; }
        }

        #region ICloneable Members

        public object Clone()
        {
            Notifications notifications = new Notifications();
            notifications.ItemAdd = ItemAdd;
            notifications.ItemRemove = ItemRemove;
            notifications.ItemUpdate = ItemUpdate;
            notifications.CacheClear = CacheClear;
            return notifications;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            itemRemove = reader.ReadBoolean();
            itemAdd= reader.ReadBoolean();
            itemUpdate= reader.ReadBoolean();
            cacheClear = reader.ReadBoolean();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(itemRemove);
            writer.Write(itemAdd);
            writer.Write(itemUpdate);
            writer.Write(cacheClear);
        }

        #endregion
    }
}
