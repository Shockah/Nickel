setlocal EnableExtensions EnableDelayedExpansion
pushd .

set /p Version=<"..\Version.txt"

rmdir /q /s "..\_Publish\linux-x64"
call Publish-Any.bat linux-x64 Nickel
mkdir "..\_Publish\linux-x64\Nickel\ModLibrary"

pushd "..\NickelLinuxLauncher"
dotnet publish NickelLinuxLauncher.csproj -c Release -r linux-x64 -p:PublishReadyToRun=false -p:TieredCompilation=false -p:PublishSingleFile=false -p:OutDir="..\_Publish\linux-x64\Nickel" --self-contained
popd

cd "..\_Publish\linux-x64"
tar.exe -a -cf "..\Nickel-!Version!-Linux.zip" "Nickel"

popd
