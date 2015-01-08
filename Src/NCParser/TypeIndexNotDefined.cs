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
using System.Runtime.Serialization;

namespace Alachisoft.NCache.Parser
{
    [Serializable]
    public class TypeIndexNotDefined : Exception, ISerializable
    {
        public TypeIndexNotDefined(String error) : base(error)
        {
        }

        public TypeIndexNotDefined(String error, Exception exception) : base(error, exception)
        {
        }

        public TypeIndexNotDefined(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
