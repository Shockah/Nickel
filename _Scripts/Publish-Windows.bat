setlocal EnableExtensions EnableDelayedExpansion
pushd .

set /p Version=<"..\Version.txt"

call Publish-Any.bat win-x64
mkdir "..\_Publish\DotNet\Nickel\ModLibrary"

cd "..\_Publish\DotNet"
tar.exe -a -cf "..\Nickel !Version!.zip" "Nickel"

popd