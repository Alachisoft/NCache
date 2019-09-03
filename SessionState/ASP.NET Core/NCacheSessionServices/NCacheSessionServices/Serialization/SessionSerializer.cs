using System.IO;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.IO;

namespace Alachisoft.NCache.Web.SessionState.Serialization
{
    public static class SessionSerializer
    {
        public static byte[] Serialize(NCacheSessionData sessionData)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new CompactBinaryWriter(stream))
                {
                    SerializationUtility.SerializeDictionary(sessionData.Items, writer);
                }
                return stream.GetBuffer();
            }
        }

        public static NCacheSessionData Deserialize(byte[] data)
        {
            NCacheSessionData sessionData = new NCacheSessionData();
            using (var stream = new MemoryStream(data))
            {
                using (var reader = new CompactBinaryReader(stream))
                {
                    sessionData.Items = SerializationUtility.DeserializeDictionary<string, byte[]>(reader);
                }
            }
            return sessionData;
        }
        #region Old Implementation
        //internal enum DataType : byte
        //{
        //    Null,
        //    ByteArray,
        //    Int,
        //    String,
        //    SessionData,
        //    SessionItems

        //}
        //public static byte[] Serialize(NCacheSessionData sessionData)
        //{
        //    byte[] bytes;
        //    using (var stream = new MemoryStream())
        //    {
        //        if (sessionData != null)
        //        {
        //            WriteDataType(stream, DataType.SessionData);
        //            if (sessionData.Items != null)
        //            {
        //                WriteDataType(stream, DataType.SessionItems);
        //                WriteInt32(stream, sessionData.Items.Count);
        //                foreach (var kvp in sessionData.Items)
        //                {
        //                    WriteString(stream, kvp.Key);
        //                    WriteBytes(stream, kvp.Value);
        //                }
        //            }
        //            else
        //            {
        //                WriteDataType(stream, DataType.Null);
        //            }
        //        }
        //        else
        //        {
        //            WriteDataType(stream, DataType.Null);
        //        }

        //        bytes = stream.GetBuffer();
        //    }
        //    return bytes;
        //}

        //public static NCacheSessionData Deserialize(byte[] data)
        //{
        //    NCacheSessionData session;
        //    using (var stream = new MemoryStream(data))
        //    {
        //        DataType type = ReadDataType(stream);
        //        if (type.Equals(DataType.SessionData))
        //        {
        //            session = new NCacheSessionData();
        //            type = ReadDataType(stream);
        //            if (type.Equals(DataType.SessionItems))
        //            {
        //                int count = ReadInt32(stream);
        //                for (int i = 0; i < count; i++)
        //                {
        //                    string key = ReadString(stream);
        //                    byte[] value = ReadBytes(stream);
        //                    session.Items.Add(key, value);
        //                }
        //            }
        //            else if (type.Equals(DataType.Null))
        //            {
        //                session.Items = null;
        //            }
        //            else throw new InvalidDataException("Incorrect deserialization sequence. ");
        //        }
        //        else if (type.Equals(DataType.Null))
        //            session = null;
        //        else throw new InvalidDataException("Incorrect deserialization sequence. ");
        //    }
        //    return session;
        //}

        //private static void WriteString(MemoryStream stream, string value)
        //{
        //    if (value != null)
        //    {
        //        WriteDataType(stream, DataType.String);
        //        byte[] utf8Bytes = Encoding.UTF8.GetBytes(value);
        //        WriteBytes(stream, utf8Bytes);
        //    }
        //    else
        //    {
        //        WriteDataType(stream, DataType.Null);
        //    }
        //}

        //private static string ReadString(MemoryStream stream)
        //{
        //    DataType type = ReadDataType(stream);
        //    if (type.Equals(DataType.String))
        //    {
        //        byte[] utf8Bytes = ReadBytes(stream);
        //        return Encoding.UTF8.GetString(utf8Bytes);
        //    }
        //    if(type.Equals(DataType.Null))
        //    {
        //        return null;
        //    }
        //    throw new InvalidDataException("Incorrect deserialization sequence. ");
        //}

        //private static void WriteInt32(MemoryStream stream, int value)
        //{
        //    unchecked
        //    {
        //        stream.WriteByte((byte)(value >> 24));
        //        stream.WriteByte((byte)(value >> 16));
        //        stream.WriteByte((byte)(value >> 8));
        //        stream.WriteByte((byte)value);
        //    }
        //}

        //private static int ReadInt32(MemoryStream stream)
        //{
        //    int b1 = stream.ReadByte();
        //    int b2 = stream.ReadByte();
        //    int b3 = stream.ReadByte();
        //    int b4 = stream.ReadByte();
        //    return ((b1 << 24) | (b2 << 16) | (b3 << 8) | (b4 << 0));
        //}

        //private static void WriteBytes(MemoryStream stream, byte[] bytes)
        //{
        //    if (bytes != null)
        //    {
        //        WriteDataType(stream,DataType.ByteArray);
        //        WriteInt32(stream, bytes.Length);
        //        stream.Write(bytes, 0, bytes.Length);
        //    }
        //    else
        //    {
        //        WriteDataType(stream, DataType.Null);
        //    }
        //}

        //private static byte[] ReadBytes(MemoryStream stream)
        //{
        //    DataType type = ReadDataType(stream);
        //    if (type.Equals(DataType.ByteArray))
        //    {
        //        int length = ReadInt32(stream);
        //        byte[] bytes = new byte[length];
        //        stream.Read(bytes, 0, bytes.Length);
        //        return bytes;
        //    }
        //    if (type.Equals(DataType.Null))
        //        return null;
        //    throw new InvalidDataException("Incorrect deserialization sequence. ");
        //}

        //private static DataType ReadDataType(MemoryStream stream)
        //{
        //    return (DataType) stream.ReadByte();
        //}

        //private static void WriteDataType(MemoryStream stream, DataType type)
        //{
        //    stream.WriteByte((byte) type);
        //}
        #endregion
    }
}
