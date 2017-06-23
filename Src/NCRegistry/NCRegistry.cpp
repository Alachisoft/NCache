// StubDll.cpp : Defines the entry point for the DLL application.
//

#include "stdafx.h"
#include "regcommon.h"
#include "resource.h"
#include <tchar.h>
#include <string>
using namespace std;


HANDLE g_hModule;

BOOL APIENTRY DllMain( HANDLE hModule, 
                       DWORD  ul_reason_for_call, 
                       LPVOID lpReserved
					 )
{
	g_hModule = hModule;
    return (int)1;
}

//extern unsigned int g_DataOffset;



extern "C"
{
	__declspec (dllexport) void GetRegVal(LPTSTR regVlue, LPCTSTR szSection, LPCTSTR szKey,LPCTSTR szDefault, short prodId)
	{
		string regVal = RegUtil::GetString(szSection,szKey,szDefault,prodId);
		_tcscpy(regVlue, regVal.c_str());
	}

	__declspec (dllexport) void GetRegKeys(LPTSTR regKeyes, LPCTSTR szSection, LPCTSTR szKey,LPCTSTR szDefault, short prodId)
	{
		string regKeys = RegUtil::GetKeys(szSection,szKey,szDefault,prodId);
		_tcscpy(regKeyes, regKeys.c_str());
	}
	
	__declspec (dllexport) bool SetRegVal(LPCTSTR szSection, LPCTSTR szKey,LPCTSTR szNewVal, short prodId)
	{
		return RegUtil::SetString(szSection,szKey,szNewVal,prodId);
	}

	__declspec (dllexport) bool SetRegValInt(LPCTSTR szSection, LPCTSTR szKey, long szNewVal, short prodId)
	{
		return RegUtil::SetInt(szSection,szKey,szNewVal,prodId);
	}
}
