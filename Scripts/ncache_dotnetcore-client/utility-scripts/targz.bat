@ECHO OFF

::_________________________________________::
::_____SETTING SOME VARIABLES FOR HELP_____::
::_________________________________________::

SET version=ncache-oss-4.9-dotnetclient-setup.tar.gz
SET product=ncache-opensource-4.9-dotnetclient.tar.gz

::_________________________________________::
::________CARRYING OUT SCRIPT WORK_________::
::_________________________________________::

"%~1\7z.exe" a -ttar  		"%TARLOCATION%\temp.tar" 			"%NCACHETEMPFOLDER%"
"%~1\7z.exe" a -tgzip 		"%TARLOCATION%\%version%" 			"%TARLOCATION%\temp.tar"
DEL 						"%TARLOCATION%\temp.tar"

"%~1\7z.exe" a -ttar  		"%TARLOCATION%\temp.tar" 		"%TARLOCATION%\%version%" "%SETUPUTILITIESPATH%\install" "%SETUPUTILITIESPATH%\uninstall" "%SETUPUTILITIESPATH%\LICENSE" "%SETUPUTILITIESPATH%\README"
"%~1\7z.exe" a -tgzip		"%TARLOCATION%\%product%"	 	"%TARLOCATION%\temp.tar"
DEL 						"%TARLOCATION%\%version%"
DEL 						"%TARLOCATION%\temp.tar"
RMDIR 						"%NCACHETEMPFOLDER%"			/s /q
