// $Id: Configurator.java,v 1.6 2004/08/12 15:43:11 belaban Exp $
using System;
//using System.Runtime.Remoting;

using Alachisoft.NGroups;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NGroups.Protocols;
using Alachisoft.NGroups.Protocols.pbcast;

namespace Alachisoft.NGroups.Stack
{
	/// <summary> The task if this class is to setup and configure the protocol stack. A string describing
	/// the desired setup, which is both the layering and the configuration of each layer, is
	/// given to the configurator which creates and configures the protocol stack and returns
	/// a reference to the top layer (Protocol).<p>
	/// Future functionality will include the capability to dynamically modify the layering
	/// of the protocol stack and the properties of each layer.
	/// </summary>
	/// <author>  Bela Ban
	/// </author>
	internal class Configurator
	{
		
		/// <summary> The configuration string has a number of entries, separated by a ':' (colon).
		/// Each entry consists of the name of the protocol, followed by an optional configuration
		/// of that protocol. The configuration is enclosed in parentheses, and contains entries
		/// which are name/value pairs connected with an assignment sign (=) and separated by
		/// a semicolon.
		/// <pre>UDP(in_port=5555;out_port=4445):FRAG(frag_size=1024)</pre><p>
		/// The <em>first</em> entry defines the <em>bottommost</em> layer, the string is parsed
		/// left to right and the protocol stack constructed bottom up. Example: the string
		/// "UDP(in_port=5555):FRAG(frag_size=32000):DEBUG" results is the following stack:<pre>
		/// 
		/// -----------------------
		/// | DEBUG                 |
		/// |-----------------------|
		/// | FRAG frag_size=32000  |
		/// |-----------------------|
		/// | UDP in_port=32000     |
		/// -----------------------
		/// </pre>
		/// </summary>
		public virtual Protocol setupProtocolStack(string configuration, ProtocolStack st)
		{
			Protocol protocol_stack = null;
			System.Collections.ArrayList protocol_configs;
			System.Collections.ArrayList protocols;
			
			protocol_configs = parseConfigurations(configuration);
			protocols = createProtocols(protocol_configs, st);
			if (protocols == null)
				return null;
			protocol_stack = connectProtocols(protocols);
			return protocol_stack;
		}
		
		
		public virtual void  startProtocolStack(Protocol bottom_prot)
		{
			while (bottom_prot != null)
			{
				bottom_prot.startDownHandler();
				bottom_prot.startUpHandler();
				bottom_prot = bottom_prot.UpProtocol;
			}
		}
		
		
		public virtual void  stopProtocolStack(Protocol start_prot)
		{
			while (start_prot != null)
			{
				start_prot.stopInternal();
				start_prot.destroy();
				start_prot = start_prot.DownProtocol;
			}
		}
		
		
		public virtual Protocol findProtocol(Protocol prot_stack, string name)
		{
			string s;
			Protocol curr_prot = prot_stack;
			
			while (true)
			{
				s = curr_prot.Name;
				if (s == null)
					continue;
				if (s.Equals(name))
					return curr_prot;
				curr_prot = curr_prot.DownProtocol;
				if (curr_prot == null)
					break;
			}
			return null;
		}
		
		
		public virtual Protocol getBottommostProtocol(Protocol prot_stack)
		{
			Protocol tmp = null, curr_prot = prot_stack;
			
			while (true)
			{
				if ((tmp = curr_prot.DownProtocol) == null)
					break;
				curr_prot = tmp;
			}
			return curr_prot;
		}
		
		
		/// <summary> Creates a new protocol given the protocol specification. Initializes the properties and starts the
		/// up and down handler threads.
		/// </summary>
		/// <param name="prot_spec">The specification of the protocol. Same convention as for specifying a protocol stack.
		/// An exception will be thrown if the class cannot be created. Example:
		/// <pre>"VERIFY_SUSPECT(timeout=1500)"</pre> Note that no colons (:) have to be
		/// specified
		/// </param>
		/// <param name="stack">The protocol stack
		/// </param>
		/// <returns> Protocol The newly created protocol
		/// </returns>
		/// <exception cref=""> Exception Will be thrown when the new protocol cannot be created
		/// </exception>
		public virtual Protocol createProtocol(string prot_spec, ProtocolStack stack)
		{
			ProtocolConfiguration config;
			Protocol prot;
			
			if (prot_spec == null)
				throw new System.Exception("Configurator.createProtocol(): prot_spec is null");
			
			// parse the configuration for this protocol
			config = new ProtocolConfiguration(this, prot_spec);
			
			// create an instance of the protocol class and configure it
			prot = config.createLayer(stack);
			
			// start the handler threads (unless down_thread or up_thread are set to false)
			prot.startDownHandler();
			prot.startUpHandler();
			
			return prot;
		}
		
		
		/// <summary> Inserts an already created (and initialized) protocol into the protocol list. Sets the links
		/// to the protocols above and below correctly and adjusts the linked list of protocols accordingly.
		/// </summary>
		/// <param name="prot"> The protocol to be inserted. Before insertion, a sanity check will ensure that none
		/// of the existing protocols have the same name as the new protocol.
		/// </param>
		/// <param name="position">Where to place the protocol with respect to the neighbor_prot (ABOVE, BELOW)
		/// </param>
		/// <param name="neighbor_prot">The name of the neighbor protocol. An exception will be thrown if this name
		/// is not found
		/// </param>
		/// <param name="stack">The protocol stack
		/// </param>
		/// <exception cref=""> Exception Will be thrown when the new protocol cannot be created, or inserted.
		/// </exception>
		public virtual void  insertProtocol(Protocol prot, int position, string neighbor_prot, ProtocolStack stack)
		{
			if (neighbor_prot == null)
				throw new System.Exception("Configurator.insertProtocol(): neighbor_prot is null");
			if (position != ProtocolStack.ABOVE && position != ProtocolStack.BELOW)
				throw new System.Exception("Configurator.insertProtocol(): position has to be ABOVE or BELOW");
			
			
			// find the neighbors below and above
			
			
			
			// connect to the protocol layer below and above
		}
		
		
		/// <summary> Removes a protocol from the stack. Stops the protocol and readjusts the linked lists of
		/// protocols.
		/// </summary>
		/// <param name="prot_name">The name of the protocol. Since all protocol names in a stack have to be unique
		/// (otherwise the stack won't be created), the name refers to just 1 protocol.
		/// </param>
		/// <exception cref=""> Exception Thrown if the protocol cannot be stopped correctly.
		/// </exception>
		public virtual void  removeProtocol(string prot_name)
		{
		}
		
		
		
		/* ------------------------------- Private Methods ------------------------------------- */

		/// <summary> Creates a protocol stack by iterating through the protocol list and connecting
		/// adjacent layers. The list starts with the topmost layer and has the bottommost
		/// layer at the tail. When all layers are connected the algorithms traverses the list
		/// once more to call startInternal() on each layer.
		/// </summary>
		/// <param name="protocol_list">List of Protocol elements (from top to bottom)
		/// </param>
		/// <returns> Protocol stack
		/// </returns>
		private Protocol connectProtocols(System.Collections.ArrayList protocol_list)
		{
			Protocol current_layer = null, next_layer = null;
			
			for (int i = 0; i < protocol_list.Count; i++)
			{
				current_layer = (Protocol) protocol_list[i];
				if (i + 1 >= protocol_list.Count)
					break;
				next_layer = (Protocol) protocol_list[i + 1];
				current_layer.UpProtocol = next_layer;
				next_layer.DownProtocol = current_layer;
			}
			return current_layer;
		}
		
		
		/// <summary> Get a string of the form "P1(config_str1):P2:P3(config_str3)" and return
		/// ProtocolConfigurations for it. That means, parse "P1(config_str1)", "P2" and
		/// "P3(config_str3)"
		/// </summary>
		/// <param name="config_str">Configuration string
		/// </param>
		/// <returns> Vector of ProtocolConfigurations
		/// </returns>
		public virtual System.Collections.ArrayList parseComponentStrings(string config_str, string delimiter)
		{
			System.Collections.ArrayList retval = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
			Tokenizer tok;
			string token;
			
			tok = new Tokenizer(config_str, delimiter);
			while (tok.HasMoreTokens())
			{
				token = tok.NextToken();
				retval.Add(token);
			}
			
			return retval;
		}
		
		
		/// <summary> Return a number of ProtocolConfigurations in a vector</summary>
		/// <param name="configuration">protocol-stack configuration string
		/// </param>
		/// <returns> Vector of ProtocolConfigurations
		/// </returns>
		public virtual System.Collections.ArrayList parseConfigurations(string configuration)
		{
			System.Collections.ArrayList retval = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
			System.Collections.ArrayList component_strings = parseComponentStrings(configuration, ":");
			string component_string;
			ProtocolConfiguration protocol_config;
			
			if (component_strings == null)
				return null;
			for (int i = 0; i < component_strings.Count; i++)
			{
				component_string = ((string) component_strings[i]);
				protocol_config = new ProtocolConfiguration(this, component_string);
				retval.Add(protocol_config);
			}
			return retval;
		}
		
		
		/// <summary> Takes vector of ProtocolConfigurations, iterates through it, creates Protocol for
		/// each ProtocolConfiguration and returns all Protocols in a vector.
		/// </summary>
		/// <param name="protocol_configs">Vector of ProtocolConfigurations
		/// </param>
		/// <param name="stack">The protocol stack
		/// </param>
		/// <returns> Vector of Protocols
		/// </returns>
		private System.Collections.ArrayList createProtocols(System.Collections.ArrayList protocol_configs, ProtocolStack stack)
		{
			System.Collections.ArrayList retval = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
			ProtocolConfiguration protocol_config;
			Protocol layer;
            stack.StackType = ProtocolStackType.TCP;
            for (int i = 0; i < protocol_configs.Count; i++)
            {
                protocol_config = (ProtocolConfiguration)protocol_configs[i];
                if (protocol_config != null)
                {
                    if (protocol_config.ProtocolName == "UDP")
                    {
                        stack.StackType = ProtocolStackType.UDP;
                        break;
                    }
                }
            }
            
			for (int i = 0; i < protocol_configs.Count; i++)
			{
				protocol_config = (ProtocolConfiguration) protocol_configs[i];
				layer = protocol_config.createLayer(stack );
				if (layer == null)
					return null;
				retval.Add(layer);
			}
			
			sanityCheck(retval);
			return retval;
		}
		
		
		/// <summary>Throws an exception if sanity check fails. Possible sanity check is uniqueness of all protocol
		/// names.
		/// </summary>
		public virtual void  sanityCheck(System.Collections.ArrayList protocols)
		{
			System.Collections.ArrayList names = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
			Protocol prot;
			string name;
			ProtocolReq req;
			System.Collections.ArrayList req_list = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
			int evt_type;
			
			// Checks for unique names
			for (int i = 0; i < protocols.Count; i++)
			{
				prot = (Protocol) protocols[i];
				name = prot.Name;
				for (int j = 0; j < names.Count; j++)
				{
					if (name.Equals(names[j]))
					{
						throw new System.Exception("Configurator.sanityCheck(): protocol name " + name + " has been used more than once; protocol names have to be unique !");
					}
				}
				names.Add(name);
			}
			
			
			// Checks whether all requirements of all layers are met
			for (int i = 0; i < protocols.Count; i++)
			{
				prot = (Protocol) protocols[i];
				req = new ProtocolReq(prot.Name);
				req.up_reqs = prot.requiredUpServices();
				req.down_reqs = prot.requiredDownServices();
				req.up_provides = prot.providedUpServices();
				req.down_provides = prot.providedDownServices();
				req_list.Add(req);
			}
			
			
			for (int i = 0; i < req_list.Count; i++)
			{
				req = (ProtocolReq) req_list[i];
				
				// check whether layers above this one provide corresponding down services
				if (req.up_reqs != null)
				{
					for (int j = 0; j < req.up_reqs.Count; j++)
					{
						evt_type = ((System.Int32) req.up_reqs[j]);
						
						if (!providesDownServices(i, req_list, evt_type))
						{
							throw new System.Exception("Configurator.sanityCheck(): event " + Event.type2String(evt_type) + " is required by " + req.name + ", but not provided by any of the layers above");
						}
					}
				}
				
				// check whether layers below this one provide corresponding up services
				if (req.down_reqs != null)
				{
					// check whether layers above this one provide up_reqs
					for (int j = 0; j < req.down_reqs.Count; j++)
					{
						evt_type = ((System.Int32) req.down_reqs[j]);
						
						if (!providesUpServices(i, req_list, evt_type))
						{
							throw new System.Exception("Configurator.sanityCheck(): event " + Event.type2String(evt_type) + " is required by " + req.name + ", but not provided by any of the layers below");
						}
					}
				}
			}
		}
		
		
		/// <summary>Check whether any of the protocols 'below' end_index provide evt_type </summary>
		public virtual bool providesUpServices(int end_index, System.Collections.ArrayList req_list, int evt_type)
		{
			ProtocolReq req;
			
			for (int i = 0; i < end_index; i++)
			{
				req = (ProtocolReq) req_list[i];
				if (req.providesUpService(evt_type))
					return true;
			}
			return false;
		}
		
		
		/// <summary>Checks whether any of the protocols 'above' start_index provide evt_type </summary>
		public virtual bool providesDownServices(int start_index, System.Collections.ArrayList req_list, int evt_type)
		{
			ProtocolReq req;
			
			for (int i = start_index; i < req_list.Count; i++)
			{
				req = (ProtocolReq) req_list[i];
				if (req.providesDownService(evt_type))
					return true;
			}
			return false;
		}
		
		
		
		/* --------------------------- End of Private Methods ---------------------------------- */
		
		
		
		
		
		private class ProtocolReq
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
						type = ((System.Int32) up_provides[i]);
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
						type = ((System.Int32) down_provides[i]);
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
						ret.Append(Event.type2String(((System.Int32) up_reqs[i])) + ' ');
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
						ret.Append(Event.type2String(((System.Int32) down_reqs[i])) + ' ');
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
						ret.Append(Event.type2String(((System.Int32) up_provides[i])) + ' ');
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
						ret.Append(Event.type2String(((System.Int32) down_provides[i])) + ' ');
				}
				return ret.ToString() + ']';
			}
		}
		
		
		/// <summary> Parses and encapsulates the specification for 1 protocol of the protocol stack, e.g.
		/// <code>UNICAST(timeout=5000)</code>
		/// </summary>
		internal class ProtocolConfiguration
		{
			private void  InitBlock(Configurator enclosingInstance)
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
					int index = value.IndexOf((System.Char) '('); // e.g. "UDP(in_port=3333)"
					int end_index = value.LastIndexOf((System.Char) ')');
					
					if (index == - 1)
					{
						protocol_name = value;
					}
					else
					{
						if (end_index == - 1)
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
								string name, valu, comp = (string) components[i];
								index = comp.IndexOf((System.Char) '=');
								if (index == - 1)
								{
									throw new System.Exception("Configurator.ProtocolConfiguration.setContents(): " + "'=' not found in " + comp);
								}
								name = comp.Substring(0, (index) - (0));
								valu = comp.Substring(index + 1, (comp.Length) - (index + 1));
								properties[(string) name] = (string) valu;
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
                        protocol = new TCP();
                        break;
                    case "TCPPING":
                        protocol = new TCPPING();
                        break;
                    case "QUEUE":
                        protocol = new QUEUE();
                        break;
                    case "TOTAL":
                        protocol = new TOTAL();
                        break;
                    case "VIEW_ENFORCER":
                        protocol = new VIEW_ENFORCER();
                        break;
                    case "pbcast.GMS":
                        protocol = new GMS();
                        break;                    
                }

                if (protocol != null)
				{
                    prot_stack.NCacheLog.Info("Configurator.createLayer()",  "Created Layer " + protocol.GetType().FullName);
                    protocol.Stack = prot_stack;
					if (properties != null)
                        if (!protocol.setPropertiesInternal(properties))
							return null;
                    protocol.init();
				}
				else
				{
                    prot_stack.NCacheLog.Error("Configurator.createLayer()",  "Couldn't create layer: " + protocol_name);
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
}