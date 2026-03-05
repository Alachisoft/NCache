
using System;

namespace Alachisoft.NGroups.Util
{
	
	/// <summary> The Command patttern (se Gamma et al.). Implementations would provide their
	/// own <code>execute</code> method.
	/// </summary>
	/// <author>  Bela Ban
	/// </author>
	internal interface Command
	{
		bool execute();
	}
}