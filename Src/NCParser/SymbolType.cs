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

namespace Alachisoft.NCache.Parser
{
    /// Respresents the type of a symbol.
    public enum SymbolType
    {		
        /// A normal non-terminal.
        NonTerminal			= 0,
		
        /// A normal terminal.
        Terminal			= 1,
		
        /// This Whitespace symbol is a special terminal that is automatically 
        /// ignored by the parsing engine. Any text accepted as whitespace is 
        /// considered to be inconsequential and "meaningless".
        Whitespace			= 2,
		
        /// The End symbol is generated when the tokenizer reaches the end of 
        /// the source text.
        End					= 3,
		
        /// This type of symbol designates the start of a block comment.
        CommentStart		= 4,
		
        /// This type of symbol designates the end of a block comment.
        CommentEnd			= 5,
		
        /// When the engine reads a token that is recognized as a line comment, 
        /// the remaining characters on the line are automatically ignored by 
        /// the parser.
        CommentLine			= 6,
		
        /// The Error symbol is a general-purpose means of representing characters 
        /// that were not recognized by the tokenizer. In other words, when the 
        /// tokenizer reads a series of characters that is not accepted by the DFA 
        /// engine, a token of this type is created.
        Error				= 7
    };
}
