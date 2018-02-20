#include "StdAfx.h"
#include ".\regcommon.h"
#include <stdlib.h>
#include <Iphlpapi.h>

#pragma comment(lib, "Iphlpapi.lib")
#pragma warning(disable:4309)


//unsigned int g_DataOffset = 0x1008;

//
////////////////////////////////////////////////
////
//// Environment specific functions.
////
//namespace Environment
//{
//	const SYSTEM_INFO& GetSystemInfo()
//	{
//		static SYSTEM_INFO sysInfo = {0};
//		if(sysInfo.dwNumberOfProcessors == 0)
//		{
//			::GetSystemInfo(&sysInfo);
//		}
//		return sysInfo;
//	}
//
//	//////////////////////////////////////////////////////////////////////
//	//
//	// Fetches the MAC address and prints it
//	//
//	int GetAdaptersAddressList(string list[], int count)
//	{
//		static bool bLoaded = false;
//		static IP_ADAPTER_INFO adapterInfo[16] = {0};       // Allocate information
//
//		if(!bLoaded)
//		{
//			DWORD dwBufLen = sizeof(adapterInfo);
//			DWORD dwStatus = ::GetAdaptersInfo(adapterInfo, &dwBufLen);
//			if(dwStatus != ERROR_SUCCESS)
//				return -1;
//
//			bLoaded = true;
//		}
//
//		PIP_ADAPTER_INFO pAdapterInfo = adapterInfo;
//		// Contains pointer to
//		// current adapter info
//		int i = 0;
//		do
//		{
//			string address;
//			for(unsigned int j=0; j<pAdapterInfo->AddressLength; j++)
//			{
//				TCHAR v[3] = {0};
//				_stprintf(v, pAdapterInfo->Address[j] < 10 ? _T("0%x"):_T("%x"), pAdapterInfo->Address[j]);
//				address += v;
//			}
//			list[i++] = address;
//			pAdapterInfo = pAdapterInfo->Next;    // Progress through
//			count--;
//		}
//		while(count && pAdapterInfo);
//		return i;
//	}
//}
//
//
////////////////////////////////////////////////
////
//// Other un-classified functions.
////
//namespace Misc
//{
//	void GetInstallTime(const BYTE* data, long version, SYSTEMTIME* pSysTime,short prodId)
//	{
//		ZeroMemory(pSysTime, sizeof(SYSTEMTIME));
//		if(data != 0)
//		{
//			SYSTEMTIME *temp = ((SYSTEMTIME*)(data + g_DataOffset)) + version;
//			pSysTime->wYear			= temp->wYear;
//			pSysTime->wMonth		= temp->wMonth;
//			pSysTime->wDayOfWeek	= temp->wDayOfWeek; 
//			pSysTime->wDay			= temp->wDay;   
//			pSysTime->wMinute		= temp->wMinute;
//			pSysTime->wSecond		= temp->wSecond; //Read the activation status
//		}
//	}
//
//	//////////////////////////////////////////////////////////////////////
//	//
//	// Returns the number of times the evaluation period has been extended so far.
//	//
//	int	GetExtensionsUsed(short prodId)
//	{
//		string extCode = RegUtil::GetString(_T("UserInfo"), _T("ExtCode"),_T(""), prodId);
//		extCode = Crypto::EDecode(extCode.c_str());
//		int ext = 0;
//		if(extCode.size() > 3)
//		{
//			if(extCode.at(0) >= _T('0') && extCode.at(0) <= _T('9'))
//				ext = extCode.at(0) - _T('0');
//		}
//		return ext;
//	}
//
//	//////////////////////////////////////////////////////////////////////
//	//
//	// Returns the number of times the evaluation period has been extended so far.
//	//
//	string	GetAuthCode(short prodId)
//	{
//		return RegUtil::GetString(_T("UserInfo"), _T("AuthCode"),"",prodId);
//	}
//
//	string GetInstallCode(short prodId)
//	{
//		return RegUtil::GetString(NULL, _T("InstallCode"),"",prodId);
//	}
//	
//	// Verifies if the version timestamp is valid, i.e., installed!
//	bool IsValidVersionMark(const SYSTEMTIME *pSysTime, short prodId)
//	{
//		//SYSTEMTIME pSysTime;
//		//GetInstallTime(version, &pSysTime);
//		if(pSysTime->wYear < 2005 || pSysTime->wYear > 3500) return false;
//		if(pSysTime->wMonth > 12 || pSysTime->wDayOfWeek > 31) return false;
//		return true;
//	}
//}
//
//
//namespace FileUtil
//{
//	bool FileExists(LPCTSTR pszFilePath)
//	{
//		DWORD attribs = GetFileAttributes(pszFilePath);
//		if(attribs == 0xffffffff || attribs & FILE_ATTRIBUTE_DIRECTORY)
//			return false;
//		return true;
//	}
//
//	int ReadFile(LPCTSTR fileName, BYTE** data)
//	{
//		*data = 0;
//		HANDLE hfile = ::CreateFile(fileName, 
//				GENERIC_READ, FILE_SHARE_READ, 
//				0, OPEN_EXISTING, 0, NULL);
//		if(hfile ==INVALID_HANDLE_VALUE)
//			return -1;
//
//		// Try to obtain hFile's size 
//		DWORD dwSize = GetFileSize (hfile, NULL) ;
//		BYTE* buffer = new BYTE[dwSize];
//		DWORD nBytesRead;
//
//		BOOL bResult = ::ReadFile(hfile, (LPVOID)buffer, dwSize, &nBytesRead, NULL) ; 
//		if (!bResult ||  nBytesRead == 0 ) 
//		{ 
//			delete [] buffer;
//			CloseHandle(hfile);
//			return -1;
//		} 
//		CloseHandle(hfile);
//		
//		Crypto::EncryptDecryptBytes(buffer + g_DataOffset,dwSize-g_DataOffset);
//
//		*data = buffer;
//		return dwSize;
//	}
//
//}
//
//extern HANDLE g_hModule;

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
#if defined(WIN64) //[Asif Imam]
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
#if defined(WIN64)||defined(NCWOW64)//[Asif Imam]
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
#if defined(WIN64)||defined(NCWOW64)//[Asif Imam]
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

#if defined(WIN64)||defined(NCWOW64)//[Asif Imam]
		
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
	// All keys are concatinated and returned back as a single string. [Asif Imam] Aug08'08
	//
	bool GetRegKeys(HKEY root, LPCTSTR keyPath, LPCTSTR keyName, string& val, short prodId) //Added by [Asif Imam] Aug 08,08
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
		else if (prodId == 1)
		{
			g_AppKeyName = _T("Software\\AlachiSoft\\NWebCache");
		}
		else if (prodId == 2)
		{
			g_AppKeyName = _T("Software\\AlachiSoft\\StorageEdge"); //NCachePoint
		}
		else if (prodId == 3)
		{
			g_AppKeyName = _T("Software\\AlachiSoft\\TayzGrid"); 
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

	
	/////////////////////////////
	////

	//string GetUserString (LPCTSTR szSection, LPCTSTR szKey,LPCTSTR szDefault)
	//{
	//	string szKeyName = GetRegBase () + string(_T("\\")) + szSection;
	//	string  retVal;
	//	if(!GetRegValue (HKEY_CURRENT_USER, szKeyName.c_str(),szKey, retVal, prodId))
	//		return szDefault;
	//	return retVal;
	//}

	//bool SetUserString (LPCTSTR szSection, LPCTSTR szKey,LPCTSTR szValue)
	//{
	//	string szKeyName = GetRegBase () + string(_T("\\")) + szSection;
	//	return SetRegValue(HKEY_CURRENT_USER, szKeyName.c_str(), szKey, szValue);
	//}

	//long GetUserInt (LPCTSTR szSection, LPCTSTR szKey,long nDefault)
	//{
	//	string szKeyName = GetRegBase () + string(_T("\\")) + szSection;
	//	string  retVal;
	//	if(!GetRegValue (HKEY_CURRENT_USER, szKeyName.c_str(),szKey, retVal,prodId))
	//		return nDefault;
	//	return _ttol(retVal.c_str());
	//}

	//bool SetUserInt (LPCTSTR szSection, LPCTSTR szKey,long nValue)
	//{
	//	string szKeyName = GetRegBase () + string(_T("\\")) + szSection;
	//	return SetRegValue(HKEY_CURRENT_USER, szKeyName.c_str(), szKey, nValue);
	//}
};
