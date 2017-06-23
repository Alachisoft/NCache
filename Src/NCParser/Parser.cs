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
using System.IO;
using System.Text;
using System.Collections;
using Console = System.Console;

// C# Translation of GoldParser, by Marcus Klimstra <klimstra@home.nl>.
// Based on GOLDParser by Devin Cook <http://www.devincook.com/goldparser>.
namespace Alachisoft.NCache.Parser
{
	/// This is the main class in the GoldParser Engine and is used to perform
	/// all duties required to the parsing of a source text string. This class
	/// contains the LALR(1) State Machine code, the DFA State Machine code,
	/// character table (used by the DFA algorithm) and all other structures and
	/// methods needed to interact with the developer.
	public class Parser
	{		
		private Hashtable		m_parameters;
		private Symbol[]		m_symbols;
		private String[]		m_charsets;
		private Rule[]			m_rules;
		private FAState[]		m_DfaStates;
		private LRActionTable[]	m_LalrTables;
	
		private bool			m_initialized;
		private bool			m_caseSensitive;
		private int				m_startSymbol;
		private int				m_initDfaState;
		private Symbol			m_errorSymbol;
		private Symbol			m_endSymbol;
		private LookAheadReader	m_source;	
	
		private int				m_lineNumber;
		private bool			m_haveReduction;
		private bool			m_trimReductions;
		private int				m_commentLevel;
		private int				m_initLalrState;
		private int				m_LalrState; 

		private TokenStack		m_inputTokens;	// Stack of tokens to be analyzed
		private TokenStack		m_outputTokens;	// The set of tokens for 1. Expecting during error, 2. Reduction
		private TokenStack		m_tempStack;	// I often dont know what to call variables. 

		/* constructor */
		
		public Parser()
		{
		}

		/// <summary>Creates a new <c>Parser</c> object for the specified 
		///          CGT file.</summary>
		/// <param name="p_filename">The name of the CGT file.</param>
		public Parser(String p_filename)
		{
			LoadGrammar(p_filename);
		}

		/// <summary>Creates a new <c>Parser</c> object for the specified 
		///          CGT file.</summary>
		/// <param name="p_filename">The name of the CGT file.</param>
		public Parser(Stream stream)
		{
			LoadGrammar(stream);
		}

		public void LoadGrammar(String p_filename)
		{
			m_parameters = new Hashtable();
			m_inputTokens = new TokenStack();
			m_outputTokens = new TokenStack();
			m_tempStack = new TokenStack();
			m_initialized = false;
			m_trimReductions = false;
			
			LoadTables(new GrammarReader(p_filename));
		}

		public void LoadGrammar(Stream stream)
		{
			m_parameters = new Hashtable();
			m_inputTokens = new TokenStack();
			m_outputTokens = new TokenStack();
			m_tempStack = new TokenStack();
			m_initialized = false;
			m_trimReductions = false;
			
			LoadTables(new GrammarReader(stream));
		}

		/* properties */

		/// Gets or sets whether or not to trim reductions which contain 
		/// only one non-terminal.
		public bool TrimReductions
		{
			get { return m_trimReductions; }
			set { m_trimReductions = value; }
		}

		/// Gets the current token.
		public Token CurrentToken
		{
			get { return m_inputTokens.PeekToken(); }
		}
		
		/// <summary>Gets the <c>Reduction</c> made by the parsing engine.</summary>
		/// <remarks>The value of this property is only valid when the Parse-method
		///          returns <c>ParseMessage.Reduction</c>.</remarks>
		public Reduction CurrentReduction
		{
			get
			{
				if (m_haveReduction)
				{					
					Token token = m_tempStack.PeekToken();
					return (token.Data as Reduction);
				}
				else
					return null;
			}
			set
			{
				if (m_haveReduction)
				{
					m_tempStack.PeekToken().Data = value;
				}
			}
		}
		
		/// Gets the line number that is currently being processed.
		public int CurrentLineNumber
		{
			get { return m_lineNumber; }
		}
		
		/* public methods */
		
		/// Pushes the specified token onto the internal input queue. 
		/// It will be the next token analyzed by the parsing engine.
		public void PushInputToken(Token p_token)
		{
			m_inputTokens.PushToken(p_token);
		}
		
		/// Pops the next token from the internal input queue.
		public Token PopInputToken()
		{
			return m_inputTokens.PopToken();
		}
	/*	
		/// Returns the token at the specified index.
		public Token GetToken(int p_index)
		{
			return m_outputTokens.GetToken(p_index);
		}
	*/	
		/// Returns a <c>TokenStack</c> containing the tokens for the reduced rule or
		/// the tokens that where expected when a syntax error occures.
		public TokenStack GetTokens()
		{
			return m_outputTokens;
		}
		
		/// <summary>Returns a string containing the value of the specified parameter.</summary>
		/// <remarks>These parameters include: Name, Version, Author, About, Case Sensitive 
		/// and Start Symbol. If the name specified is invalid, this method will 
		/// return an empty string.</remarks>
		public String GetParameter(String p_name)
		{
			String result = (String)m_parameters[p_name];
			return (result != null ? result : "");
		}

		/// Opens the file with the specified name for parsing.
		public void OpenFile(String p_filename)
		{
			Reset();
			
			m_source = new LookAheadReader(
				new StreamReader(new FileStream(p_filename, FileMode.Open)));
			
			PrepareToParse();
		}
		
		/// Opens the file with the specified name for parsing.
		public void OpenStream(TextReader stream)
		{
			Reset();
			
			m_source = new LookAheadReader(stream);
			
			PrepareToParse();
		}
		/// Closes the file opened with <c>OpenFile</c>.
		public void CloseFile()
		{
			if (m_source != null)
				m_source.Close(); 
			
			m_source = null;
		}

		/// <summary>Executes a parse-action.</summary>
		/// <remarks>When this method is called, the parsing engine 
		/// reads information from the source text and then reports what action was taken. 
		/// This ranges from a token being read and recognized from the source, a parse 
		/// reduction, or some type of error.</remarks>
		public ParseMessage Parse()
		{
			while (true)
			{
				if (m_inputTokens.Count == 0)
				{	
					// we must read a token.				
					
					Token token = RetrieveToken();
					
					if (token == null)
						throw new ParserException("RetrieveToken returned null"); 
					
					if (token.Kind != SymbolType.Whitespace)
					{
						m_inputTokens.PushToken(token);
						
						if (m_commentLevel == 0 && ! CommentToken(token))
							return ParseMessage.TokenRead;
					}
				}
				else if (m_commentLevel > 0)
				{
					// we are in a block comment.
					
					Token token = m_inputTokens.PopToken();
					
					switch (token.Kind)
					{
					case SymbolType.CommentStart:
						m_commentLevel++;
						break;
					case SymbolType.CommentEnd:
						m_commentLevel--;
						break;
					case SymbolType.End:
						return ParseMessage.CommentError;
					}
				}
				else
				{
					// we are ready to parse.
					
					Token token = m_inputTokens.PeekToken();					
					switch (token.Kind)
					{
					case SymbolType.CommentStart:
						m_inputTokens.PopToken();
						m_commentLevel++;
						break;
					case SymbolType.CommentLine:
						m_inputTokens.PopToken();
						DiscardLine();
						break;
					default:
						ParseResult result = ParseToken(token);						
						switch (result)
						{
						case ParseResult.Accept:
							return ParseMessage.Accept;
						case ParseResult.InternalError:
							return ParseMessage.InternalError;
						case ParseResult.ReduceNormal:
							return ParseMessage.Reduction;
						case ParseResult.Shift:
							m_inputTokens.PopToken();
							break;
						case ParseResult.SyntaxError:
							return ParseMessage.SyntaxError;
						}
						break;
					} // switch
				} // else
			} // while
		}

		/* private methods */
				
		private char FixCase(char p_char)
		{
			if (m_caseSensitive)
				return p_char;
				
			return Char.ToLower(p_char);
		}
		
		private String FixCase(String p_string)
		{
			if (m_caseSensitive)
				return p_string;
				
			return p_string.ToLower();
		}
		
		private void AddSymbol(Symbol p_symbol)
		{
			if (! m_initialized)
				throw new ParserException("Table sizes not initialized");

			int index = p_symbol.TableIndex;
			m_symbols[index] = p_symbol;
		}
		
		private void AddCharset(int p_index, String p_charset)
		{
			if (! m_initialized)
				throw new ParserException("Table sizes not initialized");

			m_charsets[p_index] = FixCase(p_charset);
		}
		
		private void AddRule(Rule p_rule)
		{
			if (! m_initialized)
				throw new ParserException("Table sizes not initialized");

			int index = p_rule.TableIndex;
			m_rules[index] = p_rule;
		}
		
		private void AddDfaState(int p_index, FAState p_fastate)
		{
			if (! m_initialized)
				throw new ParserException("Table sizes not initialized");

			m_DfaStates[p_index] = p_fastate;
		}
		
		private void AddLalrTable(int p_index, LRActionTable p_table)
		{
			if (! m_initialized)
				throw new ParserException("Table counts not initialized");

			m_LalrTables[p_index] = p_table;
		}
		
		private void LoadTables(GrammarReader reader)
		{
			Object obj; Int16 index;
			while (reader.MoveNext())
			{
				byte id = (byte)reader.RetrieveNext();

				switch ((RecordId)id)
				{
				case RecordId.Parameters:
					m_parameters["Name"] 	= (String)reader.RetrieveNext();
					m_parameters["Version"]	= (String)reader.RetrieveNext();
					m_parameters["Author"]	= (String)reader.RetrieveNext();
					m_parameters["About"]	= (String)reader.RetrieveNext();
					m_caseSensitive			= (Boolean)reader.RetrieveNext();
					m_startSymbol			= (Int16)reader.RetrieveNext();
					break;

				case RecordId.TableCounts:
					m_symbols		= new Symbol[(Int16)reader.RetrieveNext()];
					m_charsets		= new String[(Int16)reader.RetrieveNext()];
					m_rules			= new Rule[(Int16)reader.RetrieveNext()];
					m_DfaStates		= new FAState[(Int16)reader.RetrieveNext()];
					m_LalrTables	= new LRActionTable[(Int16)reader.RetrieveNext()];
					m_initialized = true;
					break;
					
				case RecordId.Initial:
					m_initDfaState		= (Int16)reader.RetrieveNext();
					m_initLalrState		= (Int16)reader.RetrieveNext();
					break;
					
				case RecordId.Symbols:
					index				= (Int16)reader.RetrieveNext();
					String name			= (String)reader.RetrieveNext();
					SymbolType kind		= (SymbolType)(Int16)reader.RetrieveNext();
					Symbol symbol = new Symbol(index, name, kind);
					AddSymbol(symbol);
					break;
					
				case RecordId.CharSets:
					index 				= (Int16)reader.RetrieveNext();
					String charset		= (String)reader.RetrieveNext();
					AddCharset(index, charset);
					break;
					
				case RecordId.Rules:
					index				= (Int16)reader.RetrieveNext();
					Symbol head			= m_symbols[(Int16)reader.RetrieveNext()];
					Rule rule = new Rule(index, head);

					reader.RetrieveNext();	// reserved
					while ((obj = reader.RetrieveNext()) != null)
						rule.AddItem(m_symbols[(Int16)obj]);
						
					AddRule(rule);
					break;
					
				case RecordId.DFAStates:
					FAState fastate = new FAState();
					index	= (Int16)reader.RetrieveNext();

					if ((bool)reader.RetrieveNext())
						fastate.AcceptSymbol	= (Int16)reader.RetrieveNext();
					else
						reader.RetrieveNext();
						
					reader.RetrieveNext();	// reserverd					

					while (! reader.RetrieveDone())
					{
						Int16 ci = (Int16)reader.RetrieveNext();
						Int16 ti = (Int16)reader.RetrieveNext();
						reader.RetrieveNext();	// reserved
						fastate.AddEdge(m_charsets[ci], ti);
					}
					
					AddDfaState(index, fastate);
					break;
					
				case RecordId.LRTables:
					LRActionTable table = new LRActionTable();
					index = (Int16)reader.RetrieveNext();
					reader.RetrieveNext();	// reserverd
					
					while (! reader.RetrieveDone())
					{
						Int16 sid		= (Int16)reader.RetrieveNext();
						Int16 action	= (Int16)reader.RetrieveNext();
						Int16 tid		= (Int16)reader.RetrieveNext();
						reader.RetrieveNext();	// reserved
						table.AddItem(m_symbols[sid], (Action)action, tid);
					}
					
					AddLalrTable(index, table);
					break;
					
				case RecordId.Comment:
					Console.WriteLine("Comment record encountered");
					break;
					
				default:
					throw new ParserException("Wrong id for record");
				}
			}
		}
		
		private void Reset()
		{
			foreach (Symbol symbol in m_symbols)
			{
				if (symbol.Kind == SymbolType.Error)
					m_errorSymbol = symbol;
				else if (symbol.Kind == SymbolType.End)
					m_endSymbol = symbol;
			}

			m_haveReduction = false;
			m_LalrState = m_initLalrState;
			m_lineNumber = 1;
			m_commentLevel = 0;
			
			m_inputTokens.Clear();
			m_outputTokens.Clear();
			m_tempStack.Clear();
		}
		
		private void PrepareToParse()
		{
			Token token = new Token();
			token.State = m_initLalrState;
			token.SetParent(m_symbols[m_startSymbol]);
			m_tempStack.PushToken(token);
		}

		private void DiscardLine()
		{
			m_source.DiscardLine();
			m_lineNumber++;
		}

		/// Returns true if the specified token is a CommentLine or CommentStart-symbol.
		private bool CommentToken(Token p_token)
		{
			return (p_token.Kind == SymbolType.CommentLine) 
				|| (p_token.Kind == SymbolType.CommentStart);
		}
		
		/// This function analyzes a token and either:
		///   1. Makes a SINGLE reduction and pushes a complete Reduction object on the stack
		///   2. Accepts the token and shifts
		///   3. Errors and places the expected symbol indexes in the Tokens list
		/// The Token is assumed to be valid and WILL be checked
		private ParseResult ParseToken(Token p_token)
		{
			ParseResult result = ParseResult.InternalError;
			LRActionTable table = m_LalrTables[m_LalrState];
			LRAction action = table.GetActionForSymbol(p_token.TableIndex);
			
			if (action != null)
			{
				m_haveReduction = false;
				m_outputTokens.Clear();
				
				switch (action.Action)
				{
				case Action.Accept:
					m_haveReduction = true;
					result = ParseResult.Accept;
					break;
				case Action.Shift:
					p_token.State = m_LalrState = action.Value;
					m_tempStack.PushToken(p_token);
					result = ParseResult.Shift;
					break;
				case Action.Reduce:
					result = Reduce(m_rules[action.Value]);
					break;
				}
			}
			else
			{
				// syntax error - fill expected tokens.				
				m_outputTokens.Clear();				
				foreach (LRAction a in table.Members)
				{
					SymbolType kind = a.Symbol.Kind;
					
					if (kind == SymbolType.Terminal || kind == SymbolType.End)
						m_outputTokens.PushToken(new Token(a.Symbol));
				}				
				result = ParseResult.SyntaxError;
			}
			
			return result;
		}
	
		/// <summary>Produces a reduction.</summary>
		/// <remarks>Removes as many tokens as members in the rule and pushes a 
		///          non-terminal token.</remarks>
		private ParseResult Reduce(Rule p_rule)
		{
			ParseResult result;
			Token head;
			
			if (m_trimReductions && p_rule.ContainsOneNonTerminal)
			{
				// The current rule only consists of a single nonterminal and can be trimmed from the
				// parse tree. Usually we create a new Reduction, assign it to the Data property
				// of Head and push it on the stack. However, in this case, the Data property of the
				// Head will be assigned the Data property of the reduced token (i.e. the only one
				// on the stack). In this case, to save code, the value popped of the stack is changed 
				// into the head.
				head = m_tempStack.PopToken();
				head.SetParent(p_rule.RuleNonTerminal);
				
				result = ParseResult.ReduceEliminated;
			}
			else
			{
				Reduction reduction = new Reduction();
				reduction.ParentRule = p_rule;
				
				m_tempStack.PopTokensInto(reduction, p_rule.SymbolCount);

				head = new Token();
				head.Data = reduction;
				head.SetParent(p_rule.RuleNonTerminal);
				
				m_haveReduction = true;
				result = ParseResult.ReduceNormal;
			}
			
			int index = m_tempStack.PeekToken().State;
			LRAction action = m_LalrTables[index].GetActionForSymbol(p_rule.RuleNonTerminal.TableIndex);
			
			if (action != null)
			{
				head.State = m_LalrState = action.Value;;
				m_tempStack.PushToken(head);
			}
			else
				throw new ParserException("Action for LALR state is null");
				
			return result;
		}
		
		/// This method implements the DFA algorithm and returns a token
		/// to the LALR state machine.
		private Token RetrieveToken()
		{
			Token result;
			int currentPos = 0;
			int lastAcceptState = -1;
			int lastAcceptPos = -1;
			FAState currentState = m_DfaStates[m_initDfaState];
			
			try
			{
				while (true)
				{
					// This code searches all the branches of the current DFA state for the next
					// character in the input LookaheadStream. If found the target state is returned.
					// The InStr() function searches the string pCharacterSetTable.Member(CharSetIndex)
					// starting at position 1 for ch.  The pCompareMode variable determines whether
					// the search is case sensitive.
					int	target = -1;
					char ch = FixCase(m_source.LookAhead(currentPos));
					
					foreach (FAEdge edge in currentState.Edges)
					{
						String chars = edge.Characters;
						if (chars.IndexOf(ch) != -1)
						{
							target = edge.TargetIndex;
							break;
						}
					}
					
					// This block-if statement checks whether an edge was found from the current state.
					// If so, the state and current position advance. Otherwise it is time to exit the main loop
					// and report the token found (if there was it fact one). If the LastAcceptState is -1,
					// then we never found a match and the Error Token is created. Otherwise, a new token
					// is created using the Symbol in the Accept State and all the characters that
					// comprise it.
					if (target != -1)
					{
						// This code checks whether the target state accepts a token. If so, it sets the
						// appropiate variables so when the algorithm is done, it can return the proper
						// token and number of characters.
						if (m_DfaStates[target].AcceptSymbol != -1)
						{
							lastAcceptState = target;
							lastAcceptPos = currentPos;
						}
	
						currentState = m_DfaStates[target];
						currentPos++;
					}
					else
					{
						if (lastAcceptState == -1)
						{
							result = new Token(m_errorSymbol);
							result.Data = m_source.Read(1);
						}
						else
						{
							Symbol symbol = m_symbols[m_DfaStates[lastAcceptState].AcceptSymbol];
							result = new Token(symbol);
							result.Data = m_source.Read(lastAcceptPos + 1);
						}
						break;
					}
				}
			}
			catch (EndOfStreamException)
			{
				result = new Token(m_endSymbol);
				result.Data = "";
			}

			UpdateLineNumber((String)result.Data);			

			return result;
		}
		
		private void UpdateLineNumber(String p_string)
		{
			int index, pos = 0;
			while ((index = p_string.IndexOf('\n', pos)) != -1)
			{
				pos = index + 1;
				m_lineNumber++;
			}
		}

	}
}
