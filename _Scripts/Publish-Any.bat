setlocal EnableExtensions EnableDelayedExpansion
pushd .

cd ..
mkdir "_Publish\%1\%2"

pushd NickelLauncher
dotnet publish NickelLauncher.csproj -c Release -r %1 -p:PublishReadyToRun=false -p:TieredCompilation=false -p:PublishSingleFile=false -p:UseReferenceAssembly=true -p:OutDir="..\_Publish\%1\%2" --self-contained
popd

call :BuildInternalMod %1 %2 Nickel.Bugfixes
call :BuildInternalMod %1 %2 Nickel.Essentials
call :BuildInternalMod %1 %2 Nickel.Legacy
call :BuildInternalMod %1 %2 Nickel.ModSettings
call :BuildInternalMod %1 %2 Nickel.UpdateChecks
call :BuildInternalMod %1 %2 Nickel.UpdateChecks.GitHub
call :BuildInternalMod %1 %2 Nickel.UpdateChecks.NexusMods
call :BuildInternalMod %1 %2 Nickel.UpdateChecks.UI

popd
goto :EOF



:BuildInternalMod
pushd %3
dotnet build %3.csproj -c Release -p:EnableDllReference=false -p:ModLoaderPath="..\_Publish\%1\%2" -p:ModDeployModsPath="..\_Publish\%1\%2\InternalModLibrary"
popd
goto :EOF

:EOF