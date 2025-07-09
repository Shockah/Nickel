setlocal EnableExtensions EnableDelayedExpansion
pushd .

cd ..
set /p Version=<Version.txt
mkdir "_Publish\DotNet\Nickel"

pushd NickelLauncher
dotnet publish NickelLauncher.csproj -c Release -r %1 -p:PublishReadyToRun=false -p:TieredCompilation=false -p:PublishSingleFile=false -p:UseReferenceAssembly=true -p:OutDir="..\_Publish\DotNet\Nickel" --self-contained
popd

call :BuildInternalMod Nickel.Bugfixes
call :BuildInternalMod Nickel.Essentials
call :BuildInternalMod Nickel.Legacy
call :BuildInternalMod Nickel.ModSettings
call :BuildInternalMod Nickel.UpdateChecks
call :BuildInternalMod Nickel.UpdateChecks.GitHub
call :BuildInternalMod Nickel.UpdateChecks.NexusMods
call :BuildInternalMod Nickel.UpdateChecks.UI

popd
goto :EOF



:BuildInternalMod
pushd %1
dotnet build %1.csproj -c Release -p:EnableDllReference=false -p:ModLoaderPath="..\_Publish\DotNet\Nickel" -p:ModDeployModsPath="..\_Publish\DotNet\Nickel\InternalModLibrary"
popd
goto :EOF

:EOF