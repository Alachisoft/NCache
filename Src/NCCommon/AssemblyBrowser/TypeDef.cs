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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Alachisoft.NCache.Common.AssemblyBrowser
{
	public class TypeDef : IComparable<TypeDef>
	{
		/// <summary>Members declared by the type itself, whatever their accessibility.</summary>
		const BindingFlags DeclaredMembers = BindingFlags.Public | BindingFlags.NonPublic
			| BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

		static readonly string[] NoInterfaces = new string[0];

		Type type;

		PropertyDef[] properties;
		FieldDef[] fields;
		string[] interfaceNames;

		public string Name { get { return type.Name; } }
		public string FullName { get { return GetFullName(type); } }
		public string Namespace { get { return type.Namespace; } }
		public bool IsGenericType
		{
			get
			{
				if (this.FullName.Contains("`"))
					return true;
				else
					return false;
			}
		}
		public bool IsPrimitive { get { return type.IsPrimitive; } }
		public bool IsValueType { get { return type.IsValueType; } }
		public string AssemblyFullName { get { return type.Assembly.FullName; } }
		public TypeDef BaseType
		{
			get
			{
				Type baseType = GetBaseType(type);

				if (baseType != null)
					return new TypeDef(baseType);

				return null;
			}
		}
		public bool IsClass
		{
			get
			{
				if (type != null)
					return type.IsClass;

				return false;
			}
		}


		public TypeDef(System.Type type)
		{
			this.type = type;
		}

		public ICollection GetGenericArguments()
		{
			if (type == null) return new Type[0];

			return type.GetGenericArguments();
		}

		public PropertyDef[] GetProperties()
		{
			if (properties == null)
			{
				List<PropertyDef> propertyList = new List<PropertyDef>();

				if (type != null)
					foreach (PropertyInfo prop in GetDeclaredProperties())
					{
						propertyList.Add(new PropertyDef(prop));
					}

				properties = propertyList.ToArray();
			}

			return properties;
		}

		public FieldDef[] GetFields()
		{
			if (fields == null)
			{
				List<FieldDef> fieldList = new List<FieldDef>();

				if (type != null)
					foreach (FieldInfo field in GetDeclaredFields())
					{
						fieldList.Add(new FieldDef(field));
					}

				fields = fieldList.ToArray();
			}

			return fields;
		}

		public PropertyDef GetProperty(string name)
		{
			foreach (PropertyDef property in this.GetProperties())
			{
				if (property.Name == name) return property;
			}

			return null;
		}

		public FieldDef GetField(string name)
		{
			foreach (FieldDef field in this.GetFields())
			{
				if (field.Name == name) return field;
			}

			return null;
		}

		public object GetCustomAttributeValue(string attributeName)
		{
			if (type == null) return null;

			foreach (CustomAttributeData ca in type.Assembly.GetCustomAttributesData())
				if (attributeName == ca.AttributeType.FullName)
					return ca.ConstructorArguments[0].Value;

			return null;
		}

		public bool HasDefaultConstructor()
		{
			if (type != null)
				foreach (ConstructorInfo constructor in type.GetConstructors(DeclaredMembers))
				{
					if (!constructor.IsStatic && constructor.GetParameters().Length == 0)
						return true;
				}

			return false;
		}

		public bool IsAssignableTo(Type filterType)
		{
			if (filterType == null || type == null)
				return false;

			if (this.FullName == filterType.FullName)
				return true;

			if (this.HasBaseType(filterType))
				return true;

			if (filterType.IsInterface && this.ImplementsInterface(filterType))
				return true;

			return false;
		}

		public bool ImplementsInterface(Type interfaceType)
		{
			if (interfaceType == null)
				return false;

			foreach (string implemented in this.GetInterfaceNames())
			{
				if (implemented.Equals(interfaceType.FullName))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Full names of every interface the type implements, including the ones it
		/// inherits from its base types.
		/// </summary>
		public string[] GetInterfaceNames()
		{
			if (interfaceNames == null)
			{
				interfaceNames = NoInterfaces;

				if (type != null)
					try
					{
						List<string> names = new List<string>();

						foreach (Type implemented in type.GetInterfaces())
						{
							string name = GetFullName(implemented);
							if (name != null) names.Add(name);
						}

						interfaceNames = names.ToArray();
					}
					catch (Exception)
					{
						// an interface declared in an assembly that was not deployed along
						// side the inspected one cannot be read; the type stays usable.
					}
			}

			return interfaceNames;
		}

		public bool HasBaseType(Type baseType)
		{
			if (baseType == null || type == null)
				return false;

			for (Type current = GetBaseType(type); current != null; current = GetBaseType(current))
			{
				if (GetFullName(current) == baseType.FullName)
					return true;
			}

			return false;
		}

		public int CompareTo(TypeDef other)
		{
			return Name.CompareTo(other.Name);
		}

		private PropertyInfo[] GetDeclaredProperties()
		{
			try
			{
				return type.GetProperties(DeclaredMembers);
			}
			catch (Exception)
			{
				return new PropertyInfo[0];
			}
		}

		private FieldInfo[] GetDeclaredFields()
		{
			try
			{
				return type.GetFields(DeclaredMembers);
			}
			catch (Exception)
			{
				return new FieldInfo[0];
			}
		}

		private static Type GetBaseType(Type type)
		{
			if (type == null) return null;

			try
			{
				return type.BaseType;
			}
			catch (Exception)
			{
				// the base type lives in an assembly that could not be resolved.
				return null;
			}
		}

		/// <summary>
		/// Renders a type name the way the metadata of the assembly spells it, i.e.
		/// "Namespace.Type`1&lt;Namespace.Argument&gt;" for a closed generic type, rather than
		/// the assembly qualified "Namespace.Type`1[[Namespace.Argument, Assembly, ...]]"
		/// form reflection produces. Callers parse these names, so the shape matters.
		/// </summary>
		internal static string GetFullName(Type type)
		{
			if (type == null) return null;

			// an array, by reference or pointer type is spelled in terms of its element
			// type, which may itself be generic and so need the treatment below.
			if (type.IsArray || type.IsByRef || type.IsPointer)
			{
				string elementName = GetFullName(type.GetElementType());

				if (elementName == null) return type.FullName;

				return string.Concat(elementName, GetElementSuffix(type));
			}

			if (!type.IsGenericType || type.IsGenericTypeDefinition)
				return type.FullName;

			StringBuilder name = new StringBuilder();

			name.Append(type.GetGenericTypeDefinition().FullName);
			name.Append('<');

			Type[] arguments = type.GetGenericArguments();
			for (int i = 0; i < arguments.Length; i++)
			{
				if (i > 0) name.Append(',');
				name.Append(GetFullName(arguments[i]));
			}

			name.Append('>');

			return name.ToString();
		}

		private static string GetElementSuffix(Type type)
		{
			if (type.IsByRef) return "&";
			if (type.IsPointer) return "*";

			int rank = type.GetArrayRank();

			if (rank <= 1) return "[]";

			return string.Concat("[", new string(',', rank - 1), "]");
		}
	}
}
