#pragma once
#include <string>
using namespace std;
//
//namespace Environment
//{
//	const SYSTEM_INFO&	GetSystemInfo();
//	int					GetAdaptersAddressList(string list[], int count);
//}
////////////////////////////////////////////////
////
//// Encryption specific functions.
////
//namespace Crypto
//{
//	void	EncryptDecryptBytes(BYTE *pBytes, int nCount);
//	std::string	EDecode(const char* ptr);
//	std::string EEncode(const char* prt, int count);
//}
//
////////////////////////////////////////////////
////
//// Other un-classified functions.
////
//namespace Misc
//{
//	void	GetInstallTime(const BYTE* data, long version, SYSTEMTIME* pSysTime,short prodId);
//	bool	IsValidVersionMark(const SYSTEMTIME *pSysTime,short prodId);
//	int		GetExtensionsUsed(short prodId);
//	string	GetAuthCode(short prodId);	
//	string  GetInstallCode(short prodId);
//}
//
//
////////////////////////////////////////////////
////
//// File specific functions.
////
//namespace FileUtil
//{
//	bool	FileExists(LPCTSTR pszFilePath);
//	int		ReadFile(LPCTSTR fileName, BYTE** data);
//}

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
	
	string		QueryKey(HKEY hKey); //Added by [Asif Imam] Aug 08,08

	bool		SetRegValue(HKEY hRootKey, LPCTSTR subKey, LPCTSTR keyName, LPCTSTR keyValue,short prodId);
	bool		SetRegValue(HKEY hRootKey, LPCTSTR subKey, LPCTSTR keyName, DWORD keyValue,short prodId);
	bool		GetRegValue(HKEY root, LPCTSTR keyPath, LPCTSTR keyName, string& val,short prodId);
	bool		GetRegKeys(HKEY root, LPCTSTR keyPath, LPCTSTR keyName, string& val,short prodId); //Added by [Asif Imam] Aug 08,08

	// Uses AppHive
	string		GetString (LPCTSTR szSection, LPCTSTR szKey,LPCTSTR szDefault = _T(""),short prodId=0);
	string		GetKeys (LPCTSTR szSection, LPCTSTR szKey,LPCTSTR szDefault = _T(""),short prodId=0); //Added by [Asif Imam] Aug 08,08
	bool		SetString (LPCTSTR szSection, LPCTSTR szKey,LPCTSTR szValue,short prodId);
	long		GetInt (LPCTSTR szSection, LPCTSTR szKey,long nDefault = 0,short prodId=0);
	bool		SetInt (LPCTSTR szSection, LPCTSTR szKey,long nValue,short prodId);

	// Uses HKEY_CURRENT_USER instead of AppHive
	string		GetUserString (LPCTSTR szSection, LPCTSTR szKey,LPCTSTR szDefault = _T(""),short prodId=0);
	bool		SetUserString (LPCTSTR szSection, LPCTSTR szKey,LPCTSTR szValue,short prodId);
	long		GetUserInt (LPCTSTR szSection, LPCTSTR szKey,long nDefault = 0,short prodId=0);
	bool		SetUserInt (LPCTSTR szSection, LPCTSTR szKey,long nValue,short prodId);
};
