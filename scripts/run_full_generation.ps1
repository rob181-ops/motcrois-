param(
  [int]$Count = 20,
  [ValidateSet(5, 10, 15, 20)]
  [int[]]$Sizes = @(10, 15, 20),
  [int]$Retries = 10,
  [int]$MaxUniqueTriesPerGrid = 30
)

$ErrorActionPreference = "Stop"

function Assert-NoActiveGeneration {
  $active = Get-CimInstance Win32_Process -Filter "name='pwsh.exe'" |
    Where-Object { $_.CommandLine -like "*generate_all_sizes_20.ps1*" -or $_.CommandLine -like "*generate_artifacts.ps1*" }
  if ($active) {
    $pids = ($active | Select-Object -ExpandProperty ProcessId) -join ","
    throw "Generation deja en cours (pwsh pid(s): $pids). Stoppe-la ou attends la fin avant de relancer."
  }
}

Assert-NoActiveGeneration

foreach ($size in $Sizes) {
  Write-Host ""
  Write-Host "==============================="
  Write-Host "BATCH $size x $size"
  Write-Host "==============================="

  pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\generate_all_sizes_20.ps1 `
    -Sizes $size `
    -Count $Count `
    -Retries $Retries `
    -MaxUniqueTriesPerGrid $MaxUniqueTriesPerGrid

  if ($LASTEXITCODE -ne 0) {
    throw "Batch echoue pour $size x $size (exit=$LASTEXITCODE)"
  }
}

Write-Host ""
Write-Host "OK: generation terminee pour toutes les tailles."
