param(
    [ValidateSet('win-x64', 'win-arm64')]
    [string] $Rid = 'win-x64'
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$artifactRoot = Join-Path $projectRoot 'artifacts'
$ridRoot = Join-Path $artifactRoot $Rid
$packageRoot = Join-Path $ridRoot 'package'

dotnet publish (Join-Path $projectRoot 'src/Skipower.Cli/Skipower.Cli.csproj') -c Release -r $Rid -o (Join-Path $ridRoot 'cli')
if ($LASTEXITCODE -ne 0) { throw 'CLI Native AOT publish failed.' }

dotnet publish (Join-Path $projectRoot 'src/Skipower.Gui/Skipower.Gui.csproj') -c Release -r $Rid -o (Join-Path $ridRoot 'gui')
if ($LASTEXITCODE -ne 0) { throw 'GUI Native AOT publish failed.' }

New-Item -ItemType Directory -Force -Path $packageRoot | Out-Null
Copy-Item (Join-Path $ridRoot 'cli/skipower.exe') (Join-Path $packageRoot 'skipower.exe')
Copy-Item (Join-Path $ridRoot 'gui/Skipower.Gui.exe') (Join-Path $packageRoot 'Skipower.Gui.exe')
Copy-Item (Join-Path $projectRoot 'licenses/MewUI-LICENSE.txt') (Join-Path $packageRoot 'MewUI-LICENSE.txt')
Copy-Item (Join-Path $projectRoot 'licenses/THIRD_PARTY_NOTICES.md') (Join-Path $packageRoot 'THIRD_PARTY_NOTICES.md')

$archive = Join-Path $artifactRoot "skipower-$Rid.zip"
Compress-Archive -Path (Join-Path $packageRoot '*') -DestinationPath $archive -Force
Write-Output $archive
