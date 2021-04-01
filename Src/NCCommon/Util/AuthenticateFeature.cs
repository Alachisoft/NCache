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
using Alachisoft.NCache.Runtime.Exceptions;

namespace Alachisoft.NCache.Common.Util
{
    /// <summary>
    /// This class is used to authenticate feature available in installed editions
    /// For example; If only .net edition is installed then only java supported readThru, writeThru and cacheloader won't work
    /// similarly, if only java edition is installed then only .net based readThru, writeThru and cacheloader will not work
    /// If both edition's are installed then .net and java based readThru, writeThru and cacheloader will work.
    /// </summary>
    public sealed class AuthenticateFeature
    {
        private static InstallModes _dotNetInstallMode = InstallModes.Server;
        private static InstallModes _javaInstallMode = InstallModes.None;
        private const string DOTNET_INSTALL_MODE = "DotNetInstallMode";
        private const string JAVA_INSTALL_MODE = "JavaInstallMode";
        
        /// <summary>
        /// Set the java and .net editions are installed mode
        /// </summary>
        static AuthenticateFeature()
        {
            try
            {
                object dotNetInstallMode = RegHelper.GetRegValue(RegHelper.ROOT_KEY, DOTNET_INSTALL_MODE, 0);
                object javaInstallMode = RegHelper.GetRegValue(RegHelper.ROOT_KEY, JAVA_INSTALL_MODE, 0);

                if (dotNetInstallMode != null)
                {
                    _dotNetInstallMode = (InstallModes)Convert.ToInt16(dotNetInstallMode);
                }

                if (javaInstallMode != null)
                {
                    _javaInstallMode = (InstallModes)Convert.ToInt16(javaInstallMode);
                }
            }
            catch (Exception exception){}

        }

        /// <summary>
        /// Verify whether java edition is installed or not
        /// </summary>
        /// <returns>
        /// true if java edition is installed otherwise; false
        /// </returns>
        public static bool IsJavaEnabled
        {
            get
            {
                if (_javaInstallMode == InstallModes.Client)
                    return true;
                if (_javaInstallMode == InstallModes.Developer)
                    return true;
                if (_javaInstallMode == InstallModes.Server)
                    return true;

                return false;
            }
            
        }

        /// <summary>
        /// Verify whether .net edition is installed or not
        /// </summary>
        /// <returns>
        /// true if .net edition is installed otherwise; false
        /// </returns>
        public static bool IsDotNetEnabled
        {
            get
            { 
                if (_dotNetInstallMode == InstallModes.Client)
                    return true;
                if (_dotNetInstallMode == InstallModes.Developer)
                    return true;
                if (_dotNetInstallMode == InstallModes.Server)
                    return true;

                return false;
            }            
        }

        /// <summary>
        /// Get Java Install Mode
        /// </summary>
        /// <returns>
        /// java install mode
        /// </returns>
        public static InstallModes JavaInstallMode
        {
            get { return _javaInstallMode; }
        }

        /// <summary>
        /// Get .net Install Mode
        /// </summary>
        /// <returns>
        /// .net install mode
        /// </returns>
        public static InstallModes DotNetInstallMode
        {
            get { return _dotNetInstallMode; }
        }

        public static void Authenticate(LanguageContext languageContext)
        {
            if (languageContext == LanguageContext.DOTNET && !IsDotNetEnabled)
            {
                throw new ConfigurationException(".net based readThru provider's are not supported in current installed NCache edition");
            }
            else if (languageContext == LanguageContext.JAVA && !IsJavaEnabled)
            {
                throw new ConfigurationException("java based readThru provider's are not supported in current installed NCache edition");
            }
        }
    }
}
