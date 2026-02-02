param(
  [Parameter(Mandatory = $true)]
  [ValidateSet(5, 10, 15, 20)]
  [int]$GridSize,
  [Parameter(Mandatory = $true)]
  [int]$GridId
)

$ErrorActionPreference = "Stop"

function Purge-MotCroiseEnv {
  Get-ChildItem Env:MOTCROISE_* -ErrorAction SilentlyContinue | ForEach-Object {
    try { Remove-Item $_.Name -ErrorAction SilentlyContinue } catch {}
  }
}

Purge-MotCroiseEnv

$root = (Get-Location).Path
$outDir = Join-Path $root ("GRILLES\\GRILLE {0}x{0}" -f $GridSize)
$gridNo = "{0:000}" -f $GridId
$cacheDir = Join-Path $root "cache"
$defsDb = Join-Path $cacheDir "definitions.fr.sqlite"
$resultDb = Join-Path $outDir ("motcroise-{0}.result.sqlite" -f $gridNo)

if (-not (Test-Path -LiteralPath $resultDb)) {
  throw "Result DB introuvable: $resultDb"
}

$env:MOTCROISE_CACHE_DIR = $cacheDir
$env:MOTCROISE_DEFS_DB = $defsDb
$env:MOTCROISE_RESULT_DB = $resultDb
$env:MOTCROISE_GRID_SIZE = "$GridSize"
$env:MOTCROISE_MODE = "pdf"
$env:MOTCROISE_IGNORE_DEFS = "1"
$env:MOTCROISE_USE_WIKTIONARY_NET = "0"
$env:MOTCROISE_PROGRESS = "1"
$env:MOTCROISE_PROGRESS_INLINE = "0"
$env:MOTCROISE_PHASES = "1"

Write-Host "PDF only: size=$GridSize id=$gridNo"
Write-Host "Result DB: $resultDb"

dotnet run --project .\MotCroise.Generator

