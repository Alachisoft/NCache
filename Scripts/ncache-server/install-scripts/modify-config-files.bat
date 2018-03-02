@ECHO OFF

FOR /f "skip=1 delims={}, " %%A IN ('wmic nicconfig get ipaddress') DO FOR /f "tokens=1" %%B in ("%%~A") DO ( set "IP=%%~B" GOTO :break)
:break

ECHO WRITING IP TO SERVICE CONFIG ...
ECHO ================================
IF NOT EXIST "%cd%\_.vbs" CALL :MakeReplace
CALL :FINDREPLACE "127.0.0.1" %IP% "%~1\bin\service\Alachisoft.NCache.Service.exe.config"
CALL :FINDREPLACE "127.0.0.1" %IP% "%~1\config\client.ncconf"
CALL :FINDREPLACE "127.0.0.1" %IP% "%~1\config\config.ncconf"

DEL "%cd%\_.vbs"
EXIT /b

:FindReplace <findstr> <replstr> <file>
SET replaceTxt="%cd%\tmp.txt"
FOR /f "tokens=*" %%a in ('dir /s /b /a-d /on %3') DO (
  FOR /f "usebackq" %%b in (`Findstr /mic:"%~1" "%%a"`) DO (
    <%%a cscript //nologo "%cd%\_.vbs" "%~1" "%~2">%replaceTxt%
    IF EXIST %replaceTxt% move /Y %replaceTxt% "%%~dpnxa">nul
  )
)
EXIT /b

:MakeReplace
>>"%cd%\_.vbs" ECHO with Wscript
>>"%cd%\_.vbs" ECHO set args=.arguments
>>"%cd%\_.vbs" ECHO .StDOut.Write _
>>"%cd%\_.vbs" ECHO Replace(.StdIn.ReadAll,args(0),args(1),1,-1,1)
>>"%cd%\_.vbs" ECHO end with
