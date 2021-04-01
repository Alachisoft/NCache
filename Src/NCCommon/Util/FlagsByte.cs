//  Copyright (c) 2021 Alachisoft
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
using System;

/// <summary>
/// Summary description for Class1
/// </summary>
namespace Alachisoft.NCache.Common
{    
	public class FlagsByte
	{		
		    [FlagsAttribute] 
		    public enum Flag : byte
		    {				// Hexidecimal		Decimal		Binary
			Clear = 0x00,	// 0x...0000		0			...00000000000000000
			TRANS = 0x01,		// 0x...0001		1			...00000000000000001			
            COR = TRANS << 1,	// 0x...0002		2			...00000000000000010
            TOTAL = COR << 1,	// 0x...0004		4			...00000000000000100
            TCP = TOTAL << 1,	// 0x...0008		8			...00000000000001000
            f5 = TCP << 1,	// 0x...0010		16			...00000000000010000
            f6 = f5 << 1,	// 0x...0020		32			...00000000000100000
			f7 = f6 << 1,	// 0x...0040		64			...00000000001000000
			f8 = f7 << 1,	// 0x...0080		128			...00000000010000000			
		};
		
		/// <summary>
		/// The Field that will store our 64 flags
		/// </summary>
		private byte _DataByte;	

		/// <summary>
		/// Public property SET and GET to access the Field
		/// </summary>
		public byte DataByte
		{
			get
			{
				return _DataByte;
			}
			set
			{
				_DataByte = value;
			}
		}
		
		/// <summary>
		/// Contructor
		/// Add all initialization here
		/// </summary>
		public FlagsByte()
        {  
            ClearField();
        }		

		/// <summary>
		/// ClearField clears all contents of the Field
		/// Set all bits to zero using the clear flag
		/// </summary>
		public void ClearField()
		{
			SetField(Flag.Clear);
		}

		/// <summary>
		/// FillField fills all contents of the Field
		/// Set all bits to zero using the negation of clear
		/// </summary>
		public void FillField()
		{
			SetField(~Flag.Clear);
		}

		/// <summary>
		/// Setting the specified flag(s) and turning all other flags off.
		///  - Bits that are set to 1 in the flag will be set to one in the Field.
		///  - Bits that are set to 0 in the flag will be set to zero in the Field. 
		/// </summary>
		/// <param name="flg">The flag to set in Field</param>
		private void SetField(Flag flg)
		{
			DataByte = (byte)flg;
		}

		/// <summary>
		/// Setting the specified flag(s) and leaving all other flags unchanged.
		///  - Bits that are set to 1 in the flag will be set to one in the Field.
		///  - Bits that are set to 0 in the flag will be unchanged in the Field. 
		/// </summary>
		/// <example>
		/// OR truth table
		/// 0 | 0 = 0
		/// 1 | 0 = 1
		/// 0 | 1 = 1
		/// 1 | 1 = 1
		/// </example>
		/// <param name="flg">The flag to set in Field</param>
		public void SetOn(Flag flg)
		{
			DataByte |= (byte)flg;
		}

		/// <summary>
		/// Unsetting the specified flag(s) and leaving all other flags unchanged.
		///  - Bits that are set to 1 in the flag will be set to zero in the Field.
		///  - Bits that are set to 0 in the flag will be unchanged in the Field. 
		/// </summary>
		/// <example>
		/// AND truth table
		/// 0 & 0 = 0
		/// 1 & 0 = 0
		/// 0 & 1 = 0
		/// 1 & 1 = 1
		/// </example>
		/// <param name="flg">The flag(s) to unset in Field</param>
		public void SetOff(Flag flg)
		{
            DataByte &= (byte)~flg;
		}

		/// <summary>
		/// Toggling the specified flag(s) and leaving all other bits unchanged.
		///  - Bits that are set to 1 in the flag will be toggled in the Field. 
		///  - Bits that are set to 0 in the flag will be unchanged in the Field. 
		/// </summary>
		/// <example>
		/// XOR truth table
		/// 0 ^ 0 = 0
		/// 1 ^ 0 = 1
		/// 0 ^ 1 = 1
		/// 1 ^ 1 = 0
		/// </example>
		/// <param name="flg">The flag to toggle in Field</param>
		public void SetToggle(Flag flg)
		{
			DataByte ^= (byte)flg;
		}

		/// <summary>
		/// AnyOn checks if any of the specified flag are set/on in the Field.
		/// </summary>
		/// <param name="flg">flag(s) to check</param>
		/// <returns>
		/// true if flag is set in Field
		/// false otherwise
		/// </returns>
		public bool AnyOn (Flag flg)
		{
			return (DataByte & (byte)flg) != 0;
		}

		/// <summary>
		/// AllOn checks if all the specified flags are set/on in the Field.
		/// </summary>
		/// <param name="flg">flag(s) to check</param>
		/// <returns>
		/// true if all flags are set in Field
		/// false otherwise
		/// </returns>
		public bool AllOn (Flag flg)
		{
			return (DataByte & (byte)flg) == (byte)flg;
		}

		/// <summary>
		/// IsEqual checks if all the specified flags are the same as in the Field.
		/// </summary>
		/// <param name="flg">flag(s) to check</param>
		/// <returns>
		/// true if all flags identical in the Field
		/// false otherwise
		/// </returns>
		public bool IsEqual(Flag flg)
		{
			return DataByte == (byte)flg;		
	    }       
	}
}
