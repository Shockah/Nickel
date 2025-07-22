setlocal EnableExtensions EnableDelayedExpansion
pushd .

set /p Version=<"..\Version.txt"

rmdir /q /s "..\_Publish\osx-x64"
xcopy /E "..\_Mac\Nickel.app" "..\_Publish\osx-x64\Nickel.app\"
call Publish-Any.bat osx-x64 Nickel.app\Contents\MacOS

pushd "..\NickelMacLauncher"
dotnet publish NickelMacLauncher.csproj -c Release -r osx-x64 -p:PublishReadyToRun=false -p:TieredCompilation=false -p:PublishSingleFile=false -p:OutDir="..\_Publish\osx-x64\Nickel.app\Contents\MacOS" --self-contained
popd

cd "..\_Publish\osx-x64"
tar.exe -a -cf "..\Nickel-!Version!-Mac.zip" "Nickel.app"
..\..\_Scripts\zip_exec\zip_exec.exe "..\Nickel-!Version!-Mac.zip" Nickel.app/Contents/MacOS/Nickel
..\..\_Scripts\zip_exec\zip_exec.exe "..\Nickel-!Version!-Mac.zip" Nickel.app/Contents/MacOS/NickelLauncher
..\..\_Scripts\zip_exec\zip_exec.exe "..\Nickel-!Version!-Mac.zip" Nickel.app/Contents/MacOS/NickelMacLauncher

popd