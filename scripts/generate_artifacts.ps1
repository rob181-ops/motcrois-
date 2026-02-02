param(
  [Parameter(Mandatory = $true)]
  [ValidateSet(5, 10, 15, 20)]
  [int]$GridSize,

  [string]$OutDir = "",

  [int]$GridId = 1,

  [int]$Retries = 10
)

$ErrorActionPreference = "Stop"

function Purge-MotCroiseEnv {
  Get-ChildItem Env:MOTCROISE_* -ErrorAction SilentlyContinue | ForEach-Object {
    try { Remove-Item $_.Name -ErrorAction SilentlyContinue } catch {}
  }
}

function Ensure-Dir([string]$Path) {
  if (-not (Test-Path -LiteralPath $Path)) {
    New-Item -ItemType Directory -Force -Path $Path | Out-Null
  }
}

function Run-Step([string]$Title, [scriptblock]$Action, [string]$LogPath) {
  Write-Host ""
  Write-Host "=== $Title ==="
  "=== $Title ===" | Out-File -FilePath $LogPath -Append -Encoding UTF8
  & $Action 2>&1 | Tee-Object -FilePath $LogPath -Append
}

Purge-MotCroiseEnv

$root = (Get-Location).Path
$cacheDir = Join-Path $root "cache"
$defsDb = Join-Path $cacheDir "definitions.fr.sqlite"
if (-not (Test-Path -LiteralPath $defsDb)) {
  throw "Defs DB introuvable: $defsDb"
}

if ([string]::IsNullOrWhiteSpace($OutDir)) {
  $OutDir = Join-Path $root ("GRILLES\\GRILLE {0}x{0}" -f $GridSize)
}
Ensure-Dir $OutDir

$gridNo = "{0:000}" -f $GridId

$log = Join-Path $OutDir ("run-{0}.log" -f $gridNo)
Remove-Item -LiteralPath $log -ErrorAction SilentlyContinue

$resultDb = Join-Path $OutDir ("motcroise-{0}.result.sqlite" -f $gridNo)
$jsonOut = Join-Path $OutDir ("grille-{0}.json" -f $gridNo)
$pdfOut = Join-Path $OutDir ("motcroise-{0}.pdf" -f $gridNo)
$pdfSolOut = Join-Path $OutDir ("motcroiseSolution-{0}.pdf" -f $gridNo)

# Prevent msbuild "file locked" (a previous generator process left running).
Get-Process MotCroise.Generator -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

# Common env
$env:MOTCROISE_CACHE_DIR = $cacheDir
$env:MOTCROISE_DEFS_DB = $defsDb
$env:MOTCROISE_RESULT_DB = $resultDb
$env:MOTCROISE_GRID_SIZE = "$GridSize"

$env:MOTCROISE_CACHE = "1"
$env:MOTCROISE_SOLVER = "incremental"
$env:MOTCROISE_IGNORE_DEFS = "1"
$env:MOTCROISE_USE_WIKTIONARY_NET = "0"
$env:MOTCROISE_DEFS_STRICT_FILTER = "0"
$env:MOTCROISE_MIN_SLOT_LENGTH = "2"

# Word pool (balanced by length). Large pool but solver uses candidate limits.
$env:MOTCROISE_POOL_USE_DEFS = "1"
$env:MOTCROISE_POOL_PER_LENGTH = "1000"
$env:MOTCROISE_POOL_MAX = "200000"
$env:MOTCROISE_POOL_MIN_TOTAL = "0"
$env:MOTCROISE_POOL_MIN_DEFS = "5000"

# Parallelism
$env:MOTCROISE_PARALLEL = "20"
$env:MOTCROISE_INCREMENTAL_PARALLEL = "20"
$env:MOTCROISE_PATTERN_PARALLEL = "12"
$env:MOTCROISE_PATTERN_VARIANT_PARALLEL = "2"
$env:MOTCROISE_PATTERN_SHUFFLES = "12"
$env:MOTCROISE_PATTERN_LOGS = "0"
$env:MOTCROISE_PATTERN_MAX_ATTEMPTS = "200"
$env:MOTCROISE_PATTERN_ATTEMPT_TIMEOUT_MS = "2500"
$env:MOTCROISE_SOLVER_PARALLEL = "1"

# Progress/logs
$env:MOTCROISE_PROGRESS = "1"
$env:MOTCROISE_PROGRESS_INLINE = "0"
$env:MOTCROISE_PHASES = "1"

# Incremental solver defaults (fills rows+cols interleaved; validates words in both directions)
$env:MOTCROISE_INCREMENTAL_INTERLEAVED = "1"
$env:MOTCROISE_INCREMENTAL_VALIDATE_VERTICALS = "0"
$env:MOTCROISE_INCREMENTAL_CROSS_DEPTH = "0"
$env:MOTCROISE_INCREMENTAL_CROSS_CANDIDATES = "120"
$env:MOTCROISE_INCREMENTAL_CANDIDATES = "600"
$env:MOTCROISE_INCREMENTAL_ATTEMPTS = "120"
$env:MOTCROISE_INCREMENTAL_MAX_NODES = "50000000"
$env:MOTCROISE_INCREMENTAL_MAX_SECONDS = "900"
$env:MOTCROISE_INCREMENTAL_MIN_FIRST = "14"

switch ($GridSize) {
  5  { 
    $env:MOTCROISE_SOLVER = "csp"
    $env:MOTCROISE_POOL_PER_LENGTH = "20000"
    $env:MOTCROISE_EXTRA_BLACKS = "10"
    $env:MOTCROISE_CSP_PARALLEL = "12"
    $env:MOTCROISE_CSP_ATTEMPTS = "2000"
    $env:MOTCROISE_CSP_CANDIDATES = "20000"
    $env:MOTCROISE_CSP_MAX_SECONDS = "60"
    $env:MOTCROISE_CSP_MAX_NODES = "30000000"
  }
  10 {
    $env:MOTCROISE_SOLVER = "pattern"
    $env:MOTCROISE_MIN_SLOT_LENGTH = "2"
    $env:MOTCROISE_POOL_PER_LENGTH = "8000"
    $env:MOTCROISE_PATTERN_VARIANTS = "80"
    $env:MOTCROISE_PATTERN_RANDOM_DENSITY_PCT = "22"
    $env:MOTCROISE_EXTRA_BLACKS = "60"
    $env:MOTCROISE_PATTERN_MIN_EXTRA_PCT = "25"
    $env:MOTCROISE_PATTERN_MAX_ATTEMPTS = "260"
    $env:MOTCROISE_PATTERN_ATTEMPT_TIMEOUT_MS = "2500"
  }
  15 {
    $env:MOTCROISE_SOLVER = "pattern"
    $env:MOTCROISE_MIN_SLOT_LENGTH = "2"
    $env:MOTCROISE_POOL_PER_LENGTH = "3000"
    $env:MOTCROISE_PATTERN_VARIANTS = "100"
    $env:MOTCROISE_PATTERN_RANDOM_DENSITY_PCT = "18"
    $env:MOTCROISE_EXTRA_BLACKS = "0"
    $env:MOTCROISE_PATTERN_MAX_ATTEMPTS = "1"
    $env:MOTCROISE_PATTERN_ATTEMPT_TIMEOUT_MS = "12000"
  }
  20 {
    $env:MOTCROISE_SOLVER = "pattern"
    $env:MOTCROISE_MIN_SLOT_LENGTH = "2"
    $env:MOTCROISE_POOL_PER_LENGTH = "2500"
    $env:MOTCROISE_PATTERN_VARIANTS = "120"
    $env:MOTCROISE_PATTERN_RANDOM_DENSITY_PCT = "16"
    $env:MOTCROISE_EXTRA_BLACKS = "0"
    $env:MOTCROISE_PATTERN_MAX_ATTEMPTS = "1"
    $env:MOTCROISE_PATTERN_ATTEMPT_TIMEOUT_MS = "20000"
  }
}

Write-Host "Output: $OutDir"
Write-Host "GridId: $gridNo"
Write-Host "Result DB: $resultDb"
Write-Host "Defs DB: $defsDb"

# 1) Generate grid (sqlite result)
$env:MOTCROISE_MODE = "grid"
$ok = $false
for ($i = 1; $i -le [Math]::Max(1, $Retries); $i++) {
  $null = Remove-Item -LiteralPath $resultDb -ErrorAction SilentlyContinue
  Run-Step ("GENERATION {0}x{0} (try {1}/{2})" -f $GridSize, $i, [Math]::Max(1, $Retries)) { dotnet run --project .\MotCroise.Generator } $log
  if ($LASTEXITCODE -eq 0 -and (Test-Path -LiteralPath $resultDb)) {
    $ok = $true
    break
  }
}
if (-not $ok) { throw "Aucune grille n'a ete generee apres $Retries tentatives. Voir: $log" }

if (-not (Test-Path -LiteralPath $resultDb)) {
  throw "Result DB non genere: $resultDb"
}

# 2) Export JSON (app format)
Run-Step "EXPORT JSON" { python .\MotCroise.App\scripts\import_sqlite_grid.py --grid-size $GridSize --grid-id $gridNo --result $resultDb --defs $defsDb --out $jsonOut } $log
if ($LASTEXITCODE -ne 0) { throw "python import_sqlite_grid.py a echoue (exit=$LASTEXITCODE). Voir: $log" }

# 3) Generate PDFs from sqlite (in output directory to avoid global file locks/collisions)
$env:MOTCROISE_MODE = "pdf"
$env:MOTCROISE_USE_WIKTIONARY_NET = "0"
$env:MOTCROISE_DEF_PASSES = "0"
$env:MOTCROISE_FILTER_DEFS = "0"

$pdfTmp = Join-Path $OutDir "motcroise.pdf"
$pdfSolTmp = Join-Path $OutDir "motcroiseSolution.pdf"
Remove-Item -LiteralPath $pdfTmp -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $pdfSolTmp -ErrorAction SilentlyContinue

Run-Step "GENERATION PDF" {
  Push-Location $OutDir
  try {
    dotnet run --project (Join-Path $root "MotCroise.Generator")
  } finally {
    Pop-Location
  }
} $log
if ($LASTEXITCODE -ne 0) { throw "dotnet run (pdf) a echoue (exit=$LASTEXITCODE). Voir: $log" }

if (-not (Test-Path -LiteralPath $pdfTmp)) { throw "PDF non genere: $pdfTmp" }
if (-not (Test-Path -LiteralPath $pdfSolTmp)) { throw "PDF solution non genere: $pdfSolTmp" }

Move-Item -Force -LiteralPath $pdfTmp -Destination $pdfOut
Move-Item -Force -LiteralPath $pdfSolTmp -Destination $pdfSolOut

Write-Host ""
Write-Host "OK: $OutDir"
