@echo off
setlocal
echo Building ChromiumCrispVideoPatcher.exe...
set CSC="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist %CSC% (
    echo Error: .NET Framework 4.0 csc.exe not found!
    exit /b 1
)

%CSC% /target:winexe /out:"%~dp0ChromiumCrispVideoPatcher.exe" /win32manifest:"%~dp0src\app.manifest" /optimize+ /reference:System.dll,System.Core.dll,System.Drawing.dll,System.Windows.Forms.dll,Microsoft.CSharp.dll "%~dp0src\Program.cs"

if %ERRORLEVEL% EQU 0 (
    echo [SUCCESS] Built: %~dp0ChromiumCrispVideoPatcher.exe
) else (
    echo [ERROR] Build failed!
)
endlocal
