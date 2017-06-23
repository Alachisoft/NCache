// Gold Parser engine.
// See more details on http://www.devincook.com/goldparser/
// 
// Original code is written in VB by Devin Cook (GOLDParser@DevinCook.com)
//
// This translation is done by Vladimir Morozov (vmoroz@hotmail.com)
// 
// The translation is based on the other engine translations:
// Delphi engine by Alexandre Rai (riccio@gmx.at)
// C# engine by Marcus Klimstra (klimstra@home.nl)

using System;
using System.Text;
using System.Collections;

// C# Translation of GoldParser, by Marcus Klimstra <klimstra@home.nl>.
// Based on GOLDParser by Devin Cook <http://www.devincook.com/goldparser>.
namespace Alachisoft.NCache.Parser
{
	/// This class contains the actions (reduce/shift) and goto information
	/// for a STATE in a LR parser. Essentially, this is just a row of actions in
	/// the LR state transition table. The only data structure is a list of
	/// LR Actions.
	internal class LRActionTable
	{
		private ArrayList	m_members;
		
		/* constructor */
		
		public LRActionTable()
		{
			m_members = new ArrayList();
		}
		
		/* properties */
		
		public int Count
		{
			get { return m_members.Count; }
		}
		
		public ArrayList Members
		{
			get { return m_members; }
		}
		
		/* public methods */
		
		public LRAction GetActionForSymbol(int p_symbolIndex)
		{
			// kan met hashtable bv.
			
			foreach (LRAction action in m_members)
			{
				if (action.Symbol.TableIndex == p_symbolIndex)
					return action;
			}			
			
			return null;
		}
		
		public LRAction GetItem(int p_index)
		{
			if (p_index >= 0 && p_index < m_members.Count) 
				return (LRAction)m_members[p_index];
			else
				return null;
		}
		
		/// <summary>Adds an new LRAction to this table.</summary>
		/// <param name="p_symbol">The Symbol.</param>
		/// <param name="p_action">The Action.</param>
		/// <param name="p_value">The value.</param>
		public void AddItem(Symbol p_symbol, Action p_action, int p_value)
		{
			LRAction item = new LRAction();
			item.Symbol = p_symbol;
			item.Action = p_action;
			item.Value = p_value;
			m_members.Add(item);
		}
		
		public override String ToString()
		{
			StringBuilder result = new StringBuilder();
			result.Append("LALR table:\n");
			foreach (LRAction action in m_members)
			{
				result.Append("- ").Append(action.ToString() + "\n");
			}
			return result.ToString();
		}
	}
}
