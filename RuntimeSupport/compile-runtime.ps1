param(
 [string]$GamePath='D:/SteamLibrary/steamapps/common/Nuclear Option',
 [string]$UnityEditorPath='C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor'
)
$ErrorActionPreference='Stop'
$destination=Join-Path $PSScriptRoot '../Releases/0.1.0/F5FlightSupport.dll'
New-Item -ItemType Directory -Path (Split-Path $destination) -Force | Out-Null
$compilerArguments=@('/target:library','/noconfig','/nostdlib+',('/out:'+$destination))
foreach($assembly in @('mscorlib','System','System.Core','netstandard','UnityEngine','UnityEngine.CoreModule','UnityEngine.PhysicsModule','Assembly-CSharp','Mirage','Rewired_Core')) {
 $reference=Join-Path $GamePath ('NuclearOption_Data/Managed/'+$assembly+'.dll')
 if(!(Test-Path -LiteralPath $reference)){throw "Missing local game reference $assembly"}
 $compilerArguments+=('/reference:'+$reference)
}
foreach($assembly in @('BepInEx','0Harmony')) {$compilerArguments+=('/reference:'+(Join-Path $GamePath ('BepInEx/core/'+$assembly+'.dll')))}
$compilerArguments+=(Join-Path $PSScriptRoot 'F5FlightSupport.cs')
& (Join-Path $UnityEditorPath 'Data/NetCoreRuntime/dotnet.exe') (Join-Path $UnityEditorPath 'Data/DotNetSdkRoslyn/csc.dll') @compilerArguments
if($LASTEXITCODE -ne 0){throw 'F5FlightSupport compilation failed'}
Get-FileHash -LiteralPath $destination -Algorithm SHA256

