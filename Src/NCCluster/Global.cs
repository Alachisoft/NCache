//
// In order to convert some functionality to Visual C#, the Java Language Conversion Assistant
// creates "support classes" that duplicate the original functionality.  
//
// Support classes replicate the functionality of the original code, but in some cases they are 
// substantially different architecturally. Although every effort is made to preserve the 
// original architecture of the application in the converted project, the user should be aware that 
// the primary goal of these support classes is to replicate functionality, and that at times 
// the architecture of the resulting solution may differ somewhat.
//

using System.Collections;
using System.Collections.Specialized;

using Alachisoft.NGroups;
using Alachisoft.NGroups.Util;
using Alachisoft.NGroups.Blocks;
using Alachisoft.NGroups.Protocols;
using Alachisoft.NGroups.Protocols.pbcast;
using Alachisoft.NCache.Serialization;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures;


/// <summary>
/// Contains conversion support elements such as classes, interfaces and static methods.
/// </summary>
public class Global
{
	static public void RegisterCompactTypes()
	{
		CompactFormatterServices.RegisterCompactType(typeof(List),81);
        CompactFormatterServices.RegisterCompactType(typeof(ProductVersion), 302);
#if SERVER
		CompactFormatterServices.RegisterCompactType(typeof(ViewId),82);
		CompactFormatterServices.RegisterCompactType(typeof(View),83);
		CompactFormatterServices.RegisterCompactType(typeof(PingRsp),85);
		CompactFormatterServices.RegisterCompactType(typeof(Digest),87);
		CompactFormatterServices.RegisterCompactType(typeof(Message),89);
		CompactFormatterServices.RegisterCompactType(typeof(MergeView),90);
		CompactFormatterServices.RegisterCompactType(typeof(MergeData),91);
        CompactFormatterServices.RegisterCompactType(typeof(JoinRsp), 92);
		CompactFormatterServices.RegisterCompactType(typeof(RequestCorrelator.HDR),93);
		CompactFormatterServices.RegisterCompactType(typeof(TOTAL.HDR),94);
		CompactFormatterServices.RegisterCompactType(typeof(GMS.HDR),98);
		CompactFormatterServices.RegisterCompactType(typeof(PingHeader),103);
		CompactFormatterServices.RegisterCompactType(typeof(TcpHeader),104);
        CompactFormatterServices.RegisterCompactType(typeof(ConnectionTable.Connection.ConnectionHeader), 108);
        CompactFormatterServices.RegisterCompactType(typeof(HashMapBucket), 114);
        CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.Net.Address), 110);
        CompactFormatterServices.RegisterCompactType(typeof(TCP.HearBeat), 115);
        CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.Stats.HPTimeStats), 126);
        CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.Stats.HPTime), 127);
        CompactFormatterServices.RegisterCompactType(typeof(MessageTrace), 128);
        CompactFormatterServices.RegisterCompactType(typeof(ConnectInfo), 137);
#endif
	}

	/*******************************/
	//Provides access to a static System.Random class instance
	static public System.Random Random = new System.Random();

	/*******************************/
	/// <summary>
	/// This class provides functionality not found in .NET collection-related interfaces.
	/// </summary>
	internal class ICollectionSupport
	{
		/// <summary>
		/// Removes all the elements from the specified collection that are contained in the target collection.
		/// </summary>
		/// <param name="target">Collection where the elements will be removed.</param>
		/// <param name="c">Elements to remove from the target collection.</param>
		/// <returns>true</returns>
		public static bool RemoveAll(ArrayList target, ArrayList c)
		{
			try
			{
				for (int i=0; i<c.Count; i++)
				{
					target.Remove(c[i]);
				}
			}
			catch (System.Exception ex)
			{
				throw ex;
			}
			return true;
		}

		/// <summary>
		/// Retains the elements in the target collection that are contained in the specified collection
		/// </summary>
		/// <param name="target">Collection where the elements will be removed.</param>
		/// <param name="c">Elements to be retained in the target collection.</param>
		/// <returns>true</returns>
		public static bool RetainAll(ArrayList target, ArrayList c)
		{
			try
			{
				for (int i = target.Count - 1; i>=0; i--)
				{
					if(!c.Contains(target[i]))
						target.RemoveAt(i);
				}
			}
			catch (System.Exception ex)
			{
				throw ex;
			}
			return true;
		}
	}

	/*******************************/

	
	/// <summary>
	/// Converts an array of bytes to an array of chars
	/// </summary>
	/// <param name="byteArray">The array of bytes to convert</param>
	/// <returns>The new array of chars</returns>
	public static char[] ToCharArray(byte[] byteArray) 
	{
		return System.Text.UTF8Encoding.UTF8.GetChars(byteArray);
	}


	/*******************************/
	/// <summary>
	/// Converts the specified collection to its string representation.
	/// </summary>
	/// <param name="c">The collection to convert to string.</param>
	/// <returns>A string representation of the specified collection.</returns>
	public static string CollectionToString(ICollection c)
	{
		System.Text.StringBuilder s = new System.Text.StringBuilder();
		
		if (c != null)
		{
		
			ArrayList l = new ArrayList(c);

			bool isDictionary = (c is BitArray || c is Hashtable || c is IDictionary || c is NameValueCollection || (l.Count > 0 && l[0] is DictionaryEntry));
			for (int index = 0; index < l.Count; index++) 
			{
				if (l[index] == null)
					s.Append("null");
				else if (!isDictionary)
					s.Append(l[index]);
				else
				{
					isDictionary = true;
					if (c is NameValueCollection)
						s.Append(((NameValueCollection)c).GetKey (index));
					else
						s.Append(((DictionaryEntry) l[index]).Key);
					s.Append("=");
					if (c is NameValueCollection)
						s.Append(((NameValueCollection)c).GetValues(index)[0]);
					else
						s.Append(((DictionaryEntry) l[index]).Value);

				}
				if (index < l.Count - 1)
					s.Append(", ");
			}
			
			if(isDictionary)
			{
				if(c is ArrayList)
					isDictionary = false;
			}
			if (isDictionary)
			{
				s.Insert(0, "{");
				s.Append("}");
			}
			else 
			{
				s.Insert(0, "[");
				s.Append("]");
			}
		}
		else
			s.Insert(0, "null");
		return s.ToString();
	}

	/// <summary>
	/// Tests if the specified object is a collection and converts it to its string representation.
	/// </summary>
	/// <param name="obj">The object to convert to string</param>
	/// <returns>A string representation of the specified object.</returns>
	public static string CollectionToString(object obj)
	{
		string result = "";

		if (obj != null)
		{
			if (obj is ICollection)
				result = CollectionToString((ICollection)obj);
			else
				result = obj.ToString();
		}
		else
			result = "null";

		return result;
	}
    public static string ArrayListToString(ArrayList list)
    {
        System.Text.StringBuilder s = new System.Text.StringBuilder();

        if (list != null)
        {
            s.Append("[ ");

            foreach (object item in list)
            {
                s.Append(item.ToString() + ",");
            }
            s.Remove(s.Length, 1);
            s.Append(" ]");
        }
        else
        {
            s.Append("<null>");
        }
        return s.ToString();
    }

}
