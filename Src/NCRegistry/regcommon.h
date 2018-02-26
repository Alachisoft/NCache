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

#pragma once
#include <string>
using namespace std;

//////////////////////////////////////////////
//
// Registry specific functions.
//
namespace RegUtil
{
	// get/set the base registry key, a short hand convenience used by other functions 
	// like GetString etc.
	//
	void		SetRegBase(LPCTSTR szKeyName, HKEY rootKey = HKEY_LOCAL_MACHINE);

	bool		KeyExists(HKEY hRootKey, LPCTSTR subKey);
	
	string		QueryKey(HKEY hKey); 

	bool		SetRegValue(HKEY hRootKey, LPCTSTR subKey, LPCTSTR keyName, LPCTSTR keyValue,short prodId);
	bool		SetRegValue(HKEY hRootKey, LPCTSTR subKey, LPCTSTR keyName, DWORD keyValue,short prodId);
	bool		GetRegValue(HKEY root, LPCTSTR keyPath, LPCTSTR keyName, string& val,short prodId);
	bool		GetRegKeys(HKEY root, LPCTSTR keyPath, LPCTSTR keyName, string& val,short prodId); 

	// Uses AppHive
	string		GetString (LPCTSTR szSection, LPCTSTR szKey,LPCTSTR szDefault = _T(""),short prodId=0);
	string		GetKeys (LPCTSTR szSection, LPCTSTR szKey,LPCTSTR szDefault = _T(""),short prodId=0); 
	bool		SetString (LPCTSTR szSection, LPCTSTR szKey,LPCTSTR szValue,short prodId);
	long		GetInt (LPCTSTR szSection, LPCTSTR szKey,long nDefault = 0,short prodId=0);
	bool		SetInt (LPCTSTR szSection, LPCTSTR szKey,long nValue,short prodId);

	// Uses HKEY_CURRENT_USER instead of AppHive
	string		GetUserString (LPCTSTR szSection, LPCTSTR szKey,LPCTSTR szDefault = _T(""),short prodId=0);
	bool		SetUserString (LPCTSTR szSection, LPCTSTR szKey,LPCTSTR szValue,short prodId);
	long		GetUserInt (LPCTSTR szSection, LPCTSTR szKey,long nDefault = 0,short prodId=0);
	bool		SetUserInt (LPCTSTR szSection, LPCTSTR szKey,long nValue,short prodId);
};
