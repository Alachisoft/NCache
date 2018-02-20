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
    internal class ListEnumerator : System.Collections.IEnumerator
    {
        private void InitBlock(List enclosingInstance)
        {
            this.enclosingInstance = enclosingInstance;
        }
        private object tempAuxObj;
        public bool MoveNext()
        {
            bool result = hasMoreElements();
            if (result)
            {
                tempAuxObj = nextElement();
            }
            return result;
        }
        public void Reset()
        {
            tempAuxObj = null;
        }
        public object Current
        {
            get
            {
                return tempAuxObj;
            }

        }
        private List enclosingInstance;
        public List Enclosing_Instance
        {
            get
            {
                return enclosingInstance;
            }

        }
        internal Element curr = null;

        internal ListEnumerator(List enclosingInstance, Element start)
        {
            InitBlock(enclosingInstance);
            curr = start;
        }

        public bool hasMoreElements()
        {
            return curr != null;
        }

        public object nextElement()
        {
            object retval;

            if (curr == null)
                throw new System.ArgumentOutOfRangeException();
            retval = curr.obj;
            curr = curr.next;
            return retval;
        }
    }
}
