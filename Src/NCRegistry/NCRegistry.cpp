// Copyright (c) 2018 Alachisoft
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
// limitations under the License

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
