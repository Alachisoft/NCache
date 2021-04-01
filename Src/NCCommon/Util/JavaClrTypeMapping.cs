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
using System.Collections;

namespace Alachisoft.NCache.Common.Util
{
	/// <summary>
	/// Summary description for JavaClrTypeMapping.
	/// </summary>
	public class JavaClrTypeMapping
	{
		static Hashtable _mappingTable = new Hashtable();
		static Hashtable _predefinedTypes = new Hashtable();
		static Hashtable _collections = new Hashtable();
		static Hashtable _exceptions = new Hashtable();

        static JavaClrTypeMapping()
        {
            #region //         Predefined Types          //
            _predefinedTypes.Add("JavaToClr", new Hashtable());
            _predefinedTypes.Add("ClrToJava", new Hashtable());

            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("void", "void");             // void value
            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("bool", "java.lang.Boolean");          // True/false value
            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("System.Boolean", "java.lang.Boolean");// True/false value
            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("char", "java.lang.Character");             // Unicode character (16 bit)
            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("System.Char", "java.lang.Character");      // Unicode character (16 bit)
            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("string", "java.lang.String");// Unicode String
            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("System.String", "java.lang.String"); // Unicode String
            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("float", "java.lang.Float");         // IEEE 32-bit float
            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("System.Single", "java.lang.Float");   // IEEE 32-bit float
            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("double", "java.lang.Double");        // IEEE 64-bit float
            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("System.Double", "java.lang.Double");  // IEEE 64-bit float
            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("short", "java.lang.Short");           // Signed 16-bit integer
            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("System.Int16", "java.lang.Short");    // Signed 16-bit integer
            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("ushort", "java.lang.Short");           // Signed 16-bit integer
            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("System.UInt16", "java.lang.Short");   // Unsigned 16-bit integer
            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("int", "java.lang.Integer");             // Signed 32-bit integer
            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("System.Int32", "java.lang.Integer");      // Signed 32-bit integer
            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("uint", "java.lang.Integer");             // Signed 32-bit integer
            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("System.UInt32", "java.lang.Long");      // Signed 32-bit integer
            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("long", "java.lang.Long");            // Signed 64-bit integer
            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("System.Int64", "java.lang.Long");     // Signed 64-bit integer
            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("ulong", "java.lang.Long");            // Signed 64-bit integer
            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("System.UInt64", "java.lang.Long");     // Signed 64-bit integer
            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("byte", "java.lang.Byte");      // Unsigned 8-bit integer
            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("System.Byte", "java.lang.Byte");      // Unsigned 8-bit integer
            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("sbyte", "java.lang.Byte");      // Unsigned 8-bit integer
            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("System.SByte", "java.lang.Byte");      // Unsigned 8-bit integer
            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("System.Object", "java.lang.Object");  // Base class for all objects
            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("System.MarshalByRefObject", "java.lang.Object");  // Base class for all objects passed by reference
            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("System.DateTime", "java.util.Date"); // Dates will always be serialized (passed by value), according to .NET Remoting
            ((Hashtable)_predefinedTypes["ClrToJava"]).Add("System.Decimal", "java.math.BigDecimal"); // Will always be serialized (passed by value), according to .NET Remoting

            ((Hashtable)_predefinedTypes["JavaToClr"]).Add("void", "void");             // void value
            ((Hashtable)_predefinedTypes["JavaToClr"]).Add("boolean", "System.Boolean");          // True/false value
            ((Hashtable)_predefinedTypes["JavaToClr"]).Add("java.lang.Boolean", "System.Boolean");          // True/false value            
            ((Hashtable)_predefinedTypes["JavaToClr"]).Add("char", "System.Char");             // Unicode character (16 bit)
            ((Hashtable)_predefinedTypes["JavaToClr"]).Add("java.lang.Character", "System.Char");             // Unicode character (16 bit)            
            ((Hashtable)_predefinedTypes["JavaToClr"]).Add("java.lang.String", "System.String");// Unicode String
            ((Hashtable)_predefinedTypes["JavaToClr"]).Add("float", "System.Single");         // IEEE 32-bit float
            ((Hashtable)_predefinedTypes["JavaToClr"]).Add("java.lang.Float", "System.Single");         // IEEE 32-bit float
            ((Hashtable)_predefinedTypes["JavaToClr"]).Add("double", "System.Double");        // IEEE 64-bit float
            ((Hashtable)_predefinedTypes["JavaToClr"]).Add("java.lang.Double", "System.Double");        // IEEE 64-bit float
            ((Hashtable)_predefinedTypes["JavaToClr"]).Add("int", "System.Int32");      // Signed 32-bit integer
            ((Hashtable)_predefinedTypes["JavaToClr"]).Add("java.lang.Integer", "System.Int32");      // Signed 32-bit integer
            ((Hashtable)_predefinedTypes["JavaToClr"]).Add("long", "System.Int64");     // Signed 64-bit integer
            ((Hashtable)_predefinedTypes["JavaToClr"]).Add("java.lang.Long", "System.Int64");     // Signed 64-bit integer
            ((Hashtable)_predefinedTypes["JavaToClr"]).Add("byte", "System.Byte");      // Unsigned 8-bit integer
            ((Hashtable)_predefinedTypes["JavaToClr"]).Add("java.lang.Byte", "System.Byte");      // Unsigned 8-bit integer
            ((Hashtable)_predefinedTypes["JavaToClr"]).Add("short", "System.Int16");   // Unsigned 16-bit integer
            ((Hashtable)_predefinedTypes["JavaToClr"]).Add("java.lang.Short", "System.Int16");   // Unsigned 16-bit integer
            ((Hashtable)_predefinedTypes["JavaToClr"]).Add("java.util.Date", "System.DateTime");   // DateTime
            ((Hashtable)_predefinedTypes["JavaToClr"]).Add("java.math.BigDecimal", "System.Decimal");

            #endregion

            #region //           Collections             //
            _collections.Add("JavaToClr", new Hashtable());
            _collections.Add("ClrToJava", new Hashtable());

            ((Hashtable)_collections["JavaToClr"]).Add("java.util.ArrayList", "System.Collections.IList");
            ((Hashtable)_collections["JavaToClr"]).Add("java.util.LinkedList", "System.Collections.IList");
            ((Hashtable)_collections["JavaToClr"]).Add("java.util.Vector", "System.Collections.IList");
            ((Hashtable)_collections["JavaToClr"]).Add("java.util.Hashtable", "System.Collections.IDictionary");
            ((Hashtable)_collections["JavaToClr"]).Add("java.util.Hashmap", "System.Collections.IDictionary");
            ((Hashtable)_collections["JavaToClr"]).Add("java.util.TreeMap", "System.Collections.IDictionary");
            ((Hashtable)_collections["JavaToClr"]).Add("java.util.Properties", "System.Collections.IDictionary");

            ((Hashtable)_collections["ClrToJava"]).Add("System.Collections.ArrayList", "java.util.List");
            ((Hashtable)_collections["JavaToClr"]).Add("System.Collections.Hashtable", "java.util.Map");
            #endregion

            #region //           Exceptions              //
            _exceptions.Add("JavaToClr", new Hashtable());
            _exceptions.Add("ClrToJava", new Hashtable());

            #endregion

            _mappingTable.Add("predefinedtypes", _predefinedTypes);
            _mappingTable.Add("collections", _collections);
            _mappingTable.Add("exceptions", _exceptions);

        }

		public static string ClrToJava(string clrType)
		{

			if (((Hashtable)_predefinedTypes["ClrToJava"]).Contains(clrType))
			{
				return (string)(((Hashtable)_predefinedTypes["ClrToJava"])[clrType]);
			}
			else if (((Hashtable)_collections["ClrToJava"]).Contains(clrType))
			{
				return (string)(((Hashtable)_collections["ClrToJava"])[clrType]);
			}
			else if (((Hashtable)_exceptions["ClrToJava"]).Contains(clrType))
			{
				return (string)(((Hashtable)_exceptions["ClrToJava"])[clrType]);
			}
			else
				return null;
		}

		public static string JavaToClr(string javaType)
		{
			if (((Hashtable)_predefinedTypes["JavaToClr"]).Contains(javaType))
			{
				return (string)(((Hashtable)_predefinedTypes["JavaToClr"])[javaType]);
			}
			else if (((Hashtable)_collections["JavaToClr"]).Contains(javaType))
			{
				return (string)(((Hashtable)_collections["JavaToClr"])[javaType]);
			}
			else if (((Hashtable)_exceptions["JavaToClr"]).Contains(javaType))
			{
				return (string)(((Hashtable)_exceptions["JavaToClr"])[javaType]);
			}
			else
				return null;
		}
	}      
}
