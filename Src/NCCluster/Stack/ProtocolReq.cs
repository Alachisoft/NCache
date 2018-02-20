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
// $Id: Configurator.java,v 1.6 2004/08/12 15:43:11 belaban Exp $
namespace Alachisoft.NGroups.Stack
{
    internal class ProtocolReq
    {
        internal System.Collections.ArrayList up_reqs = null;
        internal System.Collections.ArrayList down_reqs = null;
        internal System.Collections.ArrayList up_provides = null;
        internal System.Collections.ArrayList down_provides = null;
        internal string name = null;

        internal ProtocolReq(string name)
        {
            this.name = name;
        }


        internal virtual bool providesUpService(int evt_type)
        {
            int type;

            if (up_provides != null)
            {
                for (int i = 0; i < up_provides.Count; i++)
                {
                    type = ((System.Int32)up_provides[i]);
                    if (type == evt_type)
                        return true;
                }
            }
            return false;
        }

        internal virtual bool providesDownService(int evt_type)
        {
            int type;

            if (down_provides != null)
            {
                for (int i = 0; i < down_provides.Count; i++)
                {
                    type = ((System.Int32)down_provides[i]);
                    if (type == evt_type)
                        return true;
                }
            }
            return false;
        }


        public override string ToString()
        {
            System.Text.StringBuilder ret = new System.Text.StringBuilder();
            ret.Append('\n' + name + ':');
            if (up_reqs != null)
                ret.Append("\nRequires from above: " + printUpReqs());

            if (down_reqs != null)
                ret.Append("\nRequires from below: " + printDownReqs());

            if (up_provides != null)
                ret.Append("\nProvides to above: " + printUpProvides());

            if (down_provides != null)
                ret.Append("\nProvides to below: " + printDownProvides());
            return ret.ToString();
        }


        internal virtual string printUpReqs()
        {
            System.Text.StringBuilder ret = new System.Text.StringBuilder("[");
            if (up_reqs != null)
            {
                for (int i = 0; i < up_reqs.Count; i++)
                {
                    ret.Append(Event.type2String(((System.Int32)up_reqs[i])) + ' ');
                }
            }
            return ret.ToString() + ']';
        }

        internal virtual string printDownReqs()
        {
            System.Text.StringBuilder ret = new System.Text.StringBuilder("[");
            if (down_reqs != null)
            {
                for (int i = 0; i < down_reqs.Count; i++)
                {
                    ret.Append(Event.type2String(((System.Int32)down_reqs[i])) + ' ');
                }
            }
            return ret.ToString() + ']';
        }


        internal virtual string printUpProvides()
        {
            System.Text.StringBuilder ret = new System.Text.StringBuilder("[");
            if (up_provides != null)
            {
                for (int i = 0; i < up_provides.Count; i++)
                {
                    ret.Append(Event.type2String(((System.Int32)up_provides[i])) + ' ');
                }
            }
            return ret.ToString() + ']';
        }

        internal virtual string printDownProvides()
        {
            System.Text.StringBuilder ret = new System.Text.StringBuilder("[");
            if (down_provides != null)
            {
                for (int i = 0; i < down_provides.Count; i++)
                    ret.Append(Event.type2String(((System.Int32)down_provides[i])) + ' ');
            }
            return ret.ToString() + ']';
        }
    }
}
