param(
  [int[]]$Sizes = @(5, 10, 15, 20)
)

$ErrorActionPreference = "Stop"

foreach ($s in $Sizes) {
  & (Join-Path $PSScriptRoot "generate_artifacts.ps1") -GridSize $s
}

