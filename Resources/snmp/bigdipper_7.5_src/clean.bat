set msBuildDir=%WINDIR%\Microsoft.NET\Framework\v4.0.30319
call %MSBuildDir%\msbuild Build.proj /t:clean
@IF %ERRORLEVEL% NEQ 0 PAUSE