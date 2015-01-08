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
	///
	public class TokenStack
	{
		private ArrayList	m_items;
		
		/* constructor */
		
		///
		internal TokenStack()
		{
			m_items = new ArrayList();
		}
		
		/* indexer */
		
		/// Returns the token at the specified position from the top.
		public Token this[int p_index]
		{
			get { return (Token)m_items[p_index]; }
		}
		
		/* properties */
		
		/// Gets the number of items in the stack.
		public int Count
		{
			get { return m_items.Count; }
		}
				
		/* public methods */
		
		/// Removes all tokens from the stack.
		public void Clear()
		{
			m_items.Clear();
		}
		
		/// Pushes the specified token on the stack.
		public void PushToken(Token p_token)
		{
			m_items.Add(p_token);
		}
		
		/// Returns the token on top of the stack.
		public Token PeekToken()
		{
			int last = m_items.Count - 1;
			return (last < 0 ? null : (Token)m_items[last]);
		}
		
		/// <summary>Pops a token from the stack.</summary>
		/// <remarks>The token on top of the stack will be removed and returned 
		/// by the method.</remarks>
		public Token PopToken()
		{
			int last = m_items.Count - 1;
			if (last < 0) return null;
			Token result = (Token)m_items[last];
			m_items.RemoveAt(last);
			return result;
		}
		
		/// Pops the specified number of tokens from the stack and adds them
		/// to the specified <c>Reduction</c>.
		public void PopTokensInto(Reduction p_reduction, int p_count)
		{
			int start = m_items.Count - p_count;
			int end = m_items.Count;
			
			for (int i = start; i < end; i++)
				p_reduction.AddToken((Token)m_items[i]);
				
			m_items.RemoveRange(start, p_count);
		}
		
		/// <summary>Returns the token at the specified position from the top.</summary>
		/// <example>GetToken(0) returns the token on top off the stack, GetToken(1)
		/// the next one, etc.</example>
		public Token GetToken(int p_index)
		{
			return (Token)m_items[p_index];
		}
		
		/// Returns an <c>IEnumerator</c> for the tokens on the stack.
		public IEnumerator GetEnumerator()
		{
			return m_items.GetEnumerator();
		}
	}
}
