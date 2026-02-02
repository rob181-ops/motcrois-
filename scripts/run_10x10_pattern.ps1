param(
  [int]$GridSize = 10,
  [int]$PoolPerLength = 5000,
  [int]$PatternVariants = 64,
  [int]$ExtraBlacks = 80,
  [int]$PatternShuffles = 12
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
$log = Join-Path $outDir "run-grid-pattern.log"
Remove-Item -LiteralPath $log -ErrorAction SilentlyContinue

Get-Process MotCroise.Generator -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

$env:MOTCROISE_CACHE_DIR = $cacheDir
$env:MOTCROISE_DEFS_DB = $defsDb
$env:MOTCROISE_RESULT_DB = $resultDb
$env:MOTCROISE_GRID_SIZE = "$GridSize"
$env:MOTCROISE_MODE = "grid"

$env:MOTCROISE_SOLVER = "pattern"
$env:MOTCROISE_POOL_USE_DEFS = "1"
$env:MOTCROISE_POOL_PER_LENGTH = "$PoolPerLength"
$env:MOTCROISE_POOL_MAX = "400000"
$env:MOTCROISE_POOL_MIN_TOTAL = "0"
$env:MOTCROISE_POOL_MIN_DEFS = "5000"

$env:MOTCROISE_CACHE = "1"
$env:MOTCROISE_IGNORE_DEFS = "1"
$env:MOTCROISE_USE_WIKTIONARY_NET = "0"

$env:MOTCROISE_PROGRESS = "1"
$env:MOTCROISE_PROGRESS_INLINE = "0"
$env:MOTCROISE_PHASES = "1"
$env:MOTCROISE_STEP3_PROGRESS = "1"

$env:MOTCROISE_PATTERN_VARIANTS = "$PatternVariants"
$env:MOTCROISE_EXTRA_BLACKS = "$ExtraBlacks"
$env:MOTCROISE_PATTERN_PARALLEL = "20"
$env:MOTCROISE_PATTERN_VARIANT_PARALLEL = "10"
$env:MOTCROISE_PATTERN_SHUFFLES = "$PatternShuffles"
$env:MOTCROISE_PATTERN_LOGS = "0"
$env:MOTCROISE_STOP_ON_FIRST = "1"
$env:MOTCROISE_SOLVER_PARALLEL = "16"

Write-Host "Output: $outDir"
Write-Host "Result DB: $resultDb"
Write-Host "Defs DB: $defsDb"
Write-Host ""

"=== GRID $GridSize x $GridSize (pattern) ===" | Out-File -FilePath $log -Encoding UTF8
dotnet run --project .\MotCroise.Generator 2>&1 | Tee-Object -FilePath $log -Append
