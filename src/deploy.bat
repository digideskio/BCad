@echo off
set deploytype=desktop
set deploydir=%1
if [%deploydir%]==[] goto baddir
goto gooddir

:baddir
echo Usage: %0 destination
exit /b 1

:gooddir
set configuration=%2
if [%configuration%]==[] set configuration=Debug
set targetframework=%3
if [%targetframework%]==[] set targetframework=net45

if not exist "%deploydir%" mkdir "%deploydir%"
for /F "tokens=*" %%f in (%~dp0\deployment-files-%deploytype%.txt) do copy "%~dp0BCad\bin\%configuration%\%targetframework%\%%f" "%deploydir%"
