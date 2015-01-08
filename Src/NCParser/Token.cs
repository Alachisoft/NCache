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
	/// While the Symbol represents a class of terminals and nonterminals,
	/// the Token represents an individual piece of information.
	public class Token : Symbol
	{
		private int		m_state;
		private Object 	m_data;
		
		/* constructors */
		
		///
		internal Token()
		{
			m_state = -1;
			m_data = "";
		}
		
		///
		internal Token(Symbol p_symbol)
		:	this()
		{
			SetParent(p_symbol);
		}
		
		/* properties */
		
		/// Gets the state 
		internal int State
		{
			get { return m_state; }
			set { m_state = value; }
		}
		
		/// Gets or sets the information stored in the token.
		public Object Data
		{
			get { return m_data; }
			set { m_data = value; }
		}
		
		/* public methods */
		
		/// 
		internal void SetParent(Symbol p_symbol)
		{
			CopyData(p_symbol);
		}
		
		/// <summary>Returns the text representation of the token's parent symbol.</summary>
		/// <remarks>In the case of nonterminals, the name is delimited by angle brackets, 
		/// special terminals are delimited by parenthesis and terminals are delimited 
		/// by single quotes (if special characters are present).</remarks>
		public override String ToString()
		{
			return base.ToString();
		}
	}
}
