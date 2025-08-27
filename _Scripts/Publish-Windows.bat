setlocal EnableExtensions EnableDelayedExpansion
pushd .

set /p Version=<"..\Version.txt"

rmdir /q /s "..\_Publish\win-x64"
call Publish-Any.bat win-x64 Nickel
mkdir "..\_Publish\win-x64\Nickel\ModLibrary"

cd "..\_Publish\win-x64"
tar.exe -a -cf "..\Nickel-!Version!-Windows.zip" "Nickel"

popd