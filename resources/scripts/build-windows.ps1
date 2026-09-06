$ErrorActionPreference = "Stop"

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
$projectPath = Join-Path $repositoryRoot "ZZZO\ZZZO\ZZZO.csproj"
$publishDirectory = Join-Path $repositoryRoot ("ZZZO\ZZZO\bin\Release\package-" + [Guid]::NewGuid().ToString("N"))
if (-not $publishDirectory.StartsWith($repositoryRoot + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Publish directory is outside the repository."
}
& (Join-Path $PSScriptRoot "test-windows.ps1")
$archivePath = Join-Path $repositoryRoot "zzzo.zip"

dotnet publish $projectPath -p:PublishProfile=FolderProfile "-p:PublishDir=$publishDirectory/" -v:minimal
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$requiredFiles = @(
    "ZZZO.exe"
    "ZZZO.deps.json"
    "ZZZO.runtimeconfig.json"
    "CefSharp.BrowserSubprocess.Core.dll"
    "resources.pak"
    "locales\cs.pak"
    "Data\TinyMceEditor\editor.html"
    "Data\TinyMceEditor\tinymce\tinymce.min.js"
    "Data\Styles\svésedlice.css"
    "Data\Styles\svésedlice.css.footer"
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

$docasnyArchiv = Join-Path (Split-Path -Parent $publishDirectory) ("zzzo-" + [Guid]::NewGuid().ToString("N") + ".zip")
[System.IO.Compression.ZipFile]::CreateFromDirectory(
    $publishDirectory, $docasnyArchiv, [System.IO.Compression.CompressionLevel]::Optimal, $false)

Copy-Item -LiteralPath $docasnyArchiv -Destination $archivePath -Force
Get-Item -LiteralPath $archivePath
