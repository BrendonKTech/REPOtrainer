# Builds the plugin and the launcher, then makes a zip players can unzip straight into the R.E.P.O. folder.
# BepInEx's own files are copied from your game install (GameDir), which already has a clean BepInEx 5.
#
#   powershell -ExecutionPolicy Bypass -File .\package.ps1
#
# Output: dist\REPO-Item-ESP-v<version>.zip

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

# Same GameDir the plugin build uses (Directory.Build.props, or the csproj default)
$gameDir = (& dotnet msbuild "$root\REPOItemESP.csproj" -getProperty:GameDir).Trim()
if (-not (Test-Path "$gameDir\BepInEx\core\BepInEx.Preloader.dll")) {
    throw "BepInEx not found in '$gameDir'. Set GameDir in Directory.Build.props."
}

$version = [regex]::Match((Get-Content "$root\Plugin.cs" -Raw), 'PluginVersion = "([^"]+)"').Groups[1].Value

Write-Host "Building plugin and launcher..."
& dotnet build "$root\REPOItemESP.csproj" -c Release --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "Plugin build failed." }
& dotnet build "$root\Launcher\Launcher.csproj" -c Release --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "Launcher build failed." }

$dist = "$root\dist"
$stage = "$dist\stage"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force -Confirm:$false }
New-Item -ItemType Directory -Force "$stage\BepInEx\core", "$stage\BepInEx\plugins" | Out-Null

# BepInEx loader files only: no config, cache, logs or other plugins from your install
Copy-Item "$gameDir\winhttp.dll", "$gameDir\.doorstop_version" $stage
Copy-Item "$gameDir\BepInEx\core\*" "$stage\BepInEx\core" -Recurse

# Doorstop starts off, so normal Steam launches are vanilla; the launcher turns it on for one launch
$ini = Get-Content "$gameDir\doorstop_config.ini" -Raw
$ini = [regex]::Replace($ini, '(?m)^(\s*enabled\s*=\s*)\S*', '${1}false')
[IO.File]::WriteAllText("$stage\doorstop_config.ini", $ini)

Copy-Item "$root\bin\Release\netstandard2.1\REPOItemESP.dll" "$stage\BepInEx\plugins"
Copy-Item "$root\Launcher\bin\Release\net48\REPOItemESPLauncher.exe" "$stage\Play REPO with ESP.exe"
$readme = (Get-Content "$root\Packaging\README.txt" -Raw).Replace('{VERSION}', $version)
[IO.File]::WriteAllText("$stage\README - REPO Item ESP.txt", $readme)

$zip = "$dist\REPO-Item-ESP-v$version.zip"
if (Test-Path $zip) { Remove-Item $zip -Force -Confirm:$false }
# Entries are added by hand because PowerShell 5.1's CreateFromDirectory writes "\" separators,
# which some unzip tools turn into file names instead of folders
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::Open($zip, 'Create')
try {
    foreach ($file in Get-ChildItem $stage -Recurse -File -Force) {
        $entry = $file.FullName.Substring($stage.Length + 1).Replace('\', '/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $entry) | Out-Null
    }
}
finally {
    $archive.Dispose()
}

Write-Host "Done: $zip"
