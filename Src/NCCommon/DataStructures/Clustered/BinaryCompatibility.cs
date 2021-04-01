// ==++==
// 
//   Copyright (c). 2015. Microsoft Corporation.
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// ==--==

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Alachisoft.NCache.Common.DataStructures.Clustered
{
    public static class BinaryCompatibility
    {
        private sealed class BinaryCompatibilityMap
        {
            internal bool TargetsAtLeast_Phone_V7_1;
            internal bool TargetsAtLeast_Phone_V8_0;
            internal bool TargetsAtLeast_Desktop_V4_5;
            internal bool TargetsAtLeast_Desktop_V4_5_1;
            internal bool TargetsAtLeast_Desktop_V4_5_2;
            internal bool TargetsAtLeast_Desktop_V4_5_3;
            internal bool TargetsAtLeast_Desktop_V4_5_4;
            internal bool TargetsAtLeast_Desktop_V5_0;
            internal bool TargetsAtLeast_Silverlight_V4;
            internal bool TargetsAtLeast_Silverlight_V5;
            internal bool TargetsAtLeast_Silverlight_V6;
            internal BinaryCompatibilityMap()
            {
                this.AddQuirksForFramework(BinaryCompatibility.AppWasBuiltForFramework, BinaryCompatibility.AppWasBuiltForVersion);
            }
            private void AddQuirksForFramework(TargetFrameworkId builtAgainstFramework, int buildAgainstVersion)
            {
                switch (builtAgainstFramework)
                {
                    case TargetFrameworkId.NotYetChecked:
                    case TargetFrameworkId.Unrecognized:
                    case TargetFrameworkId.Unspecified:
                    case TargetFrameworkId.Portable:
                        break;
                    case TargetFrameworkId.NetFramework:
                    case TargetFrameworkId.NetCore:
                        if (buildAgainstVersion >= 50000)
                        {
                            this.TargetsAtLeast_Desktop_V5_0 = true;
                        }
                        if (buildAgainstVersion >= 40504)
                        {
                            this.TargetsAtLeast_Desktop_V4_5_4 = true;
                        }
                        if (buildAgainstVersion >= 40503)
                        {
                            this.TargetsAtLeast_Desktop_V4_5_3 = true;
                        }
                        if (buildAgainstVersion >= 40502)
                        {
                            this.TargetsAtLeast_Desktop_V4_5_2 = true;
                        }
                        if (buildAgainstVersion >= 40501)
                        {
                            this.TargetsAtLeast_Desktop_V4_5_1 = true;
                        }
                        if (buildAgainstVersion >= 40500)
                        {
                            this.TargetsAtLeast_Desktop_V4_5 = true;
                            this.AddQuirksForFramework(TargetFrameworkId.Phone, 70100);
                            this.AddQuirksForFramework(TargetFrameworkId.Silverlight, 50000);
                            return;
                        }
                        break;
                    case TargetFrameworkId.Silverlight:
                        if (buildAgainstVersion >= 40000)
                        {
                            this.TargetsAtLeast_Silverlight_V4 = true;
                        }
                        if (buildAgainstVersion >= 50000)
                        {
                            this.TargetsAtLeast_Silverlight_V5 = true;
                        }
                        if (buildAgainstVersion >= 60000)
                        {
                            this.TargetsAtLeast_Silverlight_V6 = true;
                        }
                        break;
                    case TargetFrameworkId.Phone:
                        if (buildAgainstVersion >= 80000)
                        {
                            this.TargetsAtLeast_Phone_V8_0 = true;
                        }
                        if (buildAgainstVersion >= 710)
                        {
                            this.TargetsAtLeast_Phone_V7_1 = true;
                            return;
                        }
                        break;
                    default:
                        return;
                }
            }
        }
        private static TargetFrameworkId s_AppWasBuiltForFramework;
        private static int s_AppWasBuiltForVersion;
        private static readonly BinaryCompatibility.BinaryCompatibilityMap s_map;
        private const char c_componentSeparator = ',';
        private const char c_keyValueSeparator = '=';
        private const char c_versionValuePrefix = 'v';
        private const string c_versionKey = "Version";
        private const string c_profileKey = "Profile";
        internal static bool TargetsAtLeast_Phone_V7_1
        {
            get
            {
                return BinaryCompatibility.s_map.TargetsAtLeast_Phone_V7_1;
            }
        }
        internal static bool TargetsAtLeast_Phone_V8_0
        {
            get
            {
                return BinaryCompatibility.s_map.TargetsAtLeast_Phone_V8_0;
            }
        }
        internal static bool TargetsAtLeast_Desktop_V4_5
        {
            get
            {
                return BinaryCompatibility.s_map.TargetsAtLeast_Desktop_V4_5;
            }
        }
        internal static bool TargetsAtLeast_Desktop_V4_5_1
        {
            get
            {
                return BinaryCompatibility.s_map.TargetsAtLeast_Desktop_V4_5_1;
            }
        }
        internal static bool TargetsAtLeast_Desktop_V4_5_2
        {
            get
            {
                return BinaryCompatibility.s_map.TargetsAtLeast_Desktop_V4_5_2;
            }
        }
        internal static bool TargetsAtLeast_Desktop_V4_5_3
        {
            get
            {
                return BinaryCompatibility.s_map.TargetsAtLeast_Desktop_V4_5_3;
            }
        }
        internal static bool TargetsAtLeast_Desktop_V4_5_4
        {
            get
            {
                return BinaryCompatibility.s_map.TargetsAtLeast_Desktop_V4_5_4;
            }
        }
        internal static bool TargetsAtLeast_Desktop_V5_0
        {
            get
            {
                return BinaryCompatibility.s_map.TargetsAtLeast_Desktop_V5_0;
            }
        }

        internal static bool TargetsAtLeast_Silverlight_V4
        {

            get
            {
                return BinaryCompatibility.s_map.TargetsAtLeast_Silverlight_V4;
            }
        }

        internal static bool TargetsAtLeast_Silverlight_V5
        {

            get
            {
                return BinaryCompatibility.s_map.TargetsAtLeast_Silverlight_V5;
            }
        }

        internal static bool TargetsAtLeast_Silverlight_V6
        {

            get
            {
                return BinaryCompatibility.s_map.TargetsAtLeast_Silverlight_V6;
            }
        }

        internal static TargetFrameworkId AppWasBuiltForFramework
        {

            get
            {
                if (BinaryCompatibility.s_AppWasBuiltForFramework == TargetFrameworkId.NotYetChecked)
                {
                    BinaryCompatibility.ReadTargetFrameworkId();
                }
                return BinaryCompatibility.s_AppWasBuiltForFramework;
            }
        }

        internal static int AppWasBuiltForVersion
        {

            get
            {
                if (BinaryCompatibility.s_AppWasBuiltForFramework == TargetFrameworkId.NotYetChecked)
                {
                    BinaryCompatibility.ReadTargetFrameworkId();
                }
                return BinaryCompatibility.s_AppWasBuiltForVersion;
            }
        }
        static BinaryCompatibility()
        {
            BinaryCompatibility.s_map = new BinaryCompatibility.BinaryCompatibilityMap();
        }
        private static bool ParseTargetFrameworkMonikerIntoEnum(string targetFrameworkMoniker, out TargetFrameworkId targetFramework, out int targetFrameworkVersion)
        {
            targetFramework = TargetFrameworkId.NotYetChecked;
            targetFrameworkVersion = 0;
            string text = null;
            string text2 = null;
            BinaryCompatibility.ParseFrameworkName(targetFrameworkMoniker, out text, out targetFrameworkVersion, out text2);
            string a;
            if ((a = text) != null)
            {
                if (a == ".NETFramework")
                {
                    targetFramework = TargetFrameworkId.NetFramework;
                    return true;
                }
                if (a == ".NETPortable")
                {
                    targetFramework = TargetFrameworkId.Portable;
                    return true;
                }
                if (a == ".NETCore")
                {
                    targetFramework = TargetFrameworkId.NetCore;
                    return true;
                }
                if (a == "Silverlight")
                {
                    targetFramework = TargetFrameworkId.Silverlight;
                    if (string.IsNullOrEmpty(text2))
                    {
                        return true;
                    }
                    if (text2 == "WindowsPhone")
                    {
                        targetFramework = TargetFrameworkId.Phone;
                        targetFrameworkVersion = 70000;
                        return true;
                    }
                    if (text2 == "WindowsPhone71")
                    {
                        targetFramework = TargetFrameworkId.Phone;
                        targetFrameworkVersion = 70100;
                        return true;
                    }
                    if (text2 == "WindowsPhone8")
                    {
                        targetFramework = TargetFrameworkId.Phone;
                        targetFrameworkVersion = 80000;
                        return true;
                    }
                    if (text2.StartsWith("WindowsPhone", StringComparison.Ordinal))
                    {
                        targetFramework = TargetFrameworkId.Unrecognized;
                        targetFrameworkVersion = 70100;
                        return true;
                    }
                    targetFramework = TargetFrameworkId.Unrecognized;
                    return true;
                }
            }
            targetFramework = TargetFrameworkId.Unrecognized;
            return true;
        }
        private static void ParseFrameworkName(string frameworkName, out string identifier, out int version, out string profile)
        {
            if (frameworkName == null)
            {
                throw new ArgumentNullException("frameworkName");
            }
            if (frameworkName.Length == 0)
            {
                throw new ArgumentException(ResourceHelper
                    .GetResourceString("Argument_StringZeroLength"), "frameworkName");
            }
            string[] array = frameworkName.Split(new char[]
			{
				','
			});
            version = 0;
            if (array.Length < 2 || array.Length > 3)
            {
                throw new ArgumentException(ResourceHelper.GetResourceString("Argument_FrameworkNameTooShort"), "frameworkName");
            }
            identifier = array[0].Trim();
            if (identifier.Length == 0)
            {
                throw new ArgumentException(ResourceHelper.GetResourceString("Argument_FrameworkNameInvalid"), "frameworkName");
            }
            bool flag = false;
            profile = null;
            for (int i = 1; i < array.Length; i++)
            {
                string[] array2 = array[i].Split(new char[]
				{
					'='
				});
                if (array2.Length != 2)
                {
                    throw new ArgumentException(ResourceHelper.GetResourceString("SR.Argument_FrameworkNameInvalid"), "frameworkName");
                }
                string text = array2[0].Trim();
                string text2 = array2[1].Trim();
                if (text.Equals("Version", StringComparison.OrdinalIgnoreCase))
                {
                    flag = true;
                    if (text2.Length > 0 && (text2[0] == 'v' || text2[0] == 'V'))
                    {
                        text2 = text2.Substring(1);
                    }
                    Version version2 = new Version(text2);
                    version = version2.Major * 10000;
                    if (version2.Minor > 0)
                    {
                        version += version2.Minor * 100;
                    }
                    if (version2.Build > 0)
                    {
                        version += version2.Build;
                    }
                }
                else
                {
                    if (!text.Equals("Profile", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ArgumentException(ResourceHelper.GetResourceString("Argument_FrameworkNameInvalid"), "frameworkName");
                    }
                    if (!string.IsNullOrEmpty(text2))
                    {
                        profile = text2;
                    }
                }
            }
            if (!flag)
            {
                throw new ArgumentException(ResourceHelper.GetResourceString("Argument_FrameworkNameMissingVersion"), "frameworkName");
            }
        }



        private static void ReadTargetFrameworkId()
        {
            string targetFrameworkName = string.Empty;
            AssemblyName[] assNames = Assembly.GetExecutingAssembly().GetReferencedAssemblies();
            foreach (AssemblyName name in assNames)
                if (name.Name == "mscorlib")
                {
                    targetFrameworkName = name.Version.ToString();
                }
            int num = 0;
            TargetFrameworkId targetFrameworkId;
            if (targetFrameworkName == null)
            {
                targetFrameworkId = TargetFrameworkId.Unspecified;
            }
            else
            {
                if (!BinaryCompatibility.ParseTargetFrameworkMonikerIntoEnum(targetFrameworkName, out targetFrameworkId, out num))
                {
                    targetFrameworkId = TargetFrameworkId.Unrecognized;
                }
            }
            BinaryCompatibility.s_AppWasBuiltForFramework = targetFrameworkId;
            BinaryCompatibility.s_AppWasBuiltForVersion = num;
        }
    }
}
