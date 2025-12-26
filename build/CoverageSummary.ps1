param(
  [Parameter(Mandatory = $true)]
  [string]$CoverageDir,

  [string[]]$KeyComponentPathPrefixes = @(
    "Modulus.Core\",
    "Modulus.Sdk\",
    "Modulus.Cli\",
    "Modulus.HostSdk.",
    "Hosts\"
  ),

  [ValidateRange(0, 100)]
  [double]$MinLineCoveragePercent = 95.0,

  [ValidateRange(0, 200)]
  [int]$ShowWorstFiles = 0
)

$ErrorActionPreference = "Stop"

function Normalize-Path([string]$p) {
  return $p.Replace("/", "\\")
}

function Get-ComponentName([string]$filePath, [string[]]$prefixes) {
  $p = Normalize-Path $filePath
  foreach ($prefix in $prefixes) {
    $pref = Normalize-Path $prefix
    $idx = $p.IndexOf($pref, [System.StringComparison]::OrdinalIgnoreCase)
    if ($idx -ge 0) {
      $rest = $p.Substring($idx + $pref.Length)
      # rest begins with e.g. "Architecture\\X.cs", map by prefix
      return $pref.TrimEnd("\\")
    }
  }
  return $null
}

if (-not (Test-Path $CoverageDir)) {
  throw "CoverageDir not found: $CoverageDir"
}

$coverageFiles = Get-ChildItem -Path $CoverageDir -Recurse -Filter "coverage.cobertura.xml" | Select-Object -ExpandProperty FullName
if ($coverageFiles.Count -eq 0) {
  throw "No coverage.cobertura.xml files found under: $CoverageDir"
}

Write-Host "Cobertura files: $($coverageFiles.Count)"

# Merge coverage by (file,line) using max(hits) across all runs.
$lineHits = @{} # key: "<file>|<lineNumber>" -> hits

foreach ($file in $coverageFiles) {
  [xml]$doc = Get-Content -Path $file
  $classes = $doc.SelectNodes("//class[@filename]")
  foreach ($c in $classes) {
    $fn = Normalize-Path $c.filename
    # Ignore generated/obj/bin if present
    if ($fn -match "\\\\obj\\\\" -or $fn -match "\\\\bin\\\\") { continue }

    $lines = $c.SelectNodes("./lines/line[@number and @hits]")
    foreach ($l in $lines) {
      $n = [int]$l.number
      $hits = [int]$l.hits
      $k = "$fn|$n"
      if ($lineHits.ContainsKey($k)) {
        if ($hits -gt $lineHits[$k]) { $lineHits[$k] = $hits }
      } else {
        $lineHits[$k] = $hits
      }
    }
  }
}

if ($lineHits.Count -eq 0) {
  throw "No line coverage entries found in Cobertura files."
}

# Aggregate per component (by path prefix)
$componentTotal = @{}   # component -> total lines
$componentCovered = @{} # component -> covered lines
$fileTotal = @{}        # filename -> total lines
$fileCovered = @{}      # filename -> covered lines

foreach ($k in $lineHits.Keys) {
  $parts = $k.Split("|", 2)
  $fn = $parts[0]
  $hits = [int]$lineHits[$k]

  $comp = Get-ComponentName -filePath $fn -prefixes $KeyComponentPathPrefixes
  if ($null -eq $comp) { continue }

  if (-not $componentTotal.ContainsKey($comp)) {
    $componentTotal[$comp] = 0
    $componentCovered[$comp] = 0
  }

  $componentTotal[$comp] = [int]$componentTotal[$comp] + 1
  if ($hits -gt 0) {
    $componentCovered[$comp] = [int]$componentCovered[$comp] + 1
  }

  if (-not $fileTotal.ContainsKey($fn)) {
    $fileTotal[$fn] = 0
    $fileCovered[$fn] = 0
  }
  $fileTotal[$fn] = [int]$fileTotal[$fn] + 1
  if ($hits -gt 0) {
    $fileCovered[$fn] = [int]$fileCovered[$fn] + 1
  }
}

if ($componentTotal.Keys.Count -eq 0) {
  throw "No key-component source files matched. Prefixes: $($KeyComponentPathPrefixes -join ', ')"
}

$failed = $false

Write-Host ""
Write-Host "Key component line coverage (threshold: $MinLineCoveragePercent%)"

foreach ($comp in ($componentTotal.Keys | Sort-Object)) {
  $total = [int]$componentTotal[$comp]
  $covered = [int]$componentCovered[$comp]
  $rate = if ($total -eq 0) { 0.0 } else { ($covered * 100.0) / $total }
  $status = if ($rate -ge $MinLineCoveragePercent) { "OK" } else { "FAIL" }
  if ($status -eq "FAIL") { $failed = $true }

  "{0,-25} {1,6:F2}% ({2}/{3}) {4}" -f $comp, $rate, $covered, $total, $status | Write-Host
}

if ($ShowWorstFiles -gt 0) {
  Write-Host ""
  Write-Host "Worst covered files (top $ShowWorstFiles):"

  $rows = foreach ($fn in $fileTotal.Keys) {
    $total = [int]$fileTotal[$fn]
    $covered = [int]$fileCovered[$fn]
    $rate = if ($total -eq 0) { 0.0 } else { ($covered * 100.0) / $total }
    [pscustomobject]@{
      File = $fn
      LineCoveragePercent = [math]::Round($rate, 2)
      Covered = $covered
      Total = $total
    }
  }

  $rows | Sort-Object LineCoveragePercent, File | Select-Object -First $ShowWorstFiles | Format-Table -AutoSize | Out-String | Write-Host
}

if ($failed) {
  throw "Coverage gate failed: one or more key components are below $MinLineCoveragePercent% line coverage."
}


