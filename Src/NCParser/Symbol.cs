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

// C# Translation of GoldParser, by Marcus Klimstra <klimstra@home.nl>.
// Based on GOLDParser by Devin Cook <http://www.devincook.com/goldparser>.
namespace Alachisoft.NCache.Parser
{
	/// This class is used to store the nonterminals used by the DFA and LALR parser
	/// Symbols can be either terminals (which represent a class of tokens, such as
	/// identifiers) or non-terminals (which represent the rules and structures of
	/// the grammar). Symbols fall into several categories for use by the 
	/// GoldParser Engine which are enumerated in type <c>SymbolType</c> enum.
	public class Symbol
	{
		private const String c_quotedChars = "|-+*?()[]{}<>!\x0022";
		
		private int			m_tableIndex;
		private String		m_name;
		private SymbolType	m_kind;

		/* constructor */
		
		/// Creates a new Symbol object.
		internal Symbol(int p_index, String p_name, SymbolType p_kind)
		{
			m_tableIndex = p_index;
			m_name = p_name;
			m_kind = p_kind;
		}
		
		///
		internal protected Symbol()
		:	this(-1, "", SymbolType.Error)
		{			
		}
		
		/* properties */
		
		/// Gets the index of this symbol in the GoldParser's symbol table.
		public int TableIndex
		{
			get { return m_tableIndex; }
		}
		
		/// Gets the name of the symbol.
		public String Name
		{
			get { return m_name; }
		}
		
		/// Gets the <c>SymbolType</c> of the symbol.
		public SymbolType Kind
		{
			get { return m_kind; }
		}
		
		/* public methods */
		
		/// Returns true if the specified symbol is equal to this one.
		public override bool Equals (Object p_object)
		{
			Symbol symbol = (Symbol)p_object;
			return m_name.Equals(symbol.Name) && m_kind == symbol.Kind;
		}
		
		/// Returns the hashcode for the symbol.
		public override int GetHashCode()
		{
			return (m_name + "||" + m_kind).GetHashCode();
		}

		/// <summary>Returns the text representation of the symbol.</summary>
		/// <remarks>In the case of nonterminals, the name is delimited by angle brackets, 
		/// special terminals are delimited by parenthesis and terminals are delimited 
		/// by single quotes (if special characters are present).</remarks>
		public override String ToString()
		{
			StringBuilder result = new StringBuilder();

			if (m_kind == SymbolType.NonTerminal)
				result.Append("<").Append(m_name).Append(">");
			else if (m_kind == SymbolType.Terminal)
				/* PatternFormat(m_name, result); */ result.Append(m_name);
			else
				result.Append("(").Append(m_name).Append(")");
				
			return result.ToString();
		}

		/* private methods */		

		///
		private void PatternFormat(String p_source, StringBuilder p_target)
		{
			for (int i = 0; i < p_source.Length; i++)
			{
				Char ch = p_source[i];
				if (ch == '\'') 
					p_target.Append("''");
				else if (c_quotedChars.IndexOf(ch) != -1)
					p_target.Append("'").Append(ch).Append("'");
				else
					p_target.Append(ch);
			}
		}
		
		///
		internal protected void CopyData(Symbol p_symbol)
		{
			m_name = p_symbol.Name;
			m_kind = p_symbol.Kind;
			m_tableIndex = p_symbol.TableIndex;			
		}
	}
}
