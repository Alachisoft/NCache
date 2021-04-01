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
using System.Diagnostics;

namespace Alachisoft.NCache.Common
{
    //This class is to serve as the basis for live upgrades and cross version communication in future

    /// <summary>
    /// Returns the version of the NCache running on a particular Node. 
    /// This returns a single instance per installation
    /// </summary>
    struct ProductVersionType
    {
        public static int Major, Minor, Build, Revision;
    }
    public class ProductVersion: IComparable, Runtime.Serialization.ICompactSerializable
    {
        #region members

        private byte _majorVersion1=0;
        private byte _majorVersion2=0;
        private byte _minorVersion1=0;
        private byte _minorVersion2=0;
        private string _productName= string.Empty;
        private int _editionID=-1;
        private static volatile ProductVersion _productInfo;
        private static object _syncRoot = new object(); // this object is to serve as locking instance to avoid deadlocks
         private byte[] _additionalData;
        
        #endregion
        
        #region Properties

        /// <summary>
        /// Gets the productInfo 
        /// </summary>
        public static ProductVersion ProductInfo
        {
            //Double check locking approach is used because of the multi-threaded nature of NCache
            get
            {
                if (_productInfo == null)
                {
                    lock (_syncRoot)
                    {
                        if (_productInfo == null)
                        {
                            _productInfo = new ProductVersion();
#if SERVER
                            _productInfo._editionID = 32;
                            _productInfo._majorVersion1 = 4;
                            _productInfo._majorVersion2 = 3;
                            _productInfo._minorVersion1 = 0;
                            _productInfo._minorVersion2 = 0;
                            _productInfo._productName = "NCACHE";
                            _productInfo._additionalData = new byte[0];

#elif CLIENT
                            _productInfo._editionID = 33;
                            _productInfo._majorVersion1 = 4;
                            _productInfo._majorVersion2 = 3;
                            _productInfo._minorVersion1 = 0;
                            _productInfo._minorVersion2 = 0;
                            _productInfo._productName = "NCACHE";
                            _productInfo._additionalData = new byte[0];

#endif
                        }
                    }
                    
                }
                return _productInfo;
            }
            
        }



        /// <summary>
        /// Gets/Sets the Product Name(JvCache/NCache)
        /// </summary>
        public string ProductName
        {
            get { return _productName; }
            private set
            {
                _productName = value;
            }
        }
        
        /// <summary>
        /// Get/Sets the editionID of the product
        /// </summary>
        public int EditionID
        {
            get { return _editionID; }
            private set 
            {
                _editionID = value; 
            }
        }
        
        /// <summary>
        /// Get/Set the edditional data that needs
        /// to be send.
        /// </summary>
        public byte[] AdditionalData
        {
            get { return _additionalData; }
            private set { _additionalData = value; }
        } 

        /// <summary>
        /// Get/Set the second minor version of the API
        /// </summary>
        public byte MinorVersion2
        {
            get { return _minorVersion2; }
            private set { _minorVersion2 = value; }
        }

        /// <summary>
        /// Get/Set the first minor version of the API
        /// </summary>
        public byte MinorVersion1
        {
            get { return _minorVersion1; }
            private set { _minorVersion1 = value; }
        }

        /// <summary>
        /// Get/Set the second major version of the API
        /// </summary>
        public byte MajorVersion2
        {
          get { return _majorVersion2; }
          private set { _majorVersion2 = value; }
        }

        /// <summary>
        /// Get/Set the first major version of the API
        /// </summary>
        public byte MajorVersion1
        {
            get { return _majorVersion1; }
            private set { _majorVersion1 = value; }
        }
        #endregion

        #region methods

        /// <summary>
        /// 
        /// </summary>
        public ProductVersion() 
        {

        }

        /// <summary>
        /// compares editionIDs to ensure whether the version is correct or not
        /// </summary>
        /// <param name="id"></param>
        /// <returns>
        /// true: Incase of same edition
        /// False: Incase of incorrect edition
        /// </returns>
        public bool IsValidVersion(int editionID)
        {
            if (this.EditionID == editionID)
                return true;
            else
                return false;
        }

        public static string GetVersion()
        {
            string version;
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            version = fvi.FileVersion;
            try
            {
                string[] components = version.Split('.');
                int.TryParse(components[0], out ProductVersionType.Major);
                int.TryParse(components[1], out ProductVersionType.Minor);
                int.TryParse(components[2], out ProductVersionType.Build);
                int.TryParse(components[3], out ProductVersionType.Revision);

                //Example: AssemblyFileVersion is 4.9.0.0
                // productVersion = 4.8 (Major and Minor together form a product version)
                // servicePack = SP-2 (Build tells the service pack number)
                // privatePatch = PP-2 (Revision tells the private patch number)

                string productVersion = string.Format("{0}.{1}", ProductVersionType.Major, ProductVersionType.Minor);
                string servicePack = string.Format("{0}{1}", ProductVersionType.Build > 0 ? "SP" : string.Empty, ProductVersionType.Build > 0 ? ProductVersionType.Build.ToString() : string.Empty);
                string privatePatch = string.Format("{0}{1}", ProductVersionType.Revision > 0 ? "PP" : string.Empty, ProductVersionType.Revision > 0 ? ProductVersionType.Revision.ToString() : string.Empty);

                version = (string.Format("{0} {1} {2}", productVersion, servicePack, privatePatch)).TrimEnd();
                return version;

            }
            catch (Exception ex)
            {
                AppUtil.LogEvent("Error occured while reading product info: " + ex.ToString(), EventLogEntryType.Error);
                try
                {
                    string spVersion = (string)Alachisoft.NCache.Common.RegHelper.GetRegValue(Alachisoft.NCache.Common.RegHelper.ROOT_KEY, "SPVersion", 0);
                    return (string.Format("{0} {1} ", version, spVersion));

                }
                catch
                {
                    return null;
                }

            }


        }

        #endregion

        #region ICompact Serializable Members
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _majorVersion1 = reader.ReadByte();
            _majorVersion2 = reader.ReadByte();
            _minorVersion1 = reader.ReadByte();
            _minorVersion2 = reader.ReadByte();
            _productName =(string) reader.ReadObject();
            _editionID = reader.ReadInt32();
            int temp = reader.ReadInt32();
            _additionalData = reader.ReadBytes(temp);


        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(_majorVersion1);
            writer.Write(_majorVersion2);
            writer.Write(_minorVersion1);
            writer.Write(_minorVersion2);
            writer.WriteObject(_productName);
            writer.Write(_editionID);
            writer.Write(_additionalData.Length);// to know the lengt5h of the additional data to be followed; used when deserializing
            writer.Write(_additionalData);

        }
        #endregion
               
        #region IComparable Members
        public int CompareTo(object obj)
        {
            int result = -1;
            if (obj != null && obj is ProductVersion)
            {
                ProductVersion other = (ProductVersion)obj;

                if (_editionID == other.EditionID)
                {
                    if (_majorVersion1 == other.MajorVersion1)
                    {
                        if (_majorVersion2 == other.MajorVersion2)
                        {
                            if (_minorVersion1 == other.MinorVersion1)
                            {
                                if (_minorVersion2 == other._minorVersion2)
                                {
                                    result = 0;
                                }
                                else if (_minorVersion2 < other.MinorVersion2)
                                    result = -1;
                                else
                                    result = 1;
                            }
                            else if (_minorVersion1 < other.MinorVersion1)
                                result = -1;
                            else
                                result = 1;
                        }
                        else if (_majorVersion2 < other.MajorVersion2)
                            result = -1;
                        else
                            result = 1;
                    }
                    else if (_majorVersion1 < other.MajorVersion1)
                        result = -1;
                    else
                        result = 1;
                }
                else
                    result = -1;

            }
            return result;
        }
        #endregion
        
    }
}
