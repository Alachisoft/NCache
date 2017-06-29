@echo off

for /f "skip=1 delims={}, " %%A in ('wmic nicconfig get ipaddress') do for /f "tokens=1" %%B in ("%%~A") do ( set "IP=%%~B" goto :break)
:break

ECHO WRITING IP TO SERVICE CONFIG ...
ECHO ================================
If not exist "%cd%\_.vbs" call :MakeReplace
CALL :FINDREPLACE "127.0.0.1" %IP% "%~1\bin\service\Alachisoft.NCache.Service.exe.config"
CALL :FINDREPLACE "127.0.0.1" %IP% "%~1\config\client.ncconf"
CALL :FINDREPLACE "127.0.0.1" %IP% "%~1\config\config.ncconf"
CALL :FINDREPLACE "127.0.0.1" %IP% "%~1\integrations\Memcached Wrapper\Gateway\bin\Alachisoft.NCache.Memcached.exe.config"

del "%cd%\_.vbs"
exit /b

:FindReplace <findstr> <replstr> <file>
set tmp="%cd%\tmp.txt"
for /f "tokens=*" %%a in ('dir /s /b /a-d /on %3') do (
  for /f "usebackq" %%b in (`Findstr /mic:"%~1" "%%a"`) do (
    <%%a cscript //nologo "%cd%\_.vbs" "%~1" "%~2">%tmp%
    if exist %tmp% move /Y %tmp% "%%~dpnxa">nul
  )
)
exit /b

:MakeReplace
>>"%cd%\_.vbs" echo with Wscript
>>"%cd%\_.vbs" echo set args=.arguments
>>"%cd%\_.vbs" echo .StdOut.Write _
>>"%cd%\_.vbs" echo Replace(.StdIn.ReadAll,args(0),args(1),1,-1,1)
>>"%cd%\_.vbs" echo end with
