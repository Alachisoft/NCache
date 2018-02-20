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
using System.Collections;

// C# Translation of GoldParser, by Marcus Klimstra <klimstra@home.nl>.
// Based on GOLDParser by Devin Cook <http://www.devincook.com/goldparser>.
namespace Alachisoft.NCache.Parser
{
    /// This class is used by the engine to hold a reduced rule. Rather than contain
    /// a list of Symbols, a reduction contains a list of Tokens corresponding to the
    /// the rule it represents. This class is important since it is used to store the
    /// actual source program parsed by the Engine.
    public class Reduction
	{
		private ArrayList	m_tokens;
		private Rule		m_parentRule;
		private Object		m_tag;

		/* constructor */
		
		/// Creates a new Reduction.
		public Reduction()
		{
			m_tokens = new ArrayList();
		}

		/* properties */
		
		/// Returns an <c>ArrayList</c> containing the <c>Token</c>s in this reduction.
		public ArrayList Tokens
		{
			get { return m_tokens; }
		}
		
		/// Returns the <c>Rule</c> that this <c>Reduction</c> represents.
		public Rule ParentRule
		{
			get { return m_parentRule; }
			set { m_parentRule = value; }
		}
		
		/// This is a general purpose field that can be used at the developer's leisure. 
		public Object Tag
		{
			get { return m_tag; }
			set { m_tag = value; }
		}

		/* public methods */
		
		/// Returns the token with the specified index.
		public Token GetToken(int p_index)
		{
			return (Token)m_tokens[p_index];
		}
		
		/// Returns a string-representation of this Reduction.
		public override String ToString()
		{
			return m_parentRule.ToString();
		}
		
		/// <summary>Makes the <c>IGoldVisitor</c> visit this <c>Reduction</c>.</summary>
		/// <example>See the GoldTest sample project.</example>
		public void Accept(IGoldVisitor p_visitor)
		{
			p_visitor.Visit(this);			
		}
		
		/// <summary>Makes the <c>IGoldVisitor</c> visit the children of this 
		///          <c>Reduction</c>.</summary>
		/// <example>See the GoldTest sample project.</example>
		public void ChildrenAccept(IGoldVisitor p_visitor)
		{
			foreach (Token token in m_tokens)
			{
				if (token.Kind == SymbolType.NonTerminal)
					(token.Data as Reduction).Accept(p_visitor);
			}
		}
		
		/* internal methods */

		internal void AddToken(Token p_token)
		{
			m_tokens.Add(p_token);
		}
	}
}
