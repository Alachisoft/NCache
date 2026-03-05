using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Alachisoft.NCache.Licensing.RegistryUtil;

namespace Alachisoft.NCache.Common.Util
{
    public class InstallTypeUtil
    {
       // ["anywhere", "on-prem", "aws", "azure", "aws-byol", "azure-byol", "aws-saas", "azure-saas"]

        public const string ONPREM = "on-prem";
        public const string AWS = "aws";
        public const string AWSSAAS = "aws-saas";
        public const string AWSBYOL = "aws-byol";
        public const string AZURE = "azure";
        public const string AZURESAAS = "azure-saas";
        public const string AZUREBYOL = "azure-byol";
        public const string DOCKERIMAGE = "docker_image";
        public const string DOCKER = "docker";


        public const string ONPREMCODE = "op";
        public const string AWSCODE = "aw";
        public const string AWSSAASCODE = "aws";
        public const string AWSBYOLCODE = "awb";
        public const string AZURECODE = "az";
        public const string AZURESAASCODE = "azs";
        public const string AZUREBYOLCODE = "azb";
        public const string DOCKERIMAGECODE = "dci";
        public const string DOCKERCODE = "dc";

        public static string GetInstallType(string installType)
        {
            if (installType.Equals(AWSCODE))
                return AWS;
            if (installType.Equals(AWSSAASCODE))
                return AWSSAAS;
            if (installType.Equals(AWSBYOLCODE))
                return AWSBYOL;
            if (installType.Equals(AZURECODE))
                return AZURE;
            if (installType.Equals(AZUREBYOLCODE))
                return AZUREBYOL;
            if (installType.Equals(AZURESAASCODE))
                return AZURESAAS;
            if (installType.Equals(DOCKERIMAGECODE))
                return DOCKERIMAGE;
            if (installType.Equals(DOCKERCODE))
                return DOCKER;

            return ONPREM;
        }


        public static bool IsCloudSaaSMachine()
        { 
            string installType = RegUtil.GetInstallType();

            return installType.Equals(AZURESAAS) || installType.Equals(AWSSAAS);
        }
    }

}
