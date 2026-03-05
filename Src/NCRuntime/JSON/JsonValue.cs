//  Copyright (c) 2026 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License

using Alachisoft.NCache.Runtime.Enum;
using System;
using System.Numerics;

namespace Alachisoft.NCache.Runtime.JSON
{
	/// <summary>
	/// Maps values other than JObject and JArray in JSON standards to primitive value types
	/// </summary>
	[Serializable]
    [Obsolete("This API is deprecated and will be removed in a future release. This feature is being retired and will not be continued in future versions.", false)]
    public sealed class JsonValue : JsonValueBase
	{
		#region --------------------------- Constructors ---------------------------

		internal JsonValue(object value, JsonDataType jsonType) : base(value, jsonType)
		{
		}

		#endregion

		#region ----------------------------- Behavior -----------------------------

		/// <summary>
		/// Attempts to convert JSON value to boolean
		/// </summary>
		/// <returns>converted boolean value</returns>
		/// <exception cref="FormatException"/>
		/// <exception cref="InvalidCastException"/>
		public bool ToBoolean()
		{
			return Convert.ToBoolean(Value);
		}

		/// <summary>
		/// Attempts to convert JSON value to byte
		/// </summary>
		/// <returns>converted byte value</returns>
		/// <exception cref="FormatException"/>
		/// <exception cref="InvalidCastException"/>
		/// <exception cref="OverflowException"/>
		public byte ToByte()
		{
			return Convert.ToByte(Value);
		}

		/// <summary>
		/// Attempts to convert JSON value to sbyte
		/// </summary>
		/// <returns>converted sbyte value</returns>
		/// <exception cref="FormatException"/>
		/// <exception cref="InvalidCastException"/>
		/// <exception cref="OverflowException"/>
		public sbyte ToSByte()
		{
			return Convert.ToSByte(Value);
		}

		/// <summary>
		/// Attempts to convert JSON value to short
		/// </summary>
		/// <returns>converted short value</returns>
		/// <exception cref="FormatException"/>
		/// <exception cref="InvalidCastException"/>
		/// <exception cref="OverflowException"/>
		public short ToInt16()
		{
			return Convert.ToInt16(Value);
		}

		/// <summary>
		/// Attempts to convert JSON value to unsigned int 16
		/// </summary>
		/// <returns>converted ushort value</returns>
		/// <exception cref="FormatException"/>
		/// <exception cref="InvalidCastException"/>
		/// <exception cref="OverflowException"/>
		public ushort ToUInt16()
		{
			return Convert.ToUInt16(Value);
		}

		/// <summary>
		/// Attempts to convert JSON value to int
		/// </summary>
		/// <returns>converted int value</returns>
		/// <exception cref="FormatException"/>
		/// <exception cref="InvalidCastException"/>
		/// <exception cref="OverflowException"/>
		public int ToInt32()
		{
			return Convert.ToInt32(Value);
		}

		/// <summary>
		/// Attempts to convert JSON value to unsigned int 16
		/// </summary>
		/// <returns>converted ushort value</returns>
		/// <exception cref="FormatException"/>
		/// <exception cref="InvalidCastException"/>
		/// <exception cref="OverflowException"/>
		public uint ToUInt32()
		{
			return Convert.ToUInt32(Value);
		}

		/// <summary>
		/// Attempts to convert JSON value to int 64
		/// </summary>
		/// <returns>converted long value</returns>
		/// <exception cref="FormatException"/>
		/// <exception cref="InvalidCastException"/>
		/// <exception cref="OverflowException"/>
		public long ToInt64()
		{
			return Convert.ToInt64(Value);
		}

		/// <summary>
		/// Attempts to convert JSON value to unsigned int 64
		/// </summary>
		/// <returns>converted ulong value</returns>
		/// <exception cref="FormatException"/>
		/// <exception cref="InvalidCastException"/>
		/// <exception cref="OverflowException"/>
		public ulong ToUInt64()
		{
			return Convert.ToUInt64(Value);
		}

		/// <summary>
		/// Attempts to convert JSON value to float
		/// </summary>
		/// <returns>converted float value</returns>
		/// <exception cref="FormatException"/>
		/// <exception cref="InvalidCastException"/>
		/// <exception cref="OverflowException"/>
		public float ToFloat()
		{
			return Convert.ToSingle(Value);
		}

		/// <summary>
		/// Attempts to convert JSON value to double
		/// </summary>
		/// <returns>converted double value</returns>
		/// <exception cref="FormatException"/>
		/// <exception cref="InvalidCastException"/>
		/// <exception cref="OverflowException"/>
		public double ToDouble()
		{
			return Convert.ToDouble(Value);
		}

		/// <summary>
		/// Attempts to convert JSON value to decimal
		/// </summary>
		/// <returns>converted decimal value</returns>
		/// <exception cref="FormatException"/>
		/// <exception cref="InvalidCastException"/>
		/// <exception cref="OverflowException"/>
		public decimal ToDecimal()
		{
			return Convert.ToDecimal(Value);
		}

		/// <summary>
		/// Attempts to convert JSON value to DateTime with standard format and culture
		/// </summary>
		/// <returns>converted DateTime value</returns>
		/// <exception cref="FormatException"/>
		/// <exception cref="ArgumentNullException"/>
		public DateTime ToDateTime()
		{
			return DateTime.ParseExact(
				Value.ToString(), JsonConstants.SerializedDateTimeFormat, JsonConstants.SerializedDateTimeCulture
			);
		}

		/// <summary>
		/// Attempts to convert JSON value to DateTime with provided format
		/// </summary>
		/// <param name="format">DateTime format for conversion</param>
		/// <param name="provider">Format control for DateTime</param>
		/// <returns>converted DateTime value</returns>
		/// <exception cref="FormatException"/>
		/// <exception cref="ArgumentNullException"/>
		public DateTime ToDateTime(string format, IFormatProvider provider)
		{
			return DateTime.ParseExact(Value.ToString(), format, provider);
		}

		/// <summary>
		/// Attempts to convert JSON value to string
		/// </summary>
		/// <returns>converted string value</returns>
		/// <exception cref="FormatException"/>
		/// <exception cref="InvalidCastException"/>
		public string ToStringValue()
		{
			return Convert.ToString(Value);
		}

		#endregion

		#region ---------------------------- Overrides -----------------------------

		/// <summary>
		/// Checks if obj is equal to JSON value object
		/// </summary>
		/// <param name="obj">object to be compared</param>
		/// <returns>true if obj is equal to this object</returns>
		public override bool Equals(object obj)
		{
			var otherJsonValue = obj as JsonValue;

			if (otherJsonValue == default(JsonValue))
				return false;

			if (DataType != otherJsonValue.DataType)
				return false;

			switch (otherJsonValue.DataType)
			{
				case JsonDataType.Boolean:
					return ToBoolean().Equals(otherJsonValue.ToBoolean());

				case JsonDataType.Number:
					return CompareNumber(otherJsonValue);

				case JsonDataType.String:
					return ToStringValue().Equals(otherJsonValue.ToStringValue());

				default:
					return false;
			}
		}

		/// <summary>
		/// Represents in string format
		/// </summary>
		/// <returns>string representation of the object</returns>
		public override string ToString()
		{
			switch (DataType)
			{
				case JsonDataType.Boolean:
					return ((bool)Value) ? "true" : "false";

				case JsonDataType.String:
					return $"\"{Value}\"";

				default:
					return $"{Value}";
			}
		}

		/// <summary>
		/// Gets hashcode of the value 
		/// </summary>
		/// <returns>hashcode of value</returns>
		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}

		#endregion

		#region ------------------------- Helper Methods ---------------------------

		private bool CompareNumber(JsonValue otherJsonValue)
		{
			switch (Type.GetTypeCode(Value.GetType()))
			{
				case TypeCode.Byte:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
					return ToUInt64().Equals(otherJsonValue.ToUInt64());

				case TypeCode.SByte:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
					return ToInt64().Equals(otherJsonValue.ToInt64());

				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Decimal:
					return ToDouble().Equals(otherJsonValue.ToDouble());

				default:
					return false;
			}
		}

		#endregion

		#region ---------------------------- Operators -----------------------------

		/// <summary>
		/// Implicit operator overload for boolean JSONValue
		/// </summary>
		/// <param name="value">boolean value</param>
		public static implicit operator JsonValue(bool value)
		{
			return new JsonValue(value, JsonDataType.Boolean);
		}



		/// <summary>
		/// Implicit operator overload for byte JSONValue
		/// </summary>
		/// <param name="value">byte value</param>
		public static implicit operator JsonValue(byte value)
		{
			return new JsonValue(value, JsonDataType.Number);
		}

		/// <summary>
		/// Implicit operator overload for sbyte JSONValue
		/// </summary>
		/// <param name="value">sbyte value</param>
		public static implicit operator JsonValue(sbyte value)
		{
			return new JsonValue(value, JsonDataType.Number);
		}

		/// <summary>
		/// Implicit operator overload for short JSONValue
		/// </summary>
		/// <param name="value">short value</param>
		public static implicit operator JsonValue(short value)
		{
			return new JsonValue(value, JsonDataType.Number);
		}

		/// <summary>
		/// Implicit operator overload for ushort JSONValue
		/// </summary>
		/// <param name="value">ushort value</param>
		public static implicit operator JsonValue(ushort value)
		{
			return new JsonValue(value, JsonDataType.Number);
		}

		/// <summary>
		/// Implicit operator overload for int JSONValue
		/// </summary>
		/// <param name="value">int value</param>
		public static implicit operator JsonValue(int value)
		{
			return new JsonValue(value, JsonDataType.Number);
		}

		/// <summary>
		/// Implicit operator overload for uint JSONValue
		/// </summary>
		/// <param name="value">uint value</param>
		public static implicit operator JsonValue(uint value)
		{
			return new JsonValue(value, JsonDataType.Number);
		}

		/// <summary>
		/// Implicit operator overload for long JSONValue
		/// </summary>
		/// <param name="value">long value</param>
		public static implicit operator JsonValue(long value)
		{
			return new JsonValue(value, JsonDataType.Number);
		}

		/// <summary>
		/// Implicit operator overload for ulong JSONValue
		/// </summary>
		/// <param name="value">ulong value</param>
		public static implicit operator JsonValue(ulong value)
		{
			return new JsonValue(value, JsonDataType.Number);
		}

		/// <summary>
		/// Implicit operator overload for float JSONValue
		/// </summary>
		/// <param name="value">float value</param>
		public static implicit operator JsonValue(float value)
		{
			return new JsonValue(value, JsonDataType.Number);
		}

		/// <summary>
		/// Implicit operator overload for double JSONValue
		/// </summary>
		/// <param name="value">double value</param>
		public static implicit operator JsonValue(double value)
		{
			return new JsonValue(value, JsonDataType.Number);
		}

		/// <summary>
		/// Implicit operator overload for decimal JSONValue
		/// </summary>
		/// <param name="value">decimal value</param>
		public static implicit operator JsonValue(decimal value)
		{
			return new JsonValue(value, JsonDataType.Number);
		}

		/// <summary>
		/// Implicit operator overload for DateTime JSONValue with standard format and culture
		/// </summary>
		/// <param name="value">DateTime value</param>
		public static implicit operator JsonValue(DateTime value)
		{
			return new JsonValue(
				value.ToString(
					JsonConstants.SerializedDateTimeFormat,
					JsonConstants.SerializedDateTimeCulture
				),
				JsonDataType.String
			);
		}


		public static implicit operator JsonValue(BigInteger value)
		{
			return new JsonValue(value, JsonDataType.String);

		}
		/// <summary>
		/// Implicit operator overload for string JSONValue
		/// </summary>
		/// <param name="value">string value</param>
		public static explicit operator JsonValue(string value)
		{
			if (value == default(string))
				throw new InvalidCastException("Null string value cannot be casted as JsonValue.");

			return new JsonValue(value, JsonDataType.String);
		}

		#endregion
	}
}
