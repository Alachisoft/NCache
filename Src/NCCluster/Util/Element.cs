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
// $Id: List.java,v 1.6 2004/07/05 14:17:35 belaban Exp $
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NGroups.Util
{
    [Serializable]
    internal class Element
    {
        private void InitBlock(List enclosingInstance)
        {
            this.enclosingInstance = enclosingInstance;
        }
        private List enclosingInstance;
        public List Enclosing_Instance
        {
            get
            {
                return enclosingInstance;
            }

        }
        internal object obj = null;
        internal Element next = null;
        internal Element prev = null;

        internal Element(List enclosingInstance, object o)
        {
            InitBlock(enclosingInstance);
            obj = o;
        }
    }

}
