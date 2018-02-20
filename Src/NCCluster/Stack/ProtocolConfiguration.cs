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

    /// <summary> Parses and encapsulates the specification for 1 protocol of the protocol stack, e.g.
    /// <code>UNICAST(timeout=5000)</code>
    /// </summary>
    internal class ProtocolConfiguration
    {
        private void InitBlock(Configurator enclosingInstance)
        {
            this.enclosingInstance = enclosingInstance;
        }
        private Configurator enclosingInstance;
        virtual public string ProtocolName
        {
            get
            {
                return protocol_name;
            }

        }
        virtual public System.Collections.Hashtable Properties
        {
            get
            {
                return properties;
            }

        }

        virtual public string Contents
        {
            set
            {
                int index = value.IndexOf((System.Char)'('); // e.g. "UDP(in_port=3333)"
                int end_index = value.LastIndexOf((System.Char)')');

                if (index == -1)
                {
                    protocol_name = value;
                }
                else
                {
                    if (end_index == -1)
                    {
                        throw new System.Exception("Configurator.ProtocolConfiguration.setContents(): closing ')' " + "not found in " + value + ": properties cannot be set !");
                    }
                    else
                    {
                        properties_str = value.Substring(index + 1, (end_index) - (index + 1));
                        protocol_name = value.Substring(0, (index) - (0));
                    }
                }

                /* "in_port=5555;out_port=6666" */
                if (properties_str != null)
                {
                    System.Collections.ArrayList components = Enclosing_Instance.parseComponentStrings(properties_str, ";");
                    if (components.Count > 0)
                    {
                        for (int i = 0; i < components.Count; i++)
                        {
                            string name, valu, comp = (string)components[i];
                            index = comp.IndexOf((System.Char)'=');
                            if (index == -1)
                            {
                                throw new System.Exception("Configurator.ProtocolConfiguration.setContents(): " + "'=' not found in " + comp);
                            }
                            name = comp.Substring(0, (index) - (0));
                            valu = comp.Substring(index + 1, (comp.Length) - (index + 1));
                            properties[(string)name] = (string)valu;
                        }
                    }
                }
            }

        }
        public Configurator Enclosing_Instance
        {
            get
            {
                return enclosingInstance;
            }

        }
        public ProtocolStackType StackType
        {
            get { return stackType; }
            set { stackType = value; }
        }
        private string protocol_name = null;
        private string properties_str = null;
        private System.Collections.Hashtable properties = new System.Collections.Hashtable();
        private ProtocolStackType stackType;


        /// <summary> Creates a new ProtocolConfiguration.</summary>
        /// <param name="config_str">The configuration specification for the protocol, e.g.
        /// <pre>VERIFY_SUSPECT(timeout=1500)</pre>
        /// </param>
        public ProtocolConfiguration(Configurator enclosingInstance, string config_str)
        {
            InitBlock(enclosingInstance);
            Contents = config_str;
        }


        public virtual Protocol createLayer(ProtocolStack prot_stack)
        {
            if (protocol_name == null)
                return null;

            Protocol protocol = null;

            switch (protocol_name)
            {
                case "TCP":
                    protocol = new Alachisoft.NGroups.Protocols.TCP();
                    break;
                case "TCPPING":
                    protocol = new Alachisoft.NGroups.Protocols.TCPPING();
                    break;
                case "QUEUE":
                    protocol = new Alachisoft.NGroups.Protocols.QUEUE();
                    break;
                case "TOTAL":
                    protocol = new Alachisoft.NGroups.Protocols.TOTAL();
                    break;
                case "VIEW_ENFORCER":
                    protocol = new Alachisoft.NGroups.Protocols.VIEW_ENFORCER();
                    break;
                case "pbcast.GMS":
                    protocol = new Alachisoft.NGroups.Protocols.pbcast.GMS();
                    break;
            }

            if (protocol != null)
            {
                prot_stack.NCacheLog.Info("Configurator.createLayer()", "Created Layer " + protocol.GetType().FullName);
                protocol.Stack = prot_stack;
                if (properties != null)
                    if (!protocol.setPropertiesInternal(properties))
                        return null;
                protocol.init();
            }
            else
            {
                prot_stack.NCacheLog.Error("Configurator.createLayer()", "Couldn't create layer: " + protocol_name);
            }

            return protocol;
        }


        public override string ToString()
        {
            System.Text.StringBuilder retval = new System.Text.StringBuilder();
            retval.Append("Protocol: ");
            if (protocol_name == null)
                retval.Append("<unknown>");
            else
                retval.Append(protocol_name);
            if (properties != null)
                retval.Append("(" + Global.CollectionToString(properties) + ')');
            return retval.ToString();
        }
    }
}
