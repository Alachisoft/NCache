#include "StdAfx.h"
#include ".\regcommon.h"
#include <stdlib.h>
#include <Iphlpapi.h>

#pragma comment(lib, "Iphlpapi.lib")
#pragma warning(disable:4309)


//////////////////////////////////////////////
//
// Registry Helper functions.
//
namespace RegUtil
{
	static HKEY			g_AppKeyHive = HKEY_LOCAL_MACHINE;
	static string		g_AppKeyName = _T("Software\\AlachiSoft\\NCache");

	bool KeyExists(HKEY hRootKey, LPCTSTR subKey)
	{
		HKEY hKey;
#if defined(WIN64) 
		if(ERROR_SUCCESS != RegOpenKeyEx(hRootKey, subKey,0,KEY_READ|KEY_WOW64_64KEY, &hKey) )
		{
			return false;
		}
#else
	if(ERROR_SUCCESS != RegOpenKeyEx(hRootKey, subKey,0,KEY_READ, &hKey) )
		{
			return false;
		}
#endif 
		RegCloseKey(hKey);
		return true;
	}

	////////////////////////////////////////////////////////////////////////
	////
	//// Sets a string value into registry
	////
	bool SetRegValue(HKEY hRootKey, LPCTSTR subKey, LPCTSTR keyName, LPCTSTR keyValue,short prodId)
	{
		HKEY hKeyResult;
		DWORD dwDisposition;
#if defined(WIN64)||defined(NCWOW64)
		if (RegCreateKeyEx( hRootKey, subKey, 0, NULL, REG_OPTION_NON_VOLATILE,
							KEY_WRITE|KEY_WOW64_64KEY, NULL, &hKeyResult, &dwDisposition) != ERROR_SUCCESS)
		{
			return false;
		}
#else
		if (RegCreateKeyEx( hRootKey, subKey, 0, NULL, REG_OPTION_NON_VOLATILE,
							KEY_WRITE, NULL, &hKeyResult, &dwDisposition) != ERROR_SUCCESS)
		{
			return false;
		}
#endif
		DWORD dataLength = _tcslen(keyValue) * sizeof(TCHAR);
		if (RegSetValueEx( hKeyResult, keyName, 0, REG_SZ, (const LPBYTE)(LPCTSTR) keyValue, dataLength) != ERROR_SUCCESS)
		{
			RegCloseKey(hKeyResult);
			return false;
		}

		RegCloseKey(hKeyResult);
		return true;
	}

	////////////////////////////////////////////////////////////////////////
	////
	//// Sets a long value into registry
	////
	bool SetRegValue(HKEY hRootKey, LPCTSTR subKey, LPCTSTR keyName, DWORD keyValue, short prodId)
	{
		HKEY hKeyResult;
		DWORD dwDisposition;
#if defined(WIN64)||defined(NCWOW64)
		if (RegCreateKeyEx( hRootKey, subKey, 0, NULL, REG_OPTION_NON_VOLATILE,
							KEY_WRITE|KEY_WOW64_64KEY, NULL, &hKeyResult, &dwDisposition) != ERROR_SUCCESS)
		{
			return false;
		}
#else
		if (RegCreateKeyEx( hRootKey, subKey, 0, NULL, REG_OPTION_NON_VOLATILE,
							KEY_WRITE, NULL, &hKeyResult, &dwDisposition) != ERROR_SUCCESS)
		{
			return false;
		}

#endif
		DWORD dataLength = sizeof(keyValue);
		if (RegSetValueEx( hKeyResult, keyName, 0, REG_DWORD, (CONST BYTE*)&keyValue, dataLength) != ERROR_SUCCESS)
		{
			RegCloseKey(hKeyResult);
			return false;
		}

		RegCloseKey(hKeyResult);
		return true;
	}

	//////////////////////////////////////////////////////////////////////
	//
	// Returns a string registry key value, empty when failure
	//
	bool GetRegValue(HKEY root, LPCTSTR keyPath, LPCTSTR keyName, string& val, short prodId)
	{
		long retCode;
		HKEY hKeyResult;
		DWORD  dwType;
		DWORD  buffSize=0;

#if defined(WIN64)||defined(NCWOW64)
		
		retCode = RegOpenKeyEx(root, keyPath, 0, KEY_READ|KEY_WOW64_64KEY, &hKeyResult);
#else
		retCode = RegOpenKeyEx(root, keyPath, 0, KEY_READ, &hKeyResult);
#endif
		
		
		if (retCode != ERROR_SUCCESS)
		{
			return false;
		}

		retCode = RegQueryValueEx(hKeyResult, keyName, NULL, &dwType, NULL, &buffSize);
		if ((retCode != ERROR_SUCCESS))
		{
			RegCloseKey(hKeyResult);
			return false;
		}

		if (dwType == REG_DWORD)
		{
			DWORD regData;
			retCode = RegQueryValueEx(hKeyResult, keyName, NULL, &dwType, (LPBYTE) &regData, &buffSize);
			RegCloseKey(hKeyResult);
			if (retCode == ERROR_SUCCESS)
			{
				TCHAR data[10];
				_ltot(regData, data, 10);
				val = data;
			}
		}
		else
		{
			if(buffSize != 0)
			{
				TCHAR *pRegData = new TCHAR[buffSize];
				retCode = RegQueryValueEx(hKeyResult, keyName, NULL, &dwType,(LPBYTE)pRegData, &buffSize);
				RegCloseKey(hKeyResult);
				if (retCode == ERROR_SUCCESS)
				{
					val = pRegData;
				}
				delete [] pRegData;
			}
		}
		return true;
	}


	//////////////////////////////////////////////////////////////////////
	//
	// Returns a string containing all multiple regesitry enteries under a section
	// All keys are concatinated and returned back as a single string. 
	//
	bool GetRegKeys(HKEY root, LPCTSTR keyPath, LPCTSTR keyName, string& val, short prodId) 
	{
		long retCodeA,retCodeB;
		HKEY hKeyResult;
		DWORD  dwType;
		DWORD  buffSize=0;
		DWORD i, retCode;
		DWORD dwCurIdx = 0;		
		DWORD dwDataLen = MAX_VALUE_NAME;
		DWORD dwData;
		string token = ":";
		string allKeys = "";

#if defined(WIN64)||defined(NCWOW64)//[Asif Imam]
		
		retCodeA = RegOpenKeyEx(root, keyPath, 0, KEY_READ|KEY_WOW64_64KEY, &hKeyResult);
#else
		retCodeA = RegOpenKeyEx(root, keyPath, 0, KEY_READ, &hKeyResult);
#endif		
		
		if (retCodeA == ERROR_SUCCESS)
		{
			do
			 {
				 TCHAR *strValue = new TCHAR[MAX_VALUE_NAME];
				 strValue[0] = '/0';
				 DWORD dwValLen = MAX_VALUE_NAME;
				 retCodeB = RegEnumValue(hKeyResult,
										 dwCurIdx,
										 strValue,
										 &dwValLen,
										 0,
										 &dwType,
										 NULL,
										 NULL);
					if (retCodeB == ERROR_SUCCESS)
					{
						string temp;
						temp = strValue;
						allKeys = allKeys + temp;
						allKeys = allKeys + token;
					}
					delete []strValue;
					dwCurIdx = dwCurIdx + 1;
				} while (retCodeB == ERROR_SUCCESS);				 
				
			val = allKeys;
			RegCloseKey(hKeyResult);
		}		
		return true;
	}
/***********************************
 *
 ***********************************/
	string GetRegBase(short prodId)
	{
		if (prodId == 0)
		{
			g_AppKeyName = _T("Software\\AlachiSoft\\NCache");
		}
		return g_AppKeyName;
	}

	void SetRegBase(LPCTSTR szKeyName, HKEY rootKey)
	{
		g_AppKeyName = szKeyName;
		g_AppKeyHive = rootKey;
	}

	string GetString (LPCTSTR szSection, LPCTSTR szKey,LPCTSTR szDefault, short prodId)
	{
		string szKeyName;

		if (szSection != NULL)
		{
			szKeyName = GetRegBase (prodId) + string(_T("\\")) + szSection;
		}
		else
		{
			szKeyName = GetRegBase(prodId);
		}
		
		string  retVal;
		if(!GetRegValue (g_AppKeyHive, szKeyName.c_str(), szKey, retVal,prodId))
			return szDefault;
		return retVal;
	}

	string GetKeys (LPCTSTR szSection, LPCTSTR szKey,LPCTSTR szDefault, short prodId)
	{
		string szKeyName;		
		
		if (szSection != NULL)
		{
			szKeyName = GetRegBase (prodId) + string(_T("\\")) + szSection;
		}
		else
		{
			szKeyName = GetRegBase(prodId);
		}
		
		string  retVal;
		if(!GetRegKeys (g_AppKeyHive, szKeyName.c_str(), szKey, retVal, prodId))
			return szDefault;
		return retVal;
	}	
	
	bool SetString (LPCTSTR szSection, LPCTSTR szKey,LPCTSTR szValue, short prodId)
	{
		string szKeyName = GetRegBase (prodId) + string(_T("\\")) + szSection;
		return SetRegValue(g_AppKeyHive, szKeyName.c_str(), szKey, szValue, prodId);
	}

	long GetInt (LPCTSTR szSection, LPCTSTR szKey,long nDefault, short prodId)
	{
		string szKeyName = GetRegBase (prodId) + string(_T("\\")) + szSection;
		string  retVal;
		if(!GetRegValue (g_AppKeyHive, szKeyName.c_str(),szKey, retVal,prodId))
			return nDefault;
		return _ttol(retVal.c_str());
	}

	bool SetInt (LPCTSTR szSection, LPCTSTR szKey,long nValue, short prodId)
	{
		string szKeyName = GetRegBase (prodId) + string(_T("\\")) + szSection;
		return SetRegValue(g_AppKeyHive, szKeyName.c_str(), szKey, nValue, prodId);
	}
		
};
