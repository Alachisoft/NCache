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
	/// Each state in the Deterministic Finite Automate contains multiple edges which
	/// link to other states in the automate. This class is used to represent an edge.
	internal class FAEdge
	{
		private String	m_characters;
		private int		m_targetIndex;
		
		/* constructor */

		public FAEdge(String p_characters, int p_targetIndex)
		{
			m_characters = p_characters;
			m_targetIndex = p_targetIndex;
		}
		
		/* properties */

		public String Characters
		{
			get { return m_characters; }
			set { m_characters = value; }
		}

		public int TargetIndex
		{
			get { return m_targetIndex; }
			set { m_targetIndex = value; }
		}
		
		/* public methods */

		public void AddCharacters(String p_characters)
		{
			m_characters = m_characters + p_characters;
		}

		public override String ToString()
		{
			return "DFA edge [chars=[" + m_characters + "],action=" + m_targetIndex + "]";
		}
	}
}
