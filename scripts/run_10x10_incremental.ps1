param(
  [int]$GridSize = 10,
  [int]$PoolPerLength = 1000
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
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$cacheDir = Join-Path $root "cache"
$defsDb = Join-Path $cacheDir "definitions.fr.sqlite"
if (-not (Test-Path -LiteralPath $defsDb)) {
  throw "Defs DB introuvable: $defsDb"
}

$resultDb = Join-Path $outDir "motcroise.result.sqlite"
$log = Join-Path $outDir "run-grid.log"
Remove-Item -LiteralPath $log -ErrorAction SilentlyContinue

Get-Process MotCroise.Generator -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

$env:MOTCROISE_CACHE_DIR = $cacheDir
$env:MOTCROISE_DEFS_DB = $defsDb
$env:MOTCROISE_RESULT_DB = $resultDb
$env:MOTCROISE_GRID_SIZE = "$GridSize"
$env:MOTCROISE_MODE = "grid"

$env:MOTCROISE_SOLVER = "incremental"
$env:MOTCROISE_POOL_USE_DEFS = "1"
$env:MOTCROISE_POOL_PER_LENGTH = "$PoolPerLength"
$env:MOTCROISE_POOL_MAX = "200000"
$env:MOTCROISE_POOL_MIN_TOTAL = "0"
$env:MOTCROISE_POOL_MIN_DEFS = "5000"

$env:MOTCROISE_CACHE = "1"
$env:MOTCROISE_IGNORE_DEFS = "1"
$env:MOTCROISE_USE_WIKTIONARY_NET = "0"

$env:MOTCROISE_PROGRESS = "1"
$env:MOTCROISE_PROGRESS_INLINE = "0"
$env:MOTCROISE_PHASES = "1"

$env:MOTCROISE_INCREMENTAL_INTERLEAVED = "1"
$env:MOTCROISE_INCREMENTAL_VALIDATE_VERTICALS = "0"
$env:MOTCROISE_INCREMENTAL_FINAL_VALIDATE = "1"
$env:MOTCROISE_INCREMENTAL_MIN_FIRST = "6"
$env:MOTCROISE_INCREMENTAL_ATTEMPTS = "1200"
$env:MOTCROISE_INCREMENTAL_PARALLEL = "20"
$env:MOTCROISE_INCREMENTAL_CANDIDATES = "800"
$env:MOTCROISE_INCREMENTAL_CROSS_DEPTH = "3"
$env:MOTCROISE_INCREMENTAL_CROSS_CANDIDATES = "120"
$env:MOTCROISE_INCREMENTAL_MAX_NODES = "8000000"
$env:MOTCROISE_INCREMENTAL_MAX_SECONDS = "90"

Write-Host "Output: $outDir"
Write-Host "Result DB: $resultDb"
Write-Host "Defs DB: $defsDb"
Write-Host ""

"=== GRID $GridSize x $GridSize (incremental) ===" | Out-File -FilePath $log -Encoding UTF8
dotnet run --project .\MotCroise.Generator 2>&1 | Tee-Object -FilePath $log -Append
