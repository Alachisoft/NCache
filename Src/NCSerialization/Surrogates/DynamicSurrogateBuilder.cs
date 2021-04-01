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
using System.Net;
using System.Collections;
using System.Reflection;
using System.Web.SessionState;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using Alachisoft.NCache.IO;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Net;
using System.Collections.Generic;
using System.Reflection.Emit;
using Alachisoft.NCache.Runtime.Serialization;
using System.Web;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System.IO;
using System.Threading;

namespace Alachisoft.NCache.Serialization.Surrogates
{
    /// <summary>
    /// Responsible for emitting delegates for reading and writing a PONO (Plain Old .NET Object) type.
    /// </summary>
    public class DynamicSurrogateBuilder
    {
        /// <summary> Cached member pointing to Type::GetTypeFromHandle </summary>
        static private MethodInfo _type_GetTypeFromHandle;
        /// <summary> Cached member pointing to CompactBinaryWriter::WriteObject </summary>
        static private MethodInfo _compactBinaryWriter_WriteObject;
        /// <summary> Cached member pointing to CompactBinaryWriter::WriteObjectAs </summary>
        static private MethodInfo _compactBinaryWriter_WriteObjectAs;
        /// <summary> Cached member pointing to CompactBinaryReader::ReadObject </summary>
        static private MethodInfo _compactBinaryReader_ReadObject;
        /// <summary> Cached member pointing to CompactBinaryReader::ReadObjectAs </summary>
        static private MethodInfo _compactBinaryReader_ReadObjectAs;
        /// <summary> Cached member pointing to CompactBinaryReader::SkipObject </summary>
        static private MethodInfo _compactBinaryReader_SkipObject;

        static private MethodInfo _compactBinaryReader_IfSkip;
        
        /// <summary> For Portable Types, exit if End of File Surrogate appears </summary>
        static private Label EOFNet;

        /// <summary> [Multiple Labels]Skip assigning of returned value if skip surrogate is read; then the default value is kept</summary>
        static private Label[] SKIP;

        /// <summary> [Multiple Labels] In case of no skip surrogate found continue work</summary>
        static private Label[] CONTINUE;

        static private Hashtable _attributeOrder;

        static private Hashtable _nonCompactFields;

        /// <summary>
        /// If portable, attribute order matters else attribute order is skipped
        /// </summary>
        static private bool portable = false;

        static private short _subTypeHandle;

        /// <summary>
        /// If portable, attribute order matters else attribute order is skipped
        /// </summary>
        public static bool Portable
        {
            get { return portable; }
            set { portable = value; }
        }

        public static short SubTypeHandle
        {
            get { return _subTypeHandle; }
            set { _subTypeHandle = value; }
        }

        /// <summary>
        /// Static constructor caches meta information
        /// </summary>
        static DynamicSurrogateBuilder()
        {
            _type_GetTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle");
            _compactBinaryWriter_WriteObject = typeof(CompactBinaryWriter).
                GetMethod("WriteObject", new Type[1] { typeof(object) });
            _compactBinaryWriter_WriteObjectAs = typeof(CompactBinaryWriter).
                GetMethod("WriteObjectAs", new Type[2] { typeof(object), typeof(Type) });
            _compactBinaryReader_ReadObject = typeof(CompactBinaryReader).
                GetMethod("ReadObject", Type.EmptyTypes);
            _compactBinaryReader_ReadObjectAs = typeof(CompactBinaryReader).
                GetMethod("ReadObjectAs", new Type[1] { typeof(Type) });
            _compactBinaryReader_SkipObject = typeof(CompactBinaryReader).
                GetMethod("SkipObject");

            _compactBinaryReader_IfSkip = typeof(CompactBinaryReader).
                GetMethod("IfSkip", new Type[2] { typeof(object), typeof(object) });
        }


        /// <summary>
        /// Creates a list of FieldInfo and returns back 
        /// This List is created only for those attributes registered in config
        /// </summary>
        /// <param name="type">Type bieng registered</param>
        /// <param name="list">List of attribute/Fields present in the current class</param>
        /// <returns></returns>
        public static List<FieldInfo> GetAllFields(Type type, List<FieldInfo> list)
        {
            if (type == typeof(Object)) return new List<FieldInfo>();

            string[][] attribOrder = null;
            if (_attributeOrder != null)
                attribOrder = (string[][])(_attributeOrder[type]);

            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance;
            List<FieldInfo> fieldList = new List<FieldInfo>();
            fieldList.AddRange(type.GetFields(flags));
            Type currentType = type;
            while (true)
            {
                currentType = currentType.BaseType;
                if (currentType == typeof(Object))
                    break;
                fieldList.AddRange(currentType.GetFields(flags));
            }

            FieldInfo[] fields = fieldList.ToArray();

            FieldInfo[] DotNetFields = null;

            //Order out attributes
            if (attribOrder != null && Portable && attribOrder.Length != 0)
            {
                FieldInfo[] tempField = new FieldInfo[attribOrder[0].Length+1];
                bool EOF = true;
                for (int i = 0; i < (attribOrder[0].Length+1); i++)
                {
                    if (i == (attribOrder[0].Length) && EOF)
                    {
                        break;
                    }
                    int number = i;
                    if (!EOF)
                        number = i - 1;
                    if (attribOrder[0][number] != "skip.attribute")
                    {
                        //if (EOF && i == attribOrder[0].Length)
                        //    break;
                        if (attribOrder[1][number] == "-1" && EOF)
                        {
                            tempField[i] = null;
                            EOF = false;
                            continue;
                        }
                        for (int j = 0; j < fields.Length; j++)
                        {
                            if (attribOrder[0][number] == fields[j].Name)
                            {
                                tempField[i] = fields[j];
                            }
                        }
                        if (tempField[i] == null)
                            throw new Exception("Unable to intialize Compact Serialization: Assembly mismatch, The Assembly provided to Cache Manager is different to the one used locally: Unable to find Field " + attribOrder[0][number] + " in " + type.FullName);
                    }
                    else
                    {
                        tempField[i] = null;
                    }
                }

                //No Portable types found
                //if (tempField[tempField.Length - 1] == null)
                //{
                //    DotNetFields = new FieldInfo[tempField.Length - 1];
                //    for (int i = 0; i < (tempField.Length-1); i++)
                //    {
                //        DotNetFields[i] = tempField[i];
                //    }
                //}
                //else
                DotNetFields = tempField;
            }
            else
            {
                DotNetFields = fields;
            }

            list.AddRange(DotNetFields);

            //If class has a base, that base is also called to be registered
            //current hashtable(attributeOrder) does not contain information of this base class, therefore no attribute ordering is done
            //visible Base attributes are resolved by BindingFlags to check all visibile, previously it was restriced to DeclaringTypes only
            //therefore, this simply does nothing
            //GetAllFields(type.BaseType, list);

            return list;
        }

        /// <summary>
        /// Creates and returns a dynamic surrogate for the given <paramref name="type"/> parameter.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to generate the surrogate for</param>
        /// <returns>A dynamically generated <see cref="SerializationSurrogate"/></returns>
        public static SerializationSurrogate CreateTypeSurrogate(Type type, Hashtable attributeOrder,Hashtable nonCompactFields)
        {
            if (attributeOrder !=null)
                _attributeOrder = new Hashtable();

            if (nonCompactFields != null)
                _nonCompactFields = new Hashtable();


            //'attributeOrder' contains definition of attributes of all classes registered to the compact framework, we only reguire of that specific type which is to be registered at the moment
            //FYI: _attributeOrder is global within this class and attributeOrder is the passed in parameter
            if (_attributeOrder != null && attributeOrder != null)
                _attributeOrder.Add(type,attributeOrder[type.FullName]);

            if (_nonCompactFields != null && nonCompactFields != null)
                _nonCompactFields.Add(type, nonCompactFields);

            Type surrogateType;
            if (type.IsValueType)
            {
                surrogateType = typeof(DynamicValueTypeSurrogate<>);
            }
            else
            {
                surrogateType = typeof(DynamicRefTypeSurrogate<>);
            }
            return (SerializationSurrogate) SurrogateHelper.CreateGenericTypeInstance(surrogateType, type);
        }

        /// <summary>
        /// Generates a <see cref="WriteObjectDelegate"/> method for serializing an object of 
        /// given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of the object to be serialized</param>
        /// <returns>A dynamically generated delegate that serializes <paramref name="type"/> object</returns>
        internal static WriteObjectDelegate CreateWriterDelegate(Type type)
        {
            // Specify the types of the parameters of the dynamic method. This 
            // dynamic method has an CompactBinaryWriter parameter and an object parameter.
            Type[] methodArgs = { typeof(CompactBinaryWriter), typeof(object) };

            // Create a dynamic method with the name "", a return type
            // of void, and two parameters whose types are specified by
            // the array methodArgs. Create the method in the module that
            // defines the type.
            DynamicMethod method = new DynamicMethod(String.Empty,
                                MethodAttributes.Public | MethodAttributes.Static,
                                CallingConventions.Standard,
                                null,
                                methodArgs,
                                type,
                                true);


            // Get an ILGenerator and emit a body for the dynamic method,
            ILGenerator il = method.GetILGenerator();
            if (!portable)
                EmitWriterMethod(type, il);
            else
                EmitPortableWriterMethod(type, il);

            return (WriteObjectDelegate) method.CreateDelegate(typeof(WriteObjectDelegate));
        }       

        #region /       writer delegate generation        /
        
        /// <summary>
        /// Generates a dynamic method for serializing an object of given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of the object to be serialized</param>
        /// <param name="il">The IL generator object for the dynamic method</param>
        internal static void EmitWriterMethod(Type type, ILGenerator il)
        {
            List<FieldInfo> list = new List<FieldInfo>();

            Hashtable nonCompactFieldsTable = null;
            if (_nonCompactFields!=null&&_nonCompactFields.Contains(type))
                nonCompactFieldsTable = _nonCompactFields[type] as Hashtable;

            list = GetAllFields(type, list);//type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); //(FieldInfo[])FormatterServices.GetSerializableMembers(type);

            // Declare local variables for the method.
            LocalBuilder objLocal = il.DeclareLocal(type);

            // Cast the input graph to an object of 'type'.
            il.Emit(OpCodes.Ldarg_1);
            if (type.IsValueType)
                il.Emit(OpCodes.Unbox_Any, type);
            else
                il.Emit(OpCodes.Castclass, type);
            // Assign to local variable objLocal
            il.Emit(OpCodes.Stloc_0);

            // Emit write instruction for each serializable field
            //for (int i = 0; i < fields.Length; i++)
            foreach(FieldInfo field in list)
            {
                //FieldInfo field = fields[i];
                ///[Ata]If the class contains a difference intance of the same class
                /// then this check will fail and the instance could not be serialized/deserialized.
                ///if (field.FieldType.IsSerializable)
                if (nonCompactFieldsTable != null && nonCompactFieldsTable.Contains(field.Name))
                    continue;
                else
                {
                    // Load the first argument, which is the writer object, onto the stack.
                    il.Emit(OpCodes.Ldarg_0);
                    if (type.IsValueType)
                        il.Emit(OpCodes.Ldloca_S, objLocal);
                    else
                        il.Emit(OpCodes.Ldloc_0);

                    // Emit instruction to call appropriate method on the writer.
                    EmitWriteInstruction(field, il);                    
                }
            }
            // Return from the method.
            il.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Emits IL instructions to serialize the given <paramref name="field"/> using an 
        /// <see cref="CompactBinaryWriter"/>
        /// </summary>
        /// <param name="field">Field information for the field to be serialized</param>
        /// <param name="il">The IL generator object for the dynamic method</param>
        private static void EmitWriteInstruction(FieldInfo field, ILGenerator il)
        {
            MethodInfo writeMethod = null;
            if (field != null)
            {
                Type fieldType = field.FieldType;

                // Load the value of the field member
                il.Emit(OpCodes.Ldfld, field);
                if (fieldType.IsPrimitive)
                {
                    // Find a specialized method to write to stream
                    writeMethod = typeof(CompactBinaryWriter).GetMethod("Write", new Type[1] { fieldType });
                    if (writeMethod != null)
                    {
                        // Call the specialized method
                        il.Emit(OpCodes.Callvirt, writeMethod);
                    }
                }

                // If no specialized method was found, use generic writer methods
                if (writeMethod == null)
                {
                    // Value types must be boxed
                    if (fieldType.IsValueType)
                        il.Emit(OpCodes.Box, fieldType);

                    if (fieldType.IsInterface || !fieldType.IsPrimitive)
                    {
                        // Generate call to WriteObject() method
                        il.Emit(OpCodes.Callvirt, _compactBinaryWriter_WriteObject);
                    }
                    else
                    {
                        // Generate call to WriteObjectAs() method
                        il.Emit(OpCodes.Ldtoken, fieldType);
                        il.Emit(OpCodes.Call, _type_GetTypeFromHandle);
                        il.Emit(OpCodes.Callvirt, _compactBinaryWriter_WriteObjectAs);
                    }
                }
            }
            else
            {

                //il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Pop);
                //il.EmitWriteLine("init EOF");
                il.Emit(OpCodes.Newobj, typeof(EOFJavaSerializationSurrogate).GetConstructor(System.Type.EmptyTypes));
                //il.EmitWriteLine("init EOF done");
                // Generate call to WriteObject() method
                il.Emit(OpCodes.Callvirt, _compactBinaryWriter_WriteObject);
                //il.EmitWriteLine("init EOF uploaded");
            }
        }

        /// <summary>
        /// Emits IL instructions to serialize the given <paramref name="field"/> using an 
        /// <see cref="CompactBinaryWriter"/>
        /// </summary>
        /// <param name="field">Field information for the field to be serialized</param>
        /// <param name="il">The IL generator object for the dynamic method</param>
        private static void EmitPortableWriterMethod(Type type, ILGenerator il)
        {
            List<FieldInfo> list = new List<FieldInfo>();
            list = GetAllFields(type, list);//type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); //(FieldInfo[])FormatterServices.GetSerializableMembers(type);
            
            //il.EmitWriteLine("EmitPortableWriterMethod");

            string[][] attribOrder = null;
            if (_attributeOrder != null)
                attribOrder = (string[][])(_attributeOrder[type]);

            // Declare local variables for the method. Calls in the default constructor
            LocalBuilder objLocal = il.DeclareLocal(type);

            // Cast the input graph(object) to an object of 'type'.
            il.Emit(OpCodes.Ldarg_1);
            if (type.IsValueType)
                il.Emit(OpCodes.Unbox_Any, type);
            else
                il.Emit(OpCodes.Castclass, type);

            // Assign casted value to local variable: objLocal
            il.Emit(OpCodes.Stloc_0);

            // Emit write instruction for each serializable field
            for (int i = 0; i < list.Count; i++)
            //foreach (FieldInfo field in list)
            {
                FieldInfo field = list[i];
                ///[Ata]If the class contains a difference intance of the same class
                /// then this check will fail and the instance could not be serialized/deserialized.
                //if (field.FieldType.IsSerializable)
                bool toSkip = false;
                if(i < attribOrder[1].Length && attribOrder[1][i] == "0")
                {
                    toSkip = true;
                }

                if (!(field == null && toSkip))
                {
                    // Load the first argument, which is the writer object, onto the stack.
                    il.Emit(OpCodes.Ldarg_0);

                    if (type.IsValueType)
                        il.Emit(OpCodes.Ldloca_S, objLocal);
                    else
                        il.Emit(OpCodes.Ldloc_0);

                    // Emit instruction to call appropriate method on the writer.
                    EmitPortableWriteInstruction(field, il);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_0);
                    
                    il.Emit(OpCodes.Newobj, typeof(SkipSerializationSurrogate).GetConstructor(System.Type.EmptyTypes));
                    //il.EmitWriteLine("init EOF done");
                    // Generate call to WriteObject() method
                    il.Emit(OpCodes.Callvirt, _compactBinaryWriter_WriteObject);
                    //il.EmitWriteLine("init EOF uploaded");
                }
            }
            // Return from the method.
            il.Emit(OpCodes.Ret);
        }

        private static void EmitPortableWriteInstruction(FieldInfo field, ILGenerator il)
        {
            MethodInfo writeMethod = null;
            if (field != null)
            {
                Type fieldType = field.FieldType;

                // Load the value of the field member
                il.Emit(OpCodes.Ldfld, field);
                if (fieldType.IsPrimitive)
                {
                    //surrogate of a primitive WILL be returned
                    //il.EmitWriteLine("EmitPortableWriteInstruction");
                    ISerializationSurrogate surrogate = TypeSurrogateSelector.GetSurrogateForType(fieldType, null);
                    //il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4, (int)surrogate.TypeHandle);
                    il.Emit(OpCodes.Conv_I2);
                    MethodInfo writeMethods = typeof(CompactBinaryWriter).GetMethod("Write", new Type[1] { typeof(short) });
                    if (writeMethods != null)
                    {
                        //il.EmitWriteLine(writeMethods.ToString());
                        // Call the specialized method
                        il.Emit(OpCodes.Callvirt, writeMethods);
                        //il.EmitWriteLine(writeMethods.ToString());
                    }
                    //il.EmitWriteLine("EmitPortableWriteInstruction Before ");
                    //il.Emit(OpCodes.Ldfld, field);
                    //il.EmitWriteLine("EmitPortableWriteInstruction Primitive ");
                    //writeMethod = null;
                    // Find a specialized method to write to stream
                    writeMethod = typeof(CompactBinaryWriter).GetMethod("Write", new Type[1] { fieldType });
                    if (writeMethod != null)
                    {
                        // Call the specialized method
                        il.Emit(OpCodes.Callvirt, writeMethod);
                    }
                }

                // If no specialized method was found, use generic writer methods
                if (writeMethod == null)
                {
                    // Value types must be boxed
                    if (fieldType.IsValueType)
                        il.Emit(OpCodes.Box, fieldType);

                    if (fieldType.IsInterface || !fieldType.IsPrimitive)
                    {
                        // Generate call to WriteObject() method
                        il.Emit(OpCodes.Callvirt, _compactBinaryWriter_WriteObject);
                    }
                    else
                    {
                        // Generate call to WriteObjectAs() method
                        il.Emit(OpCodes.Ldtoken, fieldType);
                        il.Emit(OpCodes.Call, _type_GetTypeFromHandle);
                        il.Emit(OpCodes.Callvirt, _compactBinaryWriter_WriteObjectAs);
                    }
                }
            }
            else
            {

                //il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Pop);
                //il.EmitWriteLine("init EOF");
                il.Emit(OpCodes.Newobj, typeof(EOFJavaSerializationSurrogate).GetConstructor(System.Type.EmptyTypes));
                //il.EmitWriteLine("init EOF done");
                // Generate call to WriteObject() method
                
                il.Emit(OpCodes.Callvirt, _compactBinaryWriter_WriteObject);


                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4, (int)SubTypeHandle);
                il.Emit(OpCodes.Conv_I2);
                MethodInfo writeMethods = typeof(CompactBinaryWriter).GetMethod("Write", new Type[1] { typeof(short) });
                if (writeMethods != null)
                {
                    //il.EmitWriteLine(writeMethods.ToString());
                    // Call the specialized method
                    il.Emit(OpCodes.Callvirt, writeMethods);
                    //il.EmitWriteLine(writeMethods.ToString());
                }
                //il.EmitWriteLine("init EOF uploaded");
            }
        } 

        #endregion

        /// <summary>
        /// Generates a <see cref="ReadObjectDelegate"/> method for deserializing an object of 
        /// given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of the object to be deserialized</param>
        /// <returns>A dynamically generated delegate that deserializes <paramref name="type"/> object</returns>
        static internal ReadObjectDelegate CreateReaderDelegate(Type type)
        {
            // Specify the types of the parameters of the dynamic method. This 
            // dynamic method has an CompactBinaryReader parameter and an object parameter.
            Type[] methodArgs = { typeof(CompactBinaryReader), typeof(object) };

            // Create a dynamic method with the name "", a return type
            // of object, and two parameters whose types are specified by
            // the array methodArgs. Create the method in the module that
            // defines the type.
            DynamicMethod method = new DynamicMethod(String.Empty,
                                MethodAttributes.Public | MethodAttributes.Static,
                                CallingConventions.Standard,
                                typeof(object),
                                methodArgs,
                                type,
                                true);

            // Get an ILGenerator and emit a body for the dynamic method,
            ILGenerator il = method.GetILGenerator();

            LocalBuilder localObj = il.DeclareLocal(type);

            #region TestCode
            //List<FieldInfo> list = new List<FieldInfo>();
            //list = GetAllFields(type, list);

            //il.Emit(OpCodes.Ldarg_1);
            //il.Emit(OpCodes.Castclass, type);
            //il.Emit(OpCodes.Stloc_0);

            //foreach (FieldInfo field in list)
            //{
            //    if (field.FieldType.IsPrimitive)
            //    {
            //        il.Emit(OpCodes.Ldloc_0);
            //        il.Emit(OpCodes.Ldarg_0);



            //        il.Emit(OpCodes.Ldarg_0);
            //        il.Emit(OpCodes.Callvirt, _compactBinaryReader_ReadObject);

            //        //il.Emit(OpCodes.Unbox_Any, field.FieldType);


            //        //il.Emit(OpCodes.Stfld, field);


            //        il.Emit(OpCodes.Ldloc_0);
            //        il.Emit(OpCodes.Ldfld, field);
            //        il.Emit(OpCodes.Box, typeof(int));
            //        //il.Emit(OpCodes.Ldfld, field);
            //        //il.Emit(OpCodes.Callvirt, _compactBinaryReader_ReadObject);
            //        il.Emit(OpCodes.Callvirt, _compactBinaryReader_IfSkip);



            //        //il.Emit(OpCodes.Pop);

            //        il.Emit(OpCodes.Unbox_Any, field.FieldType);
            //        il.Emit(OpCodes.Stfld, field);
            //    }
            //    else
            //    {


            //        il.Emit(OpCodes.Ldloc_0);
            //        il.Emit(OpCodes.Ldarg_0);



            //        il.Emit(OpCodes.Ldarg_0);
            //        il.Emit(OpCodes.Callvirt, _compactBinaryReader_ReadObject);

            //        //il.Emit(OpCodes.Unbox_Any, field.FieldType);


            //        //il.Emit(OpCodes.Stfld, field);


            //        il.Emit(OpCodes.Ldloc_0);
            //        il.Emit(OpCodes.Ldfld, field);
            //        //il.Emit(OpCodes.Ldfld, field);
            //        //il.Emit(OpCodes.Callvirt, _compactBinaryReader_ReadObject);
            //        il.Emit(OpCodes.Callvirt, _compactBinaryReader_IfSkip);

            //        //il.Emit(OpCodes.Pop);

            //        il.Emit(OpCodes.Castclass, field.FieldType);
            //        il.Emit(OpCodes.Stfld, field);

            //    }
            //}
            //il.Emit(OpCodes.Ldloc_0);
            //il.Emit(OpCodes.Ret); 
            #endregion

            if (!portable)
                EmitReaderMethod(type, il);
            else
                EmitPortableReaderMethod(type, il);

            return (ReadObjectDelegate) method.CreateDelegate(typeof(ReadObjectDelegate));
        }

        #region /       reader delegate generation        /

        /// <summary>
        /// Generates a dynamic method for deserializing an object of given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of the object to be deserialized</param>
        /// <param name="il">The IL generator object for the dynamic method</param>
        internal static void EmitReaderMethod(Type type, ILGenerator il)
        {
            //FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); //(FieldInfo[])FormatterServices.GetSerializableMembers(type);
            List<FieldInfo> list = new List<FieldInfo>();
            Hashtable nonCompactFieldsTable = null;
            if(_nonCompactFields!=null && _nonCompactFields.Contains(type))
                nonCompactFieldsTable = _nonCompactFields[type] as Hashtable;
            list = GetAllFields(type, list);

            // Declare local variables for the method.
            LocalBuilder objLocal = il.DeclareLocal(type);
            LocalBuilder retType = il.DeclareLocal(typeof(object));

            EOFNet = il.DefineLabel();

            // Cast the input graph to an object of 'type'.
            il.Emit(OpCodes.Ldarg_1);
            if (type.IsValueType)
                il.Emit(OpCodes.Unbox_Any, type);
            else
                il.Emit(OpCodes.Castclass, type);
            // Assign to local variable objLocal
            il.Emit(OpCodes.Stloc_0);

            // Emit read instruction for each serializable field
            //for (int i = 0; i < fields.Length; i++)
            foreach(FieldInfo field in list)
            {
                //FieldInfo field = fields[i];
                ///[Ata]If the class contains a difference intance of the same class
                /// then this check will fail and the instance could not be serialized/deserialized.
                ///if (field.FieldType.IsSerializable)
                if (nonCompactFieldsTable != null && nonCompactFieldsTable.Contains(field.Name))
                    continue;
                else
                {
                    // Load the local object objLocal
                    if (type.IsValueType)
                        il.Emit(OpCodes.Ldloca_S, objLocal);
                    else
                        il.Emit(OpCodes.Ldloc_0);

                    // Load the first argument, which is the reader object, onto the stack.
                    il.Emit(OpCodes.Ldarg_0);

                    // Emit instruction to call appropriate method on the reader and assign 
                    // result to the field member of the object objLocal.
                    EmitReadInstruction(field, il);
                }
            }

            il.MarkLabel(EOFNet);
            // load the local object for returning to the caller
            il.Emit(OpCodes.Ldloc_0);
            if (type.IsValueType)
            {
                // Value types must be boxed
                il.Emit(OpCodes.Box, type);
            }
            il.Emit(OpCodes.Stloc_1);
            il.Emit(OpCodes.Ldloc_1);
            // Return from the method.
            il.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Emits IL instructions to deserialize the given <paramref name="field"/> using an 
        /// <see cref="CompactBinaryReader"/>
        /// </summary>
        /// <param name="field">Field information for the field to be deserialized</param>
        /// <param name="il">The IL generator object for the dynamic method</param>
        private static void EmitReadInstruction(FieldInfo field, ILGenerator il)
        {
            MethodInfo readMethod = null;
            if (field != null)
            {
                Type fieldType = field.FieldType;

                if (fieldType.IsPrimitive)
                {
                    // Find a specialized method to read from stream
                    String methodName = "Read" + fieldType.Name;
                    readMethod = typeof(CompactBinaryReader).GetMethod(methodName, new Type[0]);
                    if (readMethod != null)
                    {
                        // Call the specialized method
                        il.Emit(OpCodes.Callvirt, readMethod);
                    }
                }

                // If no specialized method was found, use generic reader methods
                if (readMethod == null)
                {
                    if (fieldType.IsInterface || !fieldType.IsPrimitive)
                    {
                        // Generate call to ReadObject() method
                        il.Emit(OpCodes.Callvirt, _compactBinaryReader_ReadObject);
                    }
                    else
                    {
                        // Generate call to ReadObjectAs() method
                        il.Emit(OpCodes.Ldtoken, fieldType);
                        il.Emit(OpCodes.Call, _type_GetTypeFromHandle);
                        il.Emit(OpCodes.Callvirt, _compactBinaryReader_ReadObjectAs);
                    }

                    // Cast the result to appropriate type.
                    if (fieldType.IsValueType)
                        il.Emit(OpCodes.Unbox_Any, fieldType);
                    else
                        il.Emit(OpCodes.Castclass, fieldType);
                }

                // Assign the result to the field member
                il.Emit(OpCodes.Stfld, field);
            }
            else
            {
                //Expecting and EOF surrogate
                // Generate call to ReadObject() method
                //[DEBUG]
                //il.EmitWriteLine("calling ReadObj");
                il.Emit(OpCodes.Callvirt, _compactBinaryReader_ReadObject);

                //[DEBUG]
                //il.EmitWriteLine("Casting Value");
                //Check if EOF for Net or not
                il.Emit(OpCodes.Unbox_Any, typeof(Boolean));

                //Jump to end and return current class (skip reading other fields)
                //[DEBUG]
                //il.EmitWriteLine("Jump");

                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Brtrue, EOFNet);

            }
        }

        /// <summary>
        /// Generates a dynamic method for deserializing an object of given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of the object to be deserialized</param>
        /// <param name="il">The IL generator object for the dynamic method</param>
        private static void EmitPortableReaderMethod(Type type, ILGenerator il)
        {
            //FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); //(FieldInfo[])FormatterServices.GetSerializableMembers(type);
            List<FieldInfo> list = new List<FieldInfo>();
            list = GetAllFields(type, list);

            string[][] attribOrder = null;
            if (_attributeOrder != null)
                attribOrder = (string[][])(_attributeOrder[type]);

            

            // Declare local variables for the method.
            LocalBuilder objLocal = il.DeclareLocal(type);
            LocalBuilder retType = il.DeclareLocal(typeof(object));

            EOFNet = il.DefineLabel();

            // Cast the input graph to an object of 'type'.
            il.Emit(OpCodes.Ldarg_1);
            if (type.IsValueType)
                il.Emit(OpCodes.Unbox_Any, type);
            else
                il.Emit(OpCodes.Castclass, type);
            // Assign to local variable objLocal
            il.Emit(OpCodes.Stloc_0);

            // Emit read instruction for each serializable field
            for (int i = 0; i < list.Count; i++)
            //foreach (FieldInfo field in list)
            {
                
                FieldInfo field = list[i];
                ///[Ata]If the class contains a difference intance of the same class
                /// then this check will fail and the instance could not be serialized/deserialized.
                ///if (field.FieldType.IsSerializable)

                bool toSkip = false;
                if (i < attribOrder[1].Length && attribOrder[1][i] == "0")
                {
                    toSkip = true;
                }

                if (!(field == null && toSkip))
                {
                    // Load the local object objLocal
                    if (type.IsValueType)
                        il.Emit(OpCodes.Ldloca_S, objLocal);
                    else
                        il.Emit(OpCodes.Ldloc_0);

                    // Load the first argument, which is the reader object, onto the stack.
                    il.Emit(OpCodes.Ldarg_0);

                    // Emit instruction to call appropriate method on the reader and assign 
                    // result to the field member of the object objLocal.
                    EmitPortableReadInstruction(field, il, objLocal);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Callvirt, _compactBinaryReader_SkipObject);
                }
            }

            il.MarkLabel(EOFNet);
            // load the local object for returning to the caller
            il.Emit(OpCodes.Ldloc_0);
            if (type.IsValueType)
            {
                // Value types must be boxed
                il.Emit(OpCodes.Box, type);
            }
            //il.Emit(OpCodes.Stloc_1);
            //il.Emit(OpCodes.Ldloc_1);
            // Return from the method.
            il.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Emits IL instructions to deserialize the given <paramref name="field"/> using an 
        /// <see cref="CompactBinaryReader"/>
        /// </summary>
        /// <param name="field">Field information for the field to be deserialized</param>
        /// <param name="il">The IL generator object for the dynamic method</param>
        private static void EmitPortableReadInstruction(FieldInfo field, ILGenerator il, LocalBuilder objLocal)
        {
            MethodInfo readMethod = null;
            if (field != null)
            {

                Type fieldType = field.FieldType;

                if (fieldType.IsPrimitive)
                {
                    // Find a specialized method to read from stream
                    String methodName = "Read" + fieldType.Name;
                    readMethod = typeof(CompactBinaryReader).GetMethod(methodName, new Type[0]);
                    //if (readMethod != null)
                    //{
                    //    // Call the specialized method
                    //    il.Emit(OpCodes.Callvirt, readMethod);
                    //}
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Callvirt, _compactBinaryReader_ReadObject);

                    //Might need to to allocate this memory on stack as volatile
                    il.Emit(OpCodes.Ldloc_0);
                    il.Emit(OpCodes.Ldfld, field);
                    il.Emit(OpCodes.Box, fieldType);
                    il.Emit(OpCodes.Callvirt, _compactBinaryReader_IfSkip);


                    //il.EmitWriteLine("before Unboxing");
                    il.Emit(OpCodes.Unbox_Any, fieldType);
                    //il.EmitWriteLine("after Unboxing");
                    
                    //il.Emit(OpCodes.Pop);
                    //il.Emit(OpCodes.Stloc_2);
                    il.Emit(OpCodes.Stfld, field);
                }

                // If no specialized method was found, use generic reader methods
                if (readMethod == null)
                {
                    if (fieldType.IsInterface || !fieldType.IsPrimitive)
                    {
                        // Generate call to ReadObject() method
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Callvirt, _compactBinaryReader_ReadObject);

                        //Might need to to allocate this memory on stack as volatile
                        il.Emit(OpCodes.Ldloc_0);
                        il.Emit(OpCodes.Ldfld, field);
                        il.Emit(OpCodes.Box, fieldType);
                        il.Emit(OpCodes.Callvirt, _compactBinaryReader_IfSkip);

                    }
                    else
                    {
                        // Generate call to ReadObjectAs() method
                        il.Emit(OpCodes.Ldtoken, fieldType);
                        il.Emit(OpCodes.Call, _type_GetTypeFromHandle);
                        il.Emit(OpCodes.Callvirt, _compactBinaryReader_ReadObjectAs);
                    }

                    // Cast the result to appropriate type.
                    if (fieldType.IsValueType)
                        
                        il.Emit(OpCodes.Unbox_Any, fieldType);
                    else
                        il.Emit(OpCodes.Castclass, fieldType);

                    il.Emit(OpCodes.Stfld, field);
                    
                }
                
            }
            else
            {
                
                //Expecting and EOF surrogate
                // Generate call to ReadObject() method
                //[DEBUG]
                //il.EmitWriteLine("calling ReadObj");
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Callvirt, _compactBinaryReader_ReadObject);
                
                //[DEBUG]
                //il.EmitWriteLine("Casting Value");
                //Check if EOF for Net or not
                il.Emit(OpCodes.Unbox_Any, typeof(short));

                //Jump to end and return current class (skip reading other fields)
                //[DEBUG]
                //il.EmitWriteLine("Jump");
                il.Emit(OpCodes.Ldc_I4, (int)SubTypeHandle);

                //il.Emit(OpCodes.Pop);
                //il.Emit(OpCodes.Brtrue, EOFNet);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Brfalse_S, EOFNet);
            }
        }

        #endregion
    }
}
