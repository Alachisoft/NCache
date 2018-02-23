@ECHO off
SET source=..\..\..\Scripts\NetCore\ncache
IF EXIST %source% (
RMDIR %source% /s /q
) 

MKDIR %source%
MKDIR %source%\bin
MKDIR %source%\bin\service
MKDIR %source%\bin\ncacheps
MKDIR %source%\config
MKDIR %source%\docs
MKDIR %source%\lib
MKDIR %source%\log-files
MKDIR %source%\log-files\ClientLogs
MKDIR %source%\integrations

