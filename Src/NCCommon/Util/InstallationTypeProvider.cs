using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Runtime.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Common
{
    [Serializable]
    public class InstallationTypeProvider : ICompactSerializable
    {
        private static InstallationTypeProvider instance = null;
        private InstallationType _installType ;
        static StringBuilder installCode = new StringBuilder();
      
        
        public bool IsServerInstallation
        {
            get
            {
                 return true;
            }
        }
       

        public static InstallationTypeProvider Provider
        {
            get
            {
                if (instance == null)
                {
                    lock (typeof(InstallationTypeProvider))
                    {
                        if (instance == null)
                            //this check will be based on store type whether its of search , messaging or distributedcache
                            instance = new InstallationTypeProvider();
                    }

                }
                return instance;
            }
        }


        private InstallationTypeProvider()
        {
            SetInstallationType();
        }

        private void SetInstallationType()
        {
            _installType = InstallationType.oss_server;

        }

       
       

        public string BuildType() 
        {
            return "server";
         
        }


        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            System.Enum.TryParse((string)reader.ReadString(), out InstallationType type);
            _installType = type;

        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(_installType.ToString());
        }

        #endregion

    }

    internal enum InstallationType
        {
           
            oss_server
        }

    

     
    }



