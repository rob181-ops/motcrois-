param(
  [int]$Count = 20,
  [ValidateSet(5, 10, 15, 20)]
  [int[]]$Sizes = @(5, 10, 15, 20),
  [int]$Retries = 10,
  [int]$MaxUniqueTriesPerGrid = 30
)

$ErrorActionPreference = "Stop"

function Purge-MotCroiseEnv {
  Get-ChildItem Env:MOTCROISE_* -ErrorAction SilentlyContinue | ForEach-Object {
    try { Remove-Item $_.Name -ErrorAction SilentlyContinue } catch {}
  }
}

Purge-MotCroiseEnv

$root = (Get-Location).Path

for ($s = 0; $s -lt $Sizes.Count; $s++) {
  $gridSize = $Sizes[$s]
  $outDir = Join-Path $root ("GRILLES\\GRILLE {0}x{0}" -f $gridSize)
  New-Item -ItemType Directory -Force -Path $outDir | Out-Null

  # Keep a per-size set of signatures so we don't generate duplicates.
  $seen = New-Object 'System.Collections.Generic.HashSet[string]'

  Write-Host ""
  Write-Host "==============================="
  Write-Host ("TAILLE {0}x{0}" -f $gridSize)
  Write-Host "==============================="

  for ($i = 1; $i -le $Count; $i++) {
    $gridNo = "{0:000}" -f $i
    Write-Host ""
    Write-Host ("--- Grille {0} / {1} (taille {2}x{2}) ---" -f $gridNo, ("{0:000}" -f $Count), $gridSize)

    $resultDb = Join-Path $outDir ("motcroise-{0}.result.sqlite" -f $gridNo)
    $jsonPath = Join-Path $outDir ("grille-{0}.json" -f $gridNo)
    $pdfPath = Join-Path $outDir ("motcroise-{0}.pdf" -f $gridNo)
    $pdfSolPath = Join-Path $outDir ("motcroiseSolution-{0}.pdf" -f $gridNo)

    # Resume-friendly: if a valid grid already exists on disk, keep it and move on.
    if (Test-Path -LiteralPath $resultDb) {
      $defsDb = Join-Path (Join-Path $root "cache") "definitions.fr.sqlite"
      $sigOut = @"
import sqlite3, hashlib, sys
result_db = r'''$resultDb'''
defs_db = r'''$defsDb'''

con = sqlite3.connect(result_db)
cur = con.cursor()
cur.execute("SELECT row_text FROM grid_rows ORDER BY row_index")
rows = [r[0] for r in cur.fetchall()]
grid = "\n".join(rows)
black = grid.count("#")
dots = grid.count(".")
letters = sum(1 for ch in grid if "A" <= ch <= "Z")
cur.execute("SELECT word FROM placements")
words = sorted({r[0] for r in cur.fetchall()})
con.close()

if dots != 0 or len(words) == 0:
  sys.exit(2)

con = sqlite3.connect(defs_db)
cur = con.cursor()
for w in words:
  cur.execute("SELECT 1 FROM definitions WHERE word = ? LIMIT 1", (w,))
  if cur.fetchone() is None:
    sys.exit(3)
con.close()

h = hashlib.sha256(grid.encode("utf-8")).hexdigest()
print(h)
"@ | python -
      if ($LASTEXITCODE -eq 0) {
        $signature = ($sigOut -split "`r?`n" | Select-Object -First 1).Trim()
        if (-not [string]::IsNullOrWhiteSpace($signature) -and $seen.Add($signature)) {
          Write-Host ("OK (existant): {0}" -f $gridNo)
          continue
        }
      }

      Write-Host ("Grille existante invalide/duplicate, on regenere: {0}" -f $gridNo)
      Remove-Item -LiteralPath $resultDb -ErrorAction SilentlyContinue
      Remove-Item -LiteralPath $jsonPath -ErrorAction SilentlyContinue
      Remove-Item -LiteralPath $pdfPath -ErrorAction SilentlyContinue
      Remove-Item -LiteralPath $pdfSolPath -ErrorAction SilentlyContinue
    }

    $ok = $false
    for ($try = 1; $try -le [Math]::Max(1, $MaxUniqueTriesPerGrid); $try++) {
      # Each attempt re-purges MOTCROISE_* so no old env var can interfere.
      Purge-MotCroiseEnv

      pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\generate_artifacts.ps1 `
        -GridSize $gridSize `
        -GridId $i `
        -OutDir $outDir `
        -Retries $Retries

      if ($LASTEXITCODE -ne 0) {
        throw ("Echec generation: taille={0}, grille={1}" -f $gridSize, $gridNo)
      }

      if (-not (Test-Path -LiteralPath $resultDb)) {
        throw ("Result DB manquant: {0}" -f $resultDb)
      }

      # Compute a signature and validate the grid is fully filled (no '.'), and has placements.
      $defsDb = Join-Path (Join-Path $root "cache") "definitions.fr.sqlite"
      $sigOut = @"
import sqlite3, hashlib, sys
result_db = r'''$resultDb'''
defs_db = r'''$defsDb'''

con = sqlite3.connect(result_db)
cur = con.cursor()
cur.execute("SELECT row_text FROM grid_rows ORDER BY row_index")
rows = [r[0] for r in cur.fetchall()]
grid = "\n".join(rows)
black = grid.count("#")
dots = grid.count(".")
letters = sum(1 for ch in grid if "A" <= ch <= "Z")
cur.execute("SELECT word FROM placements")
words = sorted({r[0] for r in cur.fetchall()})
con.close()

# Validate "fully filled": no '.' and at least some placements.
if dots != 0 or len(words) == 0:
  print(f"invalid: letters={letters} dots={dots} black={black} words={len(words)}", file=sys.stderr)
  sys.exit(2)

# Validate every word has a definition in SQLite (source of truth for "real words").
con = sqlite3.connect(defs_db)
cur = con.cursor()
missing = []
for w in words:
  cur.execute("SELECT 1 FROM definitions WHERE word = ? LIMIT 1", (w,))
  if cur.fetchone() is None:
    missing.append(w)
con.close()
if missing:
  print("missing_defs=" + ",".join(missing[:20]), file=sys.stderr)
  sys.exit(3)

h = hashlib.sha256(grid.encode("utf-8")).hexdigest()
print(h)
"@ | python -

      $exit = $LASTEXITCODE
      if ($exit -ne 0) {
        Write-Host ("Grille invalide (try {0}/{1}), on regenere..." -f $try, $MaxUniqueTriesPerGrid)
        Remove-Item -LiteralPath $resultDb -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $jsonPath -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $pdfPath -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $pdfSolPath -ErrorAction SilentlyContinue
        continue
      }

      $signature = ($sigOut -split "`r?`n" | Select-Object -First 1).Trim()
      if ([string]::IsNullOrWhiteSpace($signature)) {
        throw "Impossible de calculer la signature de la grille."
      }

      if ($seen.Add($signature)) {
        $ok = $true
        break
      }

      Write-Host ("Duplicate detectee (try {0}/{1}), on regenere..." -f $try, $MaxUniqueTriesPerGrid)
      # Remove produced files so the next attempt re-generates this id cleanly.
      Remove-Item -LiteralPath $resultDb -ErrorAction SilentlyContinue
      Remove-Item -LiteralPath $jsonPath -ErrorAction SilentlyContinue
      Remove-Item -LiteralPath $pdfPath -ErrorAction SilentlyContinue
      Remove-Item -LiteralPath $pdfSolPath -ErrorAction SilentlyContinue
    }

    if (-not $ok) {
      throw ("Impossible d'obtenir une grille unique et remplie: taille={0}, grille={1} (apres {2} essais)" -f $gridSize, $gridNo, $MaxUniqueTriesPerGrid)
    }
  }
}

Write-Host ""
Write-Host "OK: generation terminee"
