@echo off
SET version=ncache-oss-4.9-dotnetclient-setup.tar.gz
SET product=ncache-opensource-4.9-dotnetclient.tar.gz
SET tarlocation=..\..\..\Scripts\NetCore
SET setup-utilities=..\..\..\Scripts\NetCore\Installation-scripts\setup-utilities
	%1\7z.exe a -ttar  		%tarlocation%\temp.tar 			%tarlocation%\ncache
	%1\7z.exe a -tgzip 		%tarlocation%\%version% 		%tarlocation%\temp.tar
	DEL 					%tarlocation%\temp.tar 

	%1\7z.exe a -ttar  		%tarlocation%\temp.tar 		%tarlocation%\%version% %setup-utilities%\install %setup-utilities%\uninstall  %setup-utilities%\LICENSE  %setup-utilities%\README 
	%1\7z.exe a -tgzip		%tarlocation%\%product% 	%tarlocation%\temp.tar
	DEL 					%tarlocation%\%version%
	DEL 					%tarlocation%\temp.tar 
	RMDIR 					%tarlocation%\ncache 	/s /q





