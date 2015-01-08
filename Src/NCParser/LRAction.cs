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

// C# Translation of GoldParser, by Marcus Klimstra <klimstra@home.nl>.
// Based on GOLDParser by Devin Cook <http://www.devincook.com/goldparser>.
namespace Alachisoft.NCache.Parser
{
	/// This class represents an action in a LALR State. 
	/// There is one and only one action for any given symbol.
	internal class LRAction
	{
		private Symbol		m_symbol;
		private Action		m_action;
		private int			m_value;
		
		/* properties */

		public Symbol Symbol
		{
			get { return m_symbol; }
			set { m_symbol = value; }
		}
		
		public Action Action
		{
			get { return m_action; }
			set { m_action = value; }
		}
		
		public int Value
		{
			get { return m_value; }
			set { m_value = value; }
		}
		
		/* public methods */
		
		public override String ToString()
		{
			return "LALR action [symbol=" + m_symbol + ",action=" + m_action + ",value=" + m_value + "]";
		}		
	}
}
