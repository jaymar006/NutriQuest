param([string]$Root = "Assets/Scenes")

# ---------------------------------------------------------------------------
# Finds GameObjects whose MonoBehaviour references a script that no longer
# exists ("Script attached to X in scene Y is missing or no valid script is
# attached"). Unity logs these at build time and they usually mean a script was
# deleted or renamed without its .meta, leaving a dangling GUID behind.
#
# The script GUIDs are resolved against every .cs.meta in the project plus
# every .cs.meta inside Library/PackageCache, so package scripts are not
# reported as missing.
# ---------------------------------------------------------------------------
$ErrorActionPreference = 'Stop'

# ---- 1. collect every known script GUID -------------------------------------
$known = New-Object System.Collections.Generic.HashSet[string]

function Add-Guid([string]$metaPath) {
    try {
        $head = Get-Content -LiteralPath $metaPath -TotalCount 4 -ErrorAction Stop
        foreach ($l in $head) {
            if ($l -match '^guid:\s*([a-f0-9]{32})') { [void]$known.Add($Matches[1]); return }
        }
    } catch { }
}

Get-ChildItem "Assets" -Recurse -File -Filter "*.cs.meta" -ErrorAction SilentlyContinue | ForEach-Object { Add-Guid $_.FullName }
if (Test-Path "Library/PackageCache") {
    Get-ChildItem "Library/PackageCache" -Recurse -File -Filter "*.cs.meta" -ErrorAction SilentlyContinue |
        ForEach-Object { Add-Guid $_.FullName }
}
Write-Output "Known script GUIDs: $($known.Count)"

# ---- 2. scan the scenes -----------------------------------------------------
$scenes = Get-ChildItem $Root -Recurse -File -Filter "*.unity" -ErrorAction SilentlyContinue
$found = @()

foreach ($scene in $scenes) {
    $lines = Get-Content -LiteralPath $scene.FullName

    # map fileID -> GameObject name
    $goNames = @{}
    $curId = $null; $curIsGo = $false
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^--- !u!1 &(-?\d+)') { $curId = $Matches[1]; $curIsGo = $true; continue }
        if ($curIsGo -and $lines[$i] -match '^  m_Name: (.*)$') { $goNames[$curId] = $Matches[1].Trim(); $curIsGo = $false }
    }

    # walk MonoBehaviour blocks
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -notmatch '^--- !u!114 &(-?\d+)') { continue }
        $blockStart = $i
        $end = $i + 1
        while ($end -lt $lines.Count -and $lines[$end] -notmatch '^--- !u!') { $end++ }
        $seg = $lines[$blockStart..($end - 1)]

        $scriptLine = $seg | Select-String -Pattern '^  m_Script: \{fileID: (-?\d+)(, guid: ([a-f0-9]{32}))?' | Select-Object -First 1
        if (-not $scriptLine) { continue }

        $fileId = $scriptLine.Matches[0].Groups[1].Value
        $guid   = $scriptLine.Matches[0].Groups[3].Value

        # fileID 0 with no guid is a null script
        if ($fileId -eq '0' -and [string]::IsNullOrEmpty($guid)) {
            $goLine = $seg | Select-String -Pattern '^  m_GameObject: \{fileID: (-?\d+)\}' | Select-Object -First 1
            $goName = if ($goLine) { $goNames[$goLine.Matches[0].Groups[1].Value] } else { "<unknown>" }
            $found += [pscustomobject]@{ Scene = $scene.FullName.Replace((Get-Location).Path + '\',''); Object = $goName; Guid = "(null script)" }
            continue
        }

        if ([string]::IsNullOrEmpty($guid)) { continue }
        if ($known.Contains($guid)) { continue }

        $goLine = $seg | Select-String -Pattern '^  m_GameObject: \{fileID: (-?\d+)\}' | Select-Object -First 1
        $goName = if ($goLine) { $goNames[$goLine.Matches[0].Groups[1].Value] } else { "<unknown>" }
        $found += [pscustomobject]@{ Scene = $scene.FullName.Replace((Get-Location).Path + '\',''); Object = $goName; Guid = $guid }
    }
}

Write-Output ""
if ($found.Count -eq 0) {
    Write-Output "OK   no missing scripts in any scene."
} else {
    Write-Output "FAIL missing scripts:"
    $found | Group-Object Guid | ForEach-Object {
        Write-Output ("  guid {0}  ({1} occurrence(s))" -f $_.Name, $_.Count)
        $_.Group | ForEach-Object { Write-Output ("      {0,-24} in {1}" -f $_.Object, $_.Scene) }
    }
}
