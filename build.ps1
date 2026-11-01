param(
    [Parameter(Mandatory=$true)][string]$ManagedDirectory,
    [Parameter(Mandatory=$true)][string]$BepInExCoreDirectory,
    [Parameter(Mandatory=$true)][string]$CompilerPath
)
$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'src/GemporiumGrabItems.cs'
$version = [regex]::Match((Get-Content -LiteralPath $source -Raw), 'PluginVersion = "(\d+\.\d+\.\d+)"').Groups[1].Value
if (!$version) { throw 'Missing plugin version' }
$references = @()
foreach ($name in @('mscorlib','System','System.Core','netstandard','UnityEngine','UnityEngine.CoreModule','UnityEngine.IMGUIModule','UnityEngine.TextRenderingModule','Unity.InputSystem','Assembly-CSharp')) {
    $references += '/reference:' + (Resolve-Path -LiteralPath (Join-Path $ManagedDirectory ($name + '.dll'))).Path
}
foreach ($name in @('BepInEx','0Harmony')) {
    $references += '/reference:' + (Resolve-Path -LiteralPath (Join-Path $BepInExCoreDirectory ($name + '.dll'))).Path
}
$package = Join-Path $PSScriptRoot ('dist/GemporiumGrabItems-' + $version + '-BepInEx')
if (Test-Path -LiteralPath $package) { throw 'Package directory already exists; use a clean build directory.' }
$plugins = Join-Path $package 'BepInEx/plugins/GemporiumGrabItems'
New-Item -ItemType Directory -Force -Path $plugins | Out-Null
& $CompilerPath /nologo /noconfig /nostdlib+ /target:library /langversion:7.3 /optimize+ /deterministic+ ('/out:' + (Join-Path $plugins 'GemporiumGrabItems.dll')) @references $source
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed' }
foreach ($name in @('README.md','README.zh-CN.md','BUILDING.md','BUILDING.zh-CN.md','CHANGELOG.md','CHANGELOG.zh-CN.md')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $package
}
$zip = $package + '.zip'
Compress-Archive -Path (Join-Path $package '*') -DestinationPath $zip
$hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'dist/SHA256SUMS.txt'), $hash + '  ' + [IO.Path]::GetFileName($zip) + [Environment]::NewLine, [Text.Encoding]::ASCII)
Get-FileHash -LiteralPath $zip -Algorithm SHA256
