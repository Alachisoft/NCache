using System;
using System.Collections;
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NGroups
{
	/// <remarks>
	/// Coupled with an <c>ArrayList</c>, this class extends its facilites  and adds
	/// extra constraints
	/// </remarks>
	/// <summary>
	/// Used by the GMS to store the current members in the group
	/// <p><b>Author:</b> Chris Koiak, Bela Ban</p>
	/// <p><b>Date:</b>  12/03/2003</p>
	/// </summary>
	internal class Membership
	{
		/// <summary>
		/// List of current members
		/// </summary>
		private ArrayList members = null;

		/// <summary>
		/// Constructor: Initialises with no initial members
		/// </summary>
		public Membership()
		{
			members = new ArrayList(11);
			members = ArrayList.Synchronized(members);
		}

		/// <summary>
		/// Constructor: Initialises with the specified initial members 
		/// </summary>
		/// <param name="initial_members">Initial members of the membership</param>
		public Membership(ArrayList initial_members)
		{
			members=new ArrayList();
			if(initial_members != null)
				members = (ArrayList)initial_members.Clone();
			members = ArrayList.Synchronized(members);
		}

		/// <summary> returns a copy (clone) of the members in this membership.
		/// the vector returned is immutable in reference to this object.
		/// ie, modifying the vector that is being returned in this method
		/// will not modify this membership object.
		/// 
		/// </summary>
		/// <returns> a list of members,
		/// </returns>
		virtual public System.Collections.ArrayList Members
		{
			get
			{
				/*clone so that this objects members can not be manipulated from the outside*/
				return (ArrayList)members.Clone();
			}
			
		}

		/// <summary>
		/// Sets the members to the specified list
		/// </summary>
		/// <param name="membrs">The current members</param>
		public void setMembers(ArrayList membrs)
		{
			members = membrs;
		}

		/// <remarks>
		/// If the member already exists then the member will
		/// not be added to the membership
		/// </remarks>
		/// <summary>
		/// Adds a new member to this membership.
		/// </summary>
		/// <param name="new_member"></param>
		public void add(Address new_member)
		{
			if(new_member != null && !members.Contains(new_member))
			{
				members.Add(new_member);
			}
		}

		/// <summary>
		/// Adds a number of members to this membership
		/// </summary>
		/// <param name="v"></param>
		public void add(ArrayList v)
		{
			if(v != null)
			{
				for(int i=0; i < v.Count; i++)
				{
					add((Address)v[i]);
				}
			}
		}

		/// <summary>
		/// Removes the specified member
		/// </summary>
		/// <param name="old_member">Member that has left the group</param>
		public void remove(Address old_member)
		{
			if(old_member != null)
			{
				members.Remove(old_member);
			}
		}

		/// <summary> merges membership with the new members and removes suspects
		/// The Merge method will remove all the suspects and add in the new members.
		/// It will do it in the order
		/// 1. Remove suspects
		/// 2. Add new members
		/// the order is very important to notice.
		/// 
		/// </summary>
		/// <param name="new_mems">- a vector containing a list of members (Address) to be added to this membership
		/// </param>
		/// <param name="suspects">- a vector containing a list of members (Address) to be removed from this membership
		/// </param>
		public virtual void  merge(System.Collections.ArrayList new_mems, System.Collections.ArrayList suspects)
		{
			lock (this)
			{
				remove(suspects);
				add(new_mems);
			}
		}

		/* Simple inefficient bubble sort, but not used very often (only when merging) */
		public virtual void  sort()
		{
			lock (this)
			{
				members.Sort();
			}
		}

		/// <summary>
		/// Removes a number of members from the membership
		/// </summary>
		/// <param name="v"></param>
		public void remove(ArrayList v)
		{
			if(v != null)
			{
				for(int i=0; i < v.Count; i++)
				{
					remove((Address)v[i]);
				}
			}
		}

		/// <summary>
		/// Removes all members
		/// </summary>
		public void clear()
		{
			members.Clear();
		}

		/// <summary>
		/// Sets the membership to the members present in the list
		/// </summary>
		/// <param name="v">New list of members</param>
		public void set(ArrayList v)
		{
			clear();
			if (v != null)
			{
				add(v);
			}
		}

		/// <summary>
		/// Sets the membership to the specified membership
		/// </summary>
		/// <param name="m">New membership</param>
		public void set(Membership m)
		{
			clear();
			if (m != null)
			{
				add(m.Members);
			}
		}

		/// <summary>
		/// Returns true if the provided member belongs to this membership
		/// </summary>
		/// <param name="member">Member to check</param>
		/// <returns>True if the provided member belongs to this membership, otherwise false</returns>
		public bool contains(Address member)
		{
			if(member == null)
				return false;
			return members.Contains(member);
		}

		/// <summary>
		/// Returns a copy of this membership.
		/// </summary>
		/// <returns>A copy of this membership</returns>
		public Membership copy()
		{
			return (Membership)this.Clone();
		}

        /// <summary>
        /// Determines the seniority between two given nodes. Seniority is based on
        /// the joining time. If n1 has joined before than n2, n1 will be considered
        /// senior.
        /// </summary>
        /// <param name="n1">node 1</param>
        /// <param name="n2">node 2</param>
        /// <returns>senior node</returns>
        public Address DetermineSeniority(Address n1, Address n2)
        {
            int indexofn1 = members.IndexOf(n1);
            int indexofn2 = members.IndexOf(n2);

            if (indexofn1 == -1) indexofn1 = int.MaxValue;
            if (indexofn2 == -1) indexofn2 = int.MaxValue;
            
            //smaller the index of a node means that this node has joined first.
            return indexofn1 <= indexofn2 ? n1 : n2;
        }

		/// <summary>
		/// Clones the membership
		/// </summary>
		/// <returns>A clone of the membership</returns>
		public Object Clone()
		{
			Membership m;
			m = new Membership();
			m.setMembers((ArrayList)members.Clone());
			return(m);
		}

		/// <summary>
		/// The number of members in the membership
		/// </summary>
		/// <returns>Number of members in the membership</returns>
		public int size()
		{
			return members.Count;
		}

		/// <summary>
		/// Gets a member at a specified index
		/// </summary>
		/// <param name="index">Index of member</param>
		/// <returns>Address of member</returns>
		public Address elementAt(int index)
		{
			if(index<members.Count)
				return (Address)members[index];
			else
				return null;
		}

		/// <summary>
		/// String representation of the Membership object
		/// </summary>
		/// <returns>String representation of the Membership object</returns>
		public String toString()
		{
			return members.ToString();
		}

        public override bool Equals(object obj)
        {
            bool equal = true;
            Membership membership = obj as Membership;
            if (membership != null && this.size() == membership.size())
            {
                foreach (Address address in membership.members)
                {
                    if (!this.contains(address))
                    {
                        equal = false;
                        break;
                    }
                }
            }
            else 
            {
                equal = false;
            }
            return equal;
        }

        public bool ContainsIP(Address address)
        {
            if (address == null)
                return false;
            bool contains = false;
            foreach (Address add in members)
            {
                if (add.IpAddress.Equals(address.IpAddress))
                {
                    contains = true;
                }
            }
            return contains;
        }

	}
}
