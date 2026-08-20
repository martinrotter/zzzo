$ErrorActionPreference = "Stop"

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
$projectPath = Join-Path $repositoryRoot "ZZZO\ZZZO\ZZZO.csproj"
$publishDirectory = Join-Path $repositoryRoot "ZZZO\ZZZO\bin\Release\net10.0-windows7.0\publish\win-x86"
$archivePath = Join-Path $repositoryRoot "zzzo.zip"

dotnet publish $projectPath -p:PublishProfile=FolderProfile -v:minimal
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$requiredFiles = @(
    "ZZZO.exe"
    "ZZZO.deps.json"
    "ZZZO.runtimeconfig.json"
    "CefSharp.BrowserSubprocess.Core.dll"
)

foreach ($fileName in $requiredFiles) {
    $filePath = Join-Path $publishDirectory $fileName
    if (-not (Test-Path -LiteralPath $filePath -PathType Leaf)) {
        throw "Required publish file is missing: $filePath"
    }
}

$obsoleteSubprocess = Join-Path $publishDirectory "CefSharp.BrowserSubprocess.exe"
if (Test-Path -LiteralPath $obsoleteSubprocess) {
    throw "Unexpected standalone CefSharp subprocess found: $obsoleteSubprocess"
}

7z a -tzip -mx9 $archivePath (Join-Path $publishDirectory "*")
if ($LASTEXITCODE -ne 0) {
    throw "7-Zip failed with exit code $LASTEXITCODE."
}

Get-Item -LiteralPath $archivePath
