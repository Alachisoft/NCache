// Copyright (c) 2017 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Alachisoft.NCache.Runtime.Exceptions;

using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Tools.Common;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Config;
using Alachisoft.NCache.ServiceControl;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Management.ServiceControl;

namespace Alachisoft.NCache.Tools.VerifyNCacheLicence
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                VerifyNCacheLicenceTool.Run(args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    public class VerifyNCacheLicenceParam : Alachisoft.NCache.Tools.Common.CommandLineParamsBase
    {
        public VerifyNCacheLicenceParam()
        {
        }
        private bool _logo = false;

        [ArgumentAttribute(@"/G", @"/logo", false)]
        public bool Logo
        {
            get { return _logo; }
            set { _logo = value; }
        }
    }

    public class VerifyNCacheLicenceTool
    {
        static private VerifyNCacheLicenceParam cParam = new VerifyNCacheLicenceParam();

        static public void Run(string[] args)
        {
            try
            {
                object param = new VerifyNCacheLicenceParam();
                CommandLineArgumentParser.CommandLineParser(ref param, args);
                cParam = (VerifyNCacheLicenceParam)param;

                if (!cParam.Logo)
                {
                    AssemblyUsage.PrintLogo(cParam.IsLogo);
                }
                if (cParam.IsUsage)
                {
                    AssemblyUsage.PrintUsage();
                    return;
                }


                PrintUserInfo();
                Console.WriteLine();

                Console.WriteLine("Edition Installed: NCache 4.6 OpenSouce Edition.\n");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return;
            }
        }

        private static void PrintUserInfo()
        {

            string USER_KEY = RegHelper.ROOT_KEY + @"\UserInfo";
            string firstName = (string)RegHelper.GetRegValue(USER_KEY, "firstname", 0);
            string lastName = (string)RegHelper.GetRegValue(USER_KEY, "lastname", 0);
            string company = (string)RegHelper.GetRegValue(USER_KEY, "company", 0);
            string email = (string)RegHelper.GetRegValue(USER_KEY, "email", 0);

            Console.WriteLine("This product is registered to \nUser\t:\t" + firstName + " " + lastName + "\nEmail\t:\t" + email + "\nCompany\t:\t" + company);

        }
    }
}
