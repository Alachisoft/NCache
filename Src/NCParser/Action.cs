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
    /// constants associated with what action should be performed 
    internal enum Action
    {		
        ///
        Shift				= 1,
		
        ///
        Reduce				= 2,
		
        ///
        Goto				= 3,
		
        ///
        Accept				= 4,
		
        ///
        Error				= 5
    };
}
