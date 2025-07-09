setlocal EnableExtensions EnableDelayedExpansion
pushd .

set /p Version=<"..\Version.txt"

rmdir /q /s "..\_Publish\osx-x64"
xcopy /E "..\_Mac\Nickel.app" "..\_Publish\osx-x64\Nickel.app\"
call Publish-Any.bat osx-x64 Nickel.app\Contents\MacOS

cd "..\_Publish\osx-x64"
tar.exe -a -cf "..\Nickel-!Version!-Mac.zip" "Nickel.app"
%SCRIPTPATH%\zip_exec\zip_exec.exe "..\Nickel-!Version!-Mac.zip" Nickel.app/Contents/MacOS/Nickel
%SCRIPTPATH%\zip_exec\zip_exec.exe "..\Nickel-!Version!-Mac.zip" Nickel.app/Contents/MacOS/NickelLauncher

popd