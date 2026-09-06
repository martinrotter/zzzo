param(
    [switch]$Gui,
    [string]$Konfigurace = "Release"
)
$ErrorActionPreference = "Stop"
$korenZzzo = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
$projektTestu = Join-Path $korenZzzo "ZZZO\ZZZO.Tests\ZZZO.Tests.csproj"
$adresarTestu = Join-Path $korenZzzo "ZZZO\ZZZO.Tests\bin\$Konfigurace\net10.0-windows7.0\win-x86\publish"
dotnet publish $projektTestu -c $Konfigurace -r win-x86 --self-contained true -v:minimal
if ($LASTEXITCODE -ne 0) { throw "Sestavení testů selhalo: $LASTEXITCODE." }
$spoustecTestu = Join-Path $adresarTestu "ZZZO.Tests.exe"
if (-not (Test-Path -LiteralPath $spoustecTestu -PathType Leaf)) { throw "Chybí spouštěč testů: $spoustecTestu" }
if ($Gui) { & $spoustecTestu --gui } else { & $spoustecTestu }
if ($LASTEXITCODE -ne 0) { throw "Regresní testy selhaly: $LASTEXITCODE." }
