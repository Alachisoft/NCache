// $Id: VIEW_ENFORCER.java,v 1.3 2004/04/23 19:36:13 belaban Exp $
using System;
using Alachisoft.NGroups;
using Alachisoft.NGroups.Util;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NGroups.Stack;

namespace Alachisoft.NGroups.Protocols
{
	
	
	/// <summary> Used by a client until it becomes a member: all up messages are discarded until a VIEW_CHANGE
	/// is encountered. From then on, this layer just acts as a pass-through layer. Later, we may
	/// add some functionality that checks for the VIDs of messages and discards messages accordingly.
	/// </summary>
	
	internal class VIEW_ENFORCER:Protocol
	{
		/// <summary> All protocol names have to be unique !</summary>
		override public string Name
		{
			get
			{
				return "VIEW_ENFORCER";
			}
			
		}

		public VIEW_ENFORCER()
		{
			this.up_thread = false;
			this.down_thread = false;
		}

		internal Address local_addr = null;
		internal bool is_member = false;


        public override bool setProperties(System.Collections.Hashtable props)
        {
            if (stack.StackType == ProtocolStackType.TCP)
            {
                this.up_thread = false;
                this.down_thread = false;
                Stack.NCacheLog.Info(Name + ".setProperties",  "part of TCP stack");
            }
            return true;
        }
		public override void  up(Event evt)
		{
			
			switch (evt.Type)
			{
				case Event.VIEW_CHANGE: 
					if (is_member)
					// pass the view change up if we are already a member of the group
						break;
					
					System.Collections.ArrayList new_members = ((View) evt.Arg).Members;
					if (new_members == null || new_members.Count == 0)
						break;
					if (local_addr == null)
					{
                        Stack.NCacheLog.Error("VIEW_ENFORCER.up(VIEW_CHANGE): local address is null; cannot check " + "whether I'm a member of the group; discarding view change");
						return ;
					}
					
					if (new_members.Contains(local_addr))
						is_member = true;
					else
						return ;
					
					break;
				
				
				case Event.SET_LOCAL_ADDRESS: 
					local_addr = (Address) evt.Arg;
					break;
				
				
				
				case Event.MSG: 
					if (!is_member)
					{
						// drop message if we are not yet member of the group
                        Stack.NCacheLog.Info("dropping message " + evt.Arg);
						return ;
					}
                    Message msg = evt.Arg as Message;
                    if (msg != null && msg.HandledAysnc)
                    {
                        System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(AsyncPassUp), evt);
                        return;
                    }
                    
					break;
				}
			passUp(evt); // Pass up to the layer above us
		}

        /// <summary>
        /// Threadpool calls this method to pass data up the stack.
        /// </summary>
        /// <param name="evt"></param>
        public void AsyncPassUp(object evt)
        {
            passUp((Event)evt);
        }
	}
}