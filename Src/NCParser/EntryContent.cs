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
    /// Represents the type of an entry in the CGT file.
    internal enum EntryContent
    {		
        ///
        Empty				= 69,
		
        ///
        Integer				= 73,
		
        ///
        String				= 83,
		
        ///
        Boolean				= 66,
		
        ///
        Byte				= 98,
		
        ///
        Multi				= 77
    };
}
