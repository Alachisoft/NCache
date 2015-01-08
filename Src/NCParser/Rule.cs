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
	/// The Rule class is used to represent the logical structures of the grammar.
	/// Rules consist of a head containing a nonterminal followed by a series of
	/// both nonterminals and terminals.
	public class Rule
	{
		private Symbol		m_ruleNT;		// non-terminal rule
		private ArrayList	m_ruleSymbols;
		private int			m_tableIndex;
		
		/* constructor */
		
		/// Creates a new Rule.
		internal Rule(int p_tableIndex, Symbol p_head)
		{
			m_ruleSymbols = new ArrayList();
			m_tableIndex = p_tableIndex;
			m_ruleNT = p_head;
		}
		
		/* public properties */
		
		/// Gets the index of this <c>Rule</c> in the GoldParser's rule-table.
		public int TableIndex
		{
			get { return m_tableIndex; }
		}
		
		/// Gets the head symbol of this rule.
		public Symbol RuleNonTerminal
		{
			get { return m_ruleNT; }
		}		

		/// Gets the number of symbols in the body (right-hand-side) of the rule.
		public int SymbolCount
		{
			get { return m_ruleSymbols.Count; }
		}
		
		/* internal properties */
		
		/// The name of this rule.
		internal String Name
		{
			get { return "<" + m_ruleNT.Name + ">"; }
		}
		
		/// The definition of this rule.
		internal String Definition
		{
			get
			{
				StringBuilder result = new StringBuilder();
				IEnumerator enumerator = m_ruleSymbols.GetEnumerator();
				
				while (enumerator.MoveNext())
				{
					Symbol symbol = (Symbol)enumerator.Current;
					result.Append(symbol.ToString()).Append(" ");
				}
				
				return result.ToString();
			}
		}

		///
		internal bool ContainsOneNonTerminal
		{
			get 
			{
				return m_ruleSymbols.Count == 1 && 
				       ((Symbol)m_ruleSymbols[0]).Kind == 0;
			}
		}
		
		/* public methods */
		
		/// Returns the symbol in the body of the rule with the specified index. 
		public Symbol GetSymbol(int p_index)
		{
			if (p_index >= 0 && p_index < m_ruleSymbols.Count)
				return (Symbol)m_ruleSymbols[p_index];
			else
				return null;
		}
		
		/// Returns the Backus-Noir representation of this <c>Rule</c>.
		public override String ToString()
		{
			return Name + " ::= " + Definition;
		}
		
		// equals ?

		///
		internal void AddItem(Symbol p_symbol)
		{
			m_ruleSymbols.Add(p_symbol);
		}		
	}
}
