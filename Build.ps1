param([Parameter(Mandatory=$true)][string]$ServerRoot)
$ErrorActionPreference = 'Stop'
$server = (Resolve-Path -LiteralPath $ServerRoot).Path
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$referenceRoot = Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Windows .NET Framework compiler missing / Compilateur Windows absent.' }
$references = @()
foreach ($name in @('mscorlib.dll','System.dll','System.Core.dll','System.Xml.dll')) {
    $path = Join-Path $referenceRoot $name
    if (-not (Test-Path -LiteralPath $path)) { throw 'Install the .NET Framework 4.7.2 targeting pack / Installer le targeting pack 4.7.2.' }
    $references += "/reference:$path"
}
$hostMif = Join-Path $server 'Content\Mods\ModLoader\Host\Mif.dll'
if (-not (Test-Path -LiteralPath $hostMif)) { throw 'Install EmpyrionModHost first / Installer EmpyrionModHost.' }
$managed = $null
foreach ($relative in @('DedicatedServer\EmpyrionDedicated_Data\Managed','EmpyrionDedicated_Data\Managed','Empyrion_Data\Managed')) {
    $candidate = Join-Path $server $relative
    if (Test-Path -LiteralPath (Join-Path $candidate 'ModApi.dll')) { $managed = $candidate; break }
}
if (-not $managed) { throw 'Game ModApi.dll missing / ModApi.dll du jeu absent.' }
$stateRefs = @()
foreach ($name in @('Mif.dll','ModApi.dll','UnityEngine.CoreModule.dll')) {
    $path = Join-Path $managed $name
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing game reference / Reference absente: $name" }
    $stateRefs += "/reference:$path"
}
$facade = Join-Path $managed 'netstandard.dll'
if (Test-Path -LiteralPath $facade) { $stateRefs += "/reference:$facade" }
$common = @('/nologo','/noconfig','/nostdlib+','/target:library') + $references
$common += "/resource:$(Join-Path $PSScriptRoot 'localization\en.xml'),QH.en.xml"
$dist = Join-Path $PSScriptRoot 'dist\Content\Mods'
$hostOutput = Join-Path $dist 'ModLoader\MODs\QuantumHangarQc'
$stateOutput = Join-Path $dist 'QuantumHangarQcState'
New-Item -ItemType Directory -Path $hostOutput,$stateOutput -Force | Out-Null
$hostSources = @('QuantumHangarQc.cs','LiveState.cs','Markers.cs','HangarUi.cs') | ForEach-Object { Join-Path $PSScriptRoot ('src\QuantumHangarQc\' + $_) }
$stateSources = @('LiveState.cs','StateMod.cs','Markers.cs','GpsBridge.cs','HangarUi.cs','UiBridge.cs') | ForEach-Object { Join-Path $PSScriptRoot ('src\QuantumHangarQcState\' + $_) }
$sharedSource = Join-Path $PSScriptRoot 'src\Shared\Localization.cs'
$hostSources += $sharedSource
$stateSources += $sharedSource
foreach ($source in @($hostSources) + @($stateSources)) {
    if (-not (Test-Path -LiteralPath $source)) { throw "Missing source / Source absente: $source" }
}
& $compiler @common "/reference:$hostMif" "/out:$hostOutput\QuantumHangarQc.dll" @hostSources
if ($LASTEXITCODE -ne 0) { throw 'Host compilation failed / Echec compilation Host.' }
& $compiler @common @stateRefs "/out:$stateOutput\QuantumHangarQcState.dll" @stateSources
if ($LASTEXITCODE -ne 0) { throw 'State compilation failed / Echec compilation State.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'config\QuantumHangarQcState_Info.yaml') -Destination $stateOutput -Force
foreach ($output in @($hostOutput,$stateOutput)) {
    $languages = Join-Path $output 'Languages'
    New-Item -ItemType Directory -Path $languages -Force | Out-Null
    Copy-Item -Path (Join-Path $PSScriptRoot 'localization\*.xml') -Destination $languages -Force
}
Write-Host "QH BETA 0.2.6 built / compile: $dist"
Write-Host 'Build only. Read docs/INSTALLATION_FR_EN.md before installation.'
Write-Host 'Compilation uniquement. Lire la documentation avant installation.'
