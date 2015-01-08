// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

// In order to convert some functionality to Visual C#, the Java Language Conversion Assistant
// creates "support classes" that duplicate the original functionality.  
//
// Support classes replicate the functionality of the original code, but in some cases they are 
// substantially different architecturally. Although every effort is made to preserve the 
// original architecture of the application in the converted project, the user should be aware that 
// the primary goal of these support classes is to replicate functionality, and that at times 
// the architecture of the resulting solution may differ somewhat.
//

using System;
using System.IO;
using System.Collections;
using System.Collections.Specialized;

using Alachisoft.NGroups;
using Alachisoft.NGroups.Util;

using Alachisoft.NGroups.Blocks;
using Alachisoft.NGroups.Protocols;
using Alachisoft.NGroups.Protocols.pbcast;




using Alachisoft.NCache.Runtime.Serialization;


using Alachisoft.NCache.Serialization;
using Alachisoft.NCache.Serialization.Formatters;
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
        CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.ProductVersion), 302);

       

		CompactFormatterServices.RegisterCompactType(typeof(ViewId),82);
		CompactFormatterServices.RegisterCompactType(typeof(View),83);
		CompactFormatterServices.RegisterCompactType(typeof(PingRsp),85);
		CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NGroups.Protocols.pbcast.Digest),87);
		CompactFormatterServices.RegisterCompactType(typeof(Message),89);
		CompactFormatterServices.RegisterCompactType(typeof(MergeView),90);
		CompactFormatterServices.RegisterCompactType(typeof(MergeData),91);
        CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NGroups.Protocols.pbcast.JoinRsp), 92);
		CompactFormatterServices.RegisterCompactType(typeof(RequestCorrelator.HDR),93);
		CompactFormatterServices.RegisterCompactType(typeof(TOTAL.HDR),94);
		CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NGroups.Protocols.pbcast.GMS.HDR),98);
		CompactFormatterServices.RegisterCompactType(typeof(PingHeader),103);
		CompactFormatterServices.RegisterCompactType(typeof(TcpHeader),104);
        CompactFormatterServices.RegisterCompactType(typeof(ConnectionTable.Connection.ConnectionHeader), 108);

        CompactFormatterServices.RegisterCompactType(typeof(HashMapBucket), 114);
        CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.Net.Address), 110);

        CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NGroups.Protocols.TCP.HearBeat), 115);

        CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.Stats.HPTimeStats), 126);
        CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.Stats.HPTime), 127);
        CompactFormatterServices.RegisterCompactType(typeof(MessageTrace), 128);
        CompactFormatterServices.RegisterCompactType(typeof(ConnectInfo), 137);

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
	/// The class performs token processing in strings
	/// </summary>
    /// <summary>
    /// This class breaks a string into set of tokens and returns them one by one
    /// </summary>
    /// Hasan Khan: Originally this class was written by someone else which highly
    /// relied upon use of exceptions for its functionality and since it is used
    /// in many places in the code it could affect the performance of NCache. 
    /// I have been asked to fix this performance bottleneck so I will rewrite this class.
    /// 
    /// Design of this class is totally useless but I'm going to follow the old design
    /// for the sake of compatibility of rest of the code.
    /// 
    /// Design flaws:
    /// -------------
    /// 1) HasMoreTokens() works same as MoveNext
    /// 2) MoveNext() internally calls HasMoreTokens
    /// 3) Current calls NextToken
    /// 4) NextToken() gives the current token
    /// 5) Count gives the number of remaining tokens
    //internal class Tokenizer : IEnumerator
    public class Tokenizer : IEnumerator
    {
        string text;
        char[] delims;
        string[] tokens;
        int index;

        public Tokenizer(string text, string delimiters)
        {
            this.text = text;
            delims = delimiters.ToCharArray();

			/// We do not need this function in 1x so contional compiling it
			/// reason: StringSplitOptions.RemoveEmptyEntries is not defined in system assembly of .net 1x

			tokens = text.Split(delims, StringSplitOptions.RemoveEmptyEntries);

            index = -1; // First call of MoveNext will put the pointer on right position.
        }

        public string NextToken()
        {
            return tokens[index]; //Hasan: this is absurd
        }

        /// <summary>
        /// Remaining tokens count
        /// </summary>
        public int Count //Hasan: bad design
        {
            get
            {
                if (index < tokens.Length)
                    return tokens.Length - index - 1;
                else
                    return 0;
            }
        }

        /// <summary>
        /// Determines if there are more tokens to return from text.
        /// Also moves the pointer to next token
        /// </summary>
        /// <returns>True if there are more tokens otherwise, false</returns>
        public bool HasMoreTokens() //Hasan: bad design
        {
            if (index < tokens.Length - 1)
            {
                index++;
                return true;
            }
            else
                return false;
        }
        #region IEnumerator Members

        /// <summary>
        /// Performs the same action as NextToken
        /// </summary>
        public object Current
        {
            get { return NextToken(); }
        }

        /// <summary>
        /// Performs the same function as HasMoreTokens
        /// </summary>
        /// <returns>True if there are more tokens otherwise, false</returns>
        public bool MoveNext()
        {
            return HasMoreTokens(); //Hasan: this is absurd
        }

        public void Reset()
        {
            index = -1;
        }

        #endregion
    }
	
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
