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
#if !NETCORE
using System;
using System.Drawing;
using System.Collections;
using System.Text;
using System.Management.Instrumentation;
using System.Configuration.Install;
using System.Management;
using System.ComponentModel;
using System.Runtime.InteropServices;

//[assembly: Instrumented("root\\Alachisoft")]
[assembly: Instrumented("root\\NCache")]
//Security settings ???  , "O:AOG:DAD:(A;;RP WP CC DC LC SW RC WD WO CA FA GA;;;WD)"
//

namespace Alachisoft.NCache.Instrumentation
{

[System.ComponentModel.RunInstaller(true)]
public class MyInstaller : DefaultManagementProjectInstaller 
{
    private readonly string namespaceName =
            "Alachisoft_NCache_Instrumentation";

    public MyInstaller()
    {
        Run();
    }

    public void Run()
    {
        IntPtr stringSecurityDescriptorPtr = IntPtr.Zero;
        IntPtr securityDescriptorPtr = IntPtr.Zero;
        int stringSecurityDescriptorSize = 0;
        int securityDescriptorSize = 0;

        try
        {
            // Create a test namespace
            this.CreateTestNamespace();

            // Retreive SD of a namespace
            ManagementClass systemSecurity =
                new ManagementClass("root/" +
                namespaceName + ":__SystemSecurity");
            ManagementBaseObject outParams =
                systemSecurity.InvokeMethod("GetSD",
                  null, null);
            if ((uint)outParams["ReturnValue"] != 0)
            {
                Console.WriteLine("GetSD returns an error: " +
                    outParams["ReturnValue"]);
                return;
            }

            // Convert SD to string SD
            this.ConvertSDtoStringSD((byte[])outParams["SD"],
                out stringSecurityDescriptorPtr,
                out stringSecurityDescriptorSize);
            string stringSecurityDescriptor =
                Marshal.PtrToStringAuto(
                stringSecurityDescriptorPtr);
            Console.WriteLine("Original string security " +
                "descriptor of the {0} namespace:",
                namespaceName);
            Console.WriteLine(stringSecurityDescriptor);

            // Grant all permissions to everyone Group
            stringSecurityDescriptor +=
                "(A;;CCDCLCSWRPWPRCWD;;;WD)";
           // stringSecurityDescriptor += 
            //    "(A;;RPWPCCDCLCSWRCWDWOCAFAGA;;;WD)";
            //Convert string SD to SD
            Console.WriteLine(
                "\nNew String Security Descriptor:");
            Console.WriteLine(stringSecurityDescriptor);
            this.ConvertStringSDtoSD(stringSecurityDescriptor,
                out securityDescriptorPtr,
                out securityDescriptorSize);
            byte[] securityDescriptor =
                new byte[securityDescriptorSize];
            Marshal.Copy(securityDescriptorPtr,
                securityDescriptor, 0, securityDescriptorSize);

            //Set the new SD for the namespace
            ManagementBaseObject inParams =
                systemSecurity.GetMethodParameters("SetSD");
            inParams["SD"] = securityDescriptor;
            outParams = systemSecurity.InvokeMethod("SetSD",
                inParams, null);
            if ((uint)outParams["ReturnValue"] != 0)
            {
                Console.WriteLine("SetSD returns error: " +
                    outParams["ReturnValue"]);
                return;
            }
            Console.WriteLine("\nNew string security descriptor");
             //   + " is set. Press Enter to exit.");
            //Console.ReadLine();
        }
        finally
        {
            // Free unmanaged memory
            if (securityDescriptorPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(securityDescriptorPtr);
                securityDescriptorPtr = IntPtr.Zero;
            }
            if (stringSecurityDescriptorPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(stringSecurityDescriptorPtr);
                stringSecurityDescriptorPtr = IntPtr.Zero;
            }
            //this.DeleteTestNamespace();
        }
    }

    public void ConvertSDtoStringSD(byte[] securityDescriptor,
        out IntPtr stringSecurityDescriptorPtr,
        out int stringSecurityDescriptorSize)
    {
        bool result =
            ConvertSecurityDescriptorToStringSecurityDescriptor(
            securityDescriptor,
            1,
            SecurityInformation.DACL_SECURITY_INFORMATION |
            SecurityInformation.GROUP_SECURITY_INFORMATION |
            SecurityInformation.OWNER_SECURITY_INFORMATION |
            SecurityInformation.SACL_SECURITY_INFORMATION,
            out stringSecurityDescriptorPtr,
            out stringSecurityDescriptorSize);
        if (!result)
        {
            Console.WriteLine("Fail to convert" +
                " SD to string SD:");
            throw new Win32Exception(
                Marshal.GetLastWin32Error());
        }
    }

    public void ConvertStringSDtoSD(
        string stringSecurityDescriptor,
        out IntPtr securityDescriptorPtr,
        out int securityDescriptorSize)
    {
        bool result =
            ConvertStringSecurityDescriptorToSecurityDescriptor(
            stringSecurityDescriptor,
            1,
            out securityDescriptorPtr,
            out securityDescriptorSize);
        if (!result)
        {
            Console.WriteLine(
                "Fail to convert string SD to SD:");
            throw new Win32Exception(
                Marshal.GetLastWin32Error());
        }
    }

    private enum SecurityInformation : uint
    {
        OWNER_SECURITY_INFORMATION = 0x00000001,
        GROUP_SECURITY_INFORMATION = 0x00000002,
        DACL_SECURITY_INFORMATION = 0x00000004,
        SACL_SECURITY_INFORMATION = 0x00000008,
        PROTECTED_DACL_SECURITY_INFORMATION = 0x80000000,
        PROTECTED_SACL_SECURITY_INFORMATION = 0x40000000,
        UNPROTECTED_DACL_SECURITY_INFORMATION = 0x20000000,
        UNPROTECTED_SACL_SECURITY_INFORMATION = 0x10000000,
    };

    [DllImport("Advapi32.dll", CharSet = CharSet.Auto,
        SetLastError = true, ExactSpelling = false)]
    private static extern bool
        ConvertSecurityDescriptorToStringSecurityDescriptor(
        [In] byte[] SecurityDescriptor,
        [In] int RequestedStringSDRevision,
        [In] SecurityInformation SecurityInformation,
        [Out] out IntPtr StringSecurityDescriptor,
        [Out] out int StringSecurityDescriptorLen
    );


    [DllImport("Advapi32.dll", CharSet = CharSet.Auto,
        SetLastError = true, ExactSpelling = false)]
    private static extern bool
        ConvertStringSecurityDescriptorToSecurityDescriptor(
        [In] string StringSecurityDescriptor,
        [In] uint StringSDRevision,
        [Out] out IntPtr SecurityDescriptor,
        [Out] out int SecurityDescriptorSize
    );

    private void CreateTestNamespace()
    {
        ManagementClass rootNamespace =
            new ManagementClass("root:__namespace");
        ManagementObject testNamespace =
            rootNamespace.CreateInstance();
        testNamespace["Name"] = namespaceName;
        testNamespace.Put();
    }

private void DeleteTestNamespace()
{
    ManagementObject testNamespace =
    new ManagementObject("root:__namespace.Name='"
        + namespaceName + "'");
    try
    {
        testNamespace.Get();
        testNamespace.Delete();
    }
    catch (ManagementException e)
    {
        if (e.ErrorCode == ManagementStatus.NotFound)
            return;
    }
}
}

}
#endif