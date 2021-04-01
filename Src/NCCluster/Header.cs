using System;
using System.Runtime.Serialization;
using Alachisoft.NCache.Common;

namespace Alachisoft.NGroups
{
	/// <summary>
	/// Used to append messages with stack information
	/// <p><b>Author:</b> Chris Koiak, Bela Ban</p>
	/// <p><b>Date:</b>  12/03/2003</p>
	/// </summary>
	[Serializable]
	public abstract class Header //: ICloneable
	{
		public const long HDR_OVERHEAD = 255; // estimated size of a header (used to estimate the size of the entire msg)

		/// <summary> To be implemented by subclasses. Return the size of this object for the serialized version of it.
		/// I.e. how many bytes this object takes when flattened into a buffer. This may be different for each instance,
		/// or can be the same. This may also just be an estimation. E.g. FRAG uses it on Message to determine whether
		/// or not to fragment the message. Fragmentation itself will be accurate, because the entire message will actually
		/// be serialized into a byte buffer, so we can determine the exact size.
		/// </summary>
		public virtual long size()
		{
			return HDR_OVERHEAD;
		}

		/// <summary>
		/// Default String representation of the HDR
		/// </summary>
		/// <returns>String representation of the HDR</returns>
		public override string ToString()
		{
			return '[' + GetType().FullName + " HDR]";
		}

        public virtual object Clone(ObjectProvider provider)
        {
            return null;
        }

        
    }
}
