param(
  # If true, tries to enable Hyper-V/WHPX features (requires admin + reboot).
  [switch]$EnableWindowsFeatures = $false,
  # If true, disables/removes the "Android Emulator Hypervisor Driver" SDK package folder to stop the installer.
  [switch]$DisableAndroidHypervisorDriver = $true
)

$ErrorActionPreference = "Stop"

$Script:ThisScriptRoot = $PSScriptRoot

function Write-Step($msg) {
  Write-Host ""
  Write-Host "==> $msg"
}

function Test-IsAdmin {
  $id = [Security.Principal.WindowsIdentity]::GetCurrent()
  $p = New-Object Security.Principal.WindowsPrincipal($id)
  return $p.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Get-AndroidSdkDir {
  $candidates = @()
  $here = Resolve-Path $Script:ThisScriptRoot
  for ($i = 0; $i -lt 5; $i++) {
    $root = $here.Path
    $candidates += Join-Path $root "MotCroise.App\\android\\local.properties"
    $candidates += Join-Path $root "..\\MotCroise.App\\android\\local.properties"
    $parent = Split-Path $root -Parent
    if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $root) { break }
    $here = Resolve-Path $parent
  }

  foreach ($localProps in ($candidates | Select-Object -Unique)) {
    if (-not (Test-Path $localProps)) { continue }
    $line = (Get-Content $localProps | Where-Object { $_ -match '^sdk\\.dir=' } | Select-Object -First 1)
    if (-not $line) { continue }
    $raw = $line.Substring("sdk.dir=".Length)
    # local.properties escapes ":" as "\:" on Windows
    $raw = $raw -replace '\\\\:', ':'
    # Replace double backslashes with single backslash
    $raw = $raw -replace '\\\\\\\\', '\\'
    if ($raw) { return $raw }
  }
  # Common manual SDK location (as seen in local.properties for this repo)
  if (Test-Path 'E:\SDKANDROID') { return 'E:\SDKANDROID' }
  if ($env:ANDROID_SDK_ROOT) { return $env:ANDROID_SDK_ROOT }
  if ($env:ANDROID_HOME) { return $env:ANDROID_HOME }
  return $null
}

Write-Host "Fix Android Emulator Hypervisor (Windows 11 / Intel)"

Write-Step "Verification BIOS (VT-x) / Hyperviseur"
try {
  $cpu = Get-CimInstance Win32_Processor | Select-Object -First 1 Name, VirtualizationFirmwareEnabled
  $hv = Get-CimInstance Win32_ComputerSystem | Select-Object -First 1 HypervisorPresent
  Write-Host ("CPU: {0}" -f $cpu.Name)
  Write-Host ("VirtualizationFirmwareEnabled (BIOS VT-x): {0}" -f $cpu.VirtualizationFirmwareEnabled)
  Write-Host ("HypervisorPresent (Windows): {0}" -f $hv.HypervisorPresent)
  if ($cpu.VirtualizationFirmwareEnabled -ne $true) {
    Write-Host ""
    Write-Host "PROBLEME: La virtualisation Intel (VT-x) est desactivee dans le BIOS/UEFI." -ForegroundColor Yellow
    Write-Host "=> Aucune acceleration (WHPX/AEHD) ne pourra fonctionner tant que VT-x est OFF."
    Write-Host ""
    Write-Host "Fix:"
    Write-Host "  1) Redemarre dans le BIOS/UEFI"
    Write-Host "  2) Active: Intel Virtualization Technology (VT-x)"
    Write-Host "  3) Sauvegarde/quit, puis relance Windows"
    Write-Host ""
    Write-Host "Apres ca, relance:"
    Write-Host "  E:\\SDKANDROID\\emulator\\emulator.exe -accel-check"
    Write-Host ""
    # Keep going (we can still toggle Windows features), but user must fix BIOS.
  }
} catch {
  Write-Host "Impossible de verifier l'etat VT-x/Hyperviseur (CIM)."
}

Write-Step "Etat des features Windows (Hyper-V / WHPX)"
$features = @("HypervisorPlatform", "VirtualMachinePlatform", "Microsoft-Hyper-V-All")
foreach ($f in $features) {
  try {
    $out = dism /online /get-featureinfo /featurename:$f
    $etat = ($out | Select-String -Pattern 'tat' | Select-Object -First 1).Line
    if ($etat) {
      Write-Host ("{0}: {1}" -f $f, ($etat -replace '^\s*', ''))
    } else {
      Write-Host ("{0}: (etat introuvable)" -f $f)
    }
  } catch {
    Write-Host ("{0}: (non disponible)" -f $f)
  }
}

if ($EnableWindowsFeatures) {
  Write-Step "Activation des features Windows (admin requis)"
  if (-not (Test-IsAdmin)) {
    Write-Host "ERREUR: relance PowerShell en administrateur ou execute sans -EnableWindowsFeatures." -ForegroundColor Red
    exit 2
  }

  cmd /c "bcdedit /set hypervisorlaunchtype auto" | Out-Null
  foreach ($f in $features) {
    Write-Host "Enable feature: $f"
    dism /online /enable-feature /featurename:$f /all /norestart | Out-Null
  }
  Write-Host "OK. Redemarrage requis."
}

if ($DisableAndroidHypervisorDriver) {
  Write-Step "Desactivation du package SDK 'Android_Emulator_Hypervisor_Driver'"
  $sdk = Get-AndroidSdkDir
  if (-not $sdk) {
    Write-Host "SDK introuvable (local.properties/ANDROID_SDK_ROOT). Ouvre Android Studio > SDK Manager et note le chemin, puis relance." -ForegroundColor Yellow
  } else {
    Write-Host "SDK: $sdk"
    $driverDir = Join-Path $sdk "extras\\google\\Android_Emulator_Hypervisor_Driver"
    if (Test-Path $driverDir) {
      $disabledDir = $driverDir + "_DISABLED_" + (Get-Date -Format "yyyyMMdd-HHmmss")
      try {
        Rename-Item -LiteralPath $driverDir -NewName (Split-Path $disabledDir -Leaf)
        Write-Host "Renomme (desactive): $disabledDir"
      } catch {
        try {
          Remove-Item -LiteralPath $driverDir -Recurse -Force
          Write-Host "Supprime: $driverDir"
        } catch {
          Write-Host "Impossible de renommer/supprimer: $driverDir" -ForegroundColor Red
          Write-Host $_.Exception.Message
        }
      }
    } else {
      Write-Host "Dossier non present: $driverDir"
    }
  }

  Write-Step "Conseil Android Studio"
  Write-Host "Dans Android Studio -> SDK Manager -> SDK Tools:"
  Write-Host "  - Decoche 'Android Emulator Hypervisor Driver' (si coche)."
  Write-Host "Avec Hyper-V/WHPX actif, ce driver n'est pas necessaire et peut echouer a demarrer."
}

Write-Host ""
Write-Host "Termine."
