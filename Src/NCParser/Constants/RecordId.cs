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

// C# Translation of GoldParser, by Marcus Klimstra <klimstra@home.nl>.
// Based on GOLDParser by Devin Cook <http://www.devincook.com/goldparser>.
namespace Alachisoft.NCache.Parser
{
    /// Represents the type of a record in the CGT file.
	internal enum RecordId
    {
        Parameters = 80,

        TableCounts = 84,

        Initial = 73,

        Symbols = 83,

        CharSets = 67,

        Rules = 82,

        DFAStates = 68,

        LRTables = 76,

        Comment = 33
    };
}
