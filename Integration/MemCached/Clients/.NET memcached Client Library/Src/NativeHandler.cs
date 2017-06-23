/**
/// Memcached C# client
/// Copyright (c) 2005
/// 
/// Based on code written originally by Greg Whalin
/// http://www.whalin.com/memcached/
///
/// This library is free software; you can redistribute it and/or
/// modify it under the terms of the GNU Lesser General Public
/// License as published by the Free Software Foundation; either
/// version 2.1 of the License, or (at your option) any later
/// version.
///
/// This library is distributed in the hope that it will be
/// useful, but WITHOUT ANY WARRANTY; without even the implied
/// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
/// PURPOSE.  See the GNU Lesser General Public License for more
/// details.
///
/// You should have received a copy of the GNU Lesser General Public
/// License along with this library; if not, write to the Free Software
/// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307  USA
///
/// @author Tim Gebhardt <tim@gebhardtcomputing.com>
/// @version 1.0
**/
namespace Memcached.ClientLibrary
{
	using System;
	using System.Resources;
	using System.Text;
	/**
	/// COMMENT FROM ORIGINAL JAVA CLIENT LIBRARY.  NOT SURE HOW MUCH IT 
	/// APPLIES TO THIS LIBRARY.
	///
	///
	/// Handle encoding standard Java types directly which can result in significant
	/// memory savings:
	/// 
	/// Currently the Memcached driver for Java supports the setSerialize() option.
	/// This can increase performance in some situations but has a few issues:
	/// 
	/// Code that performs class casting will throw ClassCastExceptions when
	/// setSerialize is enabled. For example:
	/// 
	///     mc.set("foo", new Integer(1)); Integer output = (Integer)mc.get("foo");
	/// 
	/// Will work just file when setSerialize is true but when its false will just throw
	/// a ClassCastException.
	/// 
	/// Also internally it doesn't support bool and since toString is called wastes a
	/// lot of memory and causes additional performance issue.  For example an Integer
	/// can take anywhere from 1 byte to 10 bytes.
	/// 
	/// Due to the way the memcached slabytes allocator works it seems like a LOT of wasted
	/// memory to store primitive types as serialized objects (from a performance and
	/// memory perspective).  In our applications we have millions of small objects and
	/// wasted memory would become a big problem.
	/// 
	/// For example a Serialized bool takes 47 bytes which means it will fit into the
	/// 64byte LRU.  Using 1 byte means it will fit into the 8 byte LRU thus saving 8x
	/// the memory.  This also saves the CPU performance since we don't have to
	/// serialize bytes back and forth and we can compute the byte[] value directly.
	/// 
	/// One problem would be when the user calls get() because doing so would require
	/// the app to know the type of the object stored as a bytearray inside memcached
	/// (since the user will probably cast).
	/// 
	/// If we assume the basic types are interned we could use the first byte as the
	/// type with the remaining bytes as the value.  Then on get() we could read the
	/// first byte to determine the type and then construct the correct object for it.
	/// This would prevent the ClassCastException I talked about above.
	/// 
	/// We could remove the setSerialize() option and just assume that standard VM types
	/// are always internd in this manner.
	/// 
	/// mc.set("foo", new bool.TRUE); bool bytes = (bool)mc.get("foo");
	/// 
	/// And the type casts would work because internally we would create a new bool
	/// to return back to the client.
	/// 
	/// This would reduce memory footprint and allow for a virtual implementation of the
	/// Externalizable interface which is much faster than Serialzation.
	/// 
	/// Currently the memory improvements would be:
	/// 
	/// java.lang.bool - 8x performance improvement (now just two bytes)
	/// java.lang.Integer - 16x performance improvement (now just 5 bytes)
	/// 
	/// Most of the other primitive types would benefit from this optimization.
	/// java.lang.Character being another obvious example.
	/// 
	/// I know it seems like I'm being really picky here but for our application I'd
	/// save 1G of memory right off the bat.  We'd go down from 1.152G of memory used
	/// down to 144M of memory used which is much better IMO.
	**/
	public sealed class NativeHandler 
	{
		//FIXME: what about other common types?  Also what about
		//Collections of native types?  I could reconstruct these on the remote end
		//if necessary.  Though I'm not sure of the performance advantage here.
    
		public const byte ByteMarker = 1;
		public const byte BoolMarker = 2;
		public const byte Int32Marker = 3;
		public const byte Int64Marker = 4;
		public const byte CharMarker = 5;
		public const byte StringMarker = 6;
		public const byte StringBuilderMarker = 7;
		public const byte SingleMarker = 8;
		public const byte Int16Marker = 9;
		public const byte DoubleMarker = 10;
		public const byte DateTimeMarker = 11;

		private NativeHandler() {}

		public static bool IsHandled(object value) 
		{
			if(value is bool ||
				value is byte ||
				value is string ||
				value is char ||
				value is StringBuilder ||
				value is short ||
				value is long ||
				value is double ||
				value is float ||
				value is DateTime ||
				value is Int32) 
			{
				return true;
			}

			return false;
		}

		// **** Encode methods ******************************************************

		public static byte[] Encode(object value)
		{
			if(value == null)
				return new byte[0];

			if(value is bool)
				return Encode((bool)value);

			if(value is Int32) 
				return Encode((Int32)value);

			if(value is char)
				return Encode((char)value);

			if(value is byte)
				return Encode((byte)value);

			if(value is short)
				return Encode((short)value);

			if(value is long) 
				return Encode((long)value);

			if(value is double) 
				return Encode((double)value);

			if(value is float) 
				return Encode((float)value);

			string tempstr = value as string;
			if(tempstr != null)
				return Encode(tempstr);

			StringBuilder tempsb = value as StringBuilder;
			if(tempsb != null)
				return Encode(tempsb);

			if(value is DateTime) 
				return Encode((DateTime) value);

			return null;
		}

		public static byte[] Encode(DateTime value) 
		{
			byte[] bytes = GetBytes(value.Ticks);
			bytes[0] = DateTimeMarker;
			return bytes;
		}

		public static byte[] Encode(bool value) 
		{
			byte[] bytes = new byte[2];

			bytes[0] = BoolMarker;
        
			if(value) 
			{
				bytes[1] = 1;
			} 
			else 
			{
				bytes[1] = 0;
			}

			return bytes;
		}

		public static byte[] Encode(int value) 
		{
			byte[] bytes = GetBytes(value);
			bytes[0] = Int32Marker;

			return bytes;
		}

		public static byte[] Encode(char value) 
		{
			byte[] result = Encode((short) value);

			result[0] = CharMarker;
        
			return result;
		}
    
		public static byte[] Encode(string value) 
		{
			if(value == null)
				return new byte[1]{ StringMarker }; 

			byte[] asBytes = UTF8Encoding.UTF8.GetBytes(value);
		
			byte[] result = new byte[asBytes.Length + 1];
		
			result[0] = StringMarker;
		
			Array.Copy(asBytes, 0, result, 1, asBytes.Length);

			return result;   
		}

		public static byte[] Encode(byte value) 
		{
			byte[] bytes = new byte[2];

			bytes[0] = ByteMarker;

			bytes[1] = value;
        
			return bytes;   
		}

		public static byte[] Encode(StringBuilder value)
		{
			if(value == null)
				return new byte[1]{ StringBuilderMarker };

			byte[] bytes = Encode(value.ToString());
			bytes[0] = StringBuilderMarker;
        
			return bytes;   
		}

		public static byte[] Encode(short value)
		{
			byte[] bytes = Encode((int)value);
			bytes[0] = Int16Marker;
        
			return bytes;   
		}

		public static byte[] Encode(long value) 
		{
			byte[] bytes = GetBytes(value);
			bytes[0] = Int64Marker;

			return bytes;   
		}

		public static byte[] Encode(double value) 
		{
			byte[] temp = BitConverter.GetBytes(value);
			byte[] bytes = new byte[temp.Length + 1];
			bytes[0] = DoubleMarker;
			Array.Copy(temp, 0, bytes, 1, temp.Length);
        
			return bytes;   
		}

		public static byte[] Encode(float value) 
		{
			byte[] temp = BitConverter.GetBytes(value);
			byte[] bytes = new byte[temp.Length + 1];
			bytes[0] = SingleMarker;
			Array.Copy(temp, 0, bytes, 1, temp.Length);
		
			return bytes;   
		}

		public static byte[] GetBytes(long value) 
		{
			byte b0 = (byte)((value >> 56) & 0xFF);
			byte b1 = (byte)((value >> 48) & 0xFF);
			byte b2 = (byte)((value >> 40) & 0xFF);
			byte b3 = (byte)((value >> 32) & 0xFF);
			byte b4 = (byte)((value >> 24) & 0xFF);
			byte b5 = (byte)((value >> 16) & 0xFF);
			byte b6 = (byte)((value >> 8) & 0xFF);
			byte b7 = (byte)((value >> 0) & 0xFF);

			byte[] bytes = new byte[9];
			bytes[1] = b0;
			bytes[2] = b1;
			bytes[3] = b2;
			bytes[4] = b3;
			bytes[5] = b4;
			bytes[6] = b5;
			bytes[7] = b6;
			bytes[8] = b7;

			return bytes;   
		}

		public static byte[] GetBytes(int value) 
		{
			byte b0 = (byte)((value >> 24) & 0xFF);
			byte b1 = (byte)((value >> 16) & 0xFF);
			byte b2 = (byte)((value >> 8) & 0xFF);
			byte b3 = (byte)((value >> 0) & 0xFF);

			byte[] bytes = new byte[5];
			bytes[1] = b0;
			bytes[2] = b1;
			bytes[3] = b2;
			bytes[4] = b3;

			return bytes;   
		}

		// **** Decode methods ******************************************************

		public static Object Decode(byte[] bytes) 
		{
			//something strange is going on.
			if(bytes == null || bytes.Length == 0)
				return null;

			//determine what type this is:
		
			if(bytes[0] == BoolMarker)
				return DecodeBool(bytes);

			if(bytes[0] == Int32Marker)
				return DecodeInteger(bytes);

			if(bytes[0] == StringMarker)
				return DecodeString(bytes);

			if(bytes[0] == CharMarker)
				return DecodeCharacter(bytes);

			if(bytes[0] == ByteMarker)
				return DecodeByte(bytes);

			if(bytes[0] == StringBuilderMarker)
				return DecodeStringBuilder(bytes);

			if(bytes[0] == Int16Marker)
				return DecodeShort(bytes);

			if(bytes[0] == Int64Marker)
				return DecodeLong(bytes);

			if(bytes[0] == DoubleMarker)
				return DecodeDouble(bytes);

			if(bytes[0] == SingleMarker)
				return DecodeFloat(bytes);

			if(bytes[0] == DateTimeMarker)
				return DecodeDate(bytes);

			return null;
		}

		public static DateTime DecodeDate(byte[] bytes) 
		{
			return new DateTime(ToLong(bytes));
		}

		public static bool DecodeBool(byte[] bytes) 
		{
			if(bytes == null)
				throw new ArgumentNullException("bytes", GetLocalizedString("parameter cannot be null"));

			bool value = bytes[1] == 1;

			return value;        
		}

		public static Int32 DecodeInteger(byte[] bytes) 
		{
			return ToInt(bytes) ;
		}

		public static string DecodeString(byte[] bytes) 
		{
			if(bytes == null)
				return null;

			return UTF8Encoding.UTF8.GetString(bytes, 1, bytes.Length -1);
		}

		public static char DecodeCharacter(byte[] bytes) 
		{
			return (char)DecodeInteger(bytes);   
		}

		public static byte DecodeByte(byte[] bytes) 
		{
			if(bytes == null)
				throw new ArgumentNullException("bytes", GetLocalizedString("parameter cannot be null"));

			byte value = bytes[1];

			return value;   
		}

		public static StringBuilder DecodeStringBuilder(byte[] bytes) 
		{
			return new StringBuilder(DecodeString(bytes));   
		}

		public static short DecodeShort(byte[] bytes) 
		{
			return (short)DecodeInteger(bytes);   
		}

		public static long DecodeLong(byte[] bytes) 
		{
			return ToLong(bytes);
		}

		public static double DecodeDouble(byte[] bytes)  
		{
			return BitConverter.ToDouble(bytes, 1);   
		}

		public static float DecodeFloat(byte[] bytes) 
		{    
			return BitConverter.ToSingle(bytes, 1);        
		}

		public static int ToInt(byte[] bytes) 
		{
			if(bytes == null)
				throw new ArgumentNullException("bytes", GetLocalizedString("parameter cannot be null"));

			//This works by taking each of the bit patterns and converting them to
			//ints taking into account 2s complement and then adding them..
        
			return	((((int) bytes[4]) & 0xFF) << 32) +
				((((int) bytes[3]) & 0xFF) << 40) +
				((((int) bytes[2]) & 0xFF) << 48) +
				((((int) bytes[1]) & 0xFF) << 56) ;
		}    

		public static long ToLong(byte[] bytes) 
		{
			if(bytes == null)
				throw new ArgumentNullException("bytes", GetLocalizedString("parameter cannot be null"));

			//FIXME: this is sad in that it takes up 16 bytes instead of JUST 8
			//bytes and wastes memory.  We could use a memcached flag to enable
			//special treatment for 64bit types
        
			//This works by taking each of the bit patterns and converting them to
			//ints taking into account 2s complement and then adding them..

			return	(((long) bytes[8]) & 0xFF) +
				((((long) bytes[7]) & 0xFF) << 8) +
				((((long) bytes[6]) & 0xFF) << 16) +
				((((long) bytes[5]) & 0xFF) << 24) +
				((((long) bytes[4]) & 0xFF) << 32) +
				((((long) bytes[3]) & 0xFF) << 40) +
				((((long) bytes[2]) & 0xFF) << 48) +
				((((long) bytes[1]) & 0xFF) << 56) ;
		}
    
		private static ResourceManager _resourceManager = new ResourceManager("Memcached.ClientLibrary.StringMessages", typeof(SockIOPool).Assembly);
		private static string GetLocalizedString(string key)
		{
			return _resourceManager.GetString(key);
		}
	}
}
