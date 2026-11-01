# builds both zips into dist\
# powershell -ExecutionPolicy Bypass -File .\package.ps1

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

# same GameDir as the build
$gameDir = (& dotnet msbuild "$root\REPOItemESP.csproj" -getProperty:GameDir).Trim()
if (-not (Test-Path "$gameDir\BepInEx\core\BepInEx.Preloader.dll")) {
    throw "BepInEx not found in '$gameDir'. Set GameDir in Directory.Build.props."
}

$version = [regex]::Match((Get-Content "$root\Plugin.cs" -Raw), 'PluginVersion = "([^"]+)"').Groups[1].Value

Write-Host "Building..."
foreach ($project in "REPOItemESP.csproj", "Trainer\REPOTrainer.csproj", "Launcher\Launcher.csproj") {
    & dotnet build "$root\$project" -c Release --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $project" }
}

$dist = "$root\dist"
New-Item -ItemType Directory -Force $dist | Out-Null
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem

function New-ReleaseZip($zipName, $pluginDll, $launcherName, $readmeTemplate, $readmeName) {
    $stage = "$dist\stage\$zipName"
    if (Test-Path $stage) { Remove-Item $stage -Recurse -Force -Confirm:$false }
    New-Item -ItemType Directory -Force "$stage\BepInEx\core", "$stage\BepInEx\plugins" | Out-Null

    # only the loader, not my configs/logs
    Copy-Item "$gameDir\winhttp.dll", "$gameDir\.doorstop_version" $stage
    Copy-Item "$gameDir\BepInEx\core\*" "$stage\BepInEx\core" -Recurse

    # off by default, the launcher turns it on
    $ini = Get-Content "$gameDir\doorstop_config.ini" -Raw
    $ini = [regex]::Replace($ini, '(?m)^(\s*enabled\s*=\s*)\S*', '${1}false')
    [IO.File]::WriteAllText("$stage\doorstop_config.ini", $ini)

    Copy-Item $pluginDll "$stage\BepInEx\plugins"
    Copy-Item "$root\Launcher\bin\Release\net48\REPOItemESPLauncher.exe" "$stage\$launcherName"
    $readme = (Get-Content $readmeTemplate -Raw).Replace('{VERSION}', $version)
    [IO.File]::WriteAllText("$stage\$readmeName", $readme)

    $zip = "$dist\$zipName"
    if (Test-Path $zip) { Remove-Item $zip -Force -Confirm:$false }

    # CreateFromDirectory uses \ in paths which breaks some unzippers
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
}

New-ReleaseZip "REPO-Item-ESP-v$version.zip" "$root\bin\Release\netstandard2.1\REPOItemESP.dll" `
    "Play REPO with ESP.exe" "$root\Packaging\README.txt" "README - REPO Item ESP.txt"
New-ReleaseZip "REPO-Trainer-v$version.zip" "$root\Trainer\bin\Release\netstandard2.1\REPOTrainer.dll" `
    "Play REPO with Trainer.exe" "$root\Packaging\README-Trainer.txt" "README - REPO Trainer.txt"
