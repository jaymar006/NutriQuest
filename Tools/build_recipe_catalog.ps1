param([string]$OutFile = "Assets/Resources/Data/recipes.json")

function Decode-Yaml($v) {
    if ($null -eq $v) { return '' }
    $v = $v.Trim()
    if ($v.StartsWith("'")) {
        $v = $v.Substring(1, $v.Length - 2)
        return $v.Replace("''", "'").TrimEnd()
    }
    if ($v.StartsWith('"')) {
        $inner = $v.Substring(1, $v.Length - 2)
        $inner = $inner -replace '\\r\\n', "`r`n"
        $inner = $inner -replace '\\n', "`n"
        $inner = $inner -replace '\\t', "`t"
        $inner = $inner -replace '\\"', '"'
        $inner = $inner -replace '\\\\', '\'
        $inner = [regex]::Replace($inner, '\\x([0-9a-fA-F]{2})', {
            param($m) [char][Convert]::ToInt32($m.Groups[1].Value, 16)
        })
        $inner = [regex]::Replace($inner, '\\u([0-9a-fA-F]{4})', {
            param($m) [char][Convert]::ToInt32($m.Groups[1].Value, 16)
        })
        return $inner.TrimEnd()
    }
    return $v.TrimEnd()
}

function Count-RealQuotes($s, $q) {
    if ($q -eq '"') { return ([regex]::Matches($s, '(?<!\\)"')).Count }
    $total = ([regex]::Matches($s, "'")).Count
    $escaped = ([regex]::Matches($s, "''")).Count
    return $total - $escaped
}

# Reads a YAML scalar, which may be a multi-line quoted scalar.
# $firstValue is the field value with the field name already stripped.
function Read-Scalar($body, $startIdx, $firstValue) {
    $parts = New-Object System.Collections.ArrayList
    [void]$parts.Add($firstValue.Trim())

    if ($firstValue.StartsWith('"') -or $firstValue.StartsWith("'")) {
        $q = $firstValue[0]
        $acc = $firstValue
        $j = $startIdx
        while ((Count-RealQuotes $acc $q) -lt 2) {
            $j++
            if ($j -ge $body.Count) { break }
            $part = $body[$j].Trim()
            [void]$parts.Add($part)
            $acc = $acc + "`n" + $part
        }
    }

    # YAML flow-scalar folding: one line break folds to a space, N consecutive
    # breaks become N-1 newlines. The recipe ingredient lists use one blank
    # line between entries, so each entry must land on its own line.
    $out = $parts[0]
    $i = 1
    while ($i -lt $parts.Count) {
        $blank = 0
        while ($i -lt $parts.Count -and $parts[$i] -eq '') { $blank++; $i++ }
        if ($i -ge $parts.Count) { break }
        if ($blank -gt 0) { $out += ("`n" * $blank) } else { $out += ' ' }
        $out += $parts[$i]
        $i++
    }
    return $out
}

function Read-Scene($path) {
    $lines = Get-Content -LiteralPath $path

    $blockStart = @()
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^--- !u!(\d+) &(-?\d+)') { $blockStart += ,@($i, $Matches[1], $Matches[2]) }
    }

    $goNames = @{}; $trToGo = @{}; $trParent = @{}; $goParent = @{}
    $texts = @{}; $langOf = @{}

    for ($b = 0; $b -lt $blockStart.Count; $b++) {
        $start = $blockStart[$b][0]
        $section = $blockStart[$b][1]
        $id = $blockStart[$b][2]
        $end = if ($b + 1 -lt $blockStart.Count) { $blockStart[$b + 1][0] } else { $lines.Count }
        $body = $lines[$start..($end - 1)]

        if ($section -eq '1') {
            foreach ($l in $body) { if ($l -match '^  m_Name: (.*)$') { $goNames[$id] = $Matches[1].Trim(); break } }
            continue
        }

        if ($section -eq '4' -or $section -eq '224') {
            $go = $null; $father = $null
            foreach ($l in $body) {
                if ($null -eq $go     -and $l -match '^  m_GameObject: \{fileID: (-?\d+)\}') { $go = $Matches[1] }
                if ($null -eq $father -and $l -match '^  m_Father: \{fileID: (-?\d+)\}')    { $father = $Matches[1] }
            }
            if ($null -ne $go) { $trToGo[$id] = $go }
            if ($null -ne $father) { $trParent[$id] = $father }
            continue
        }

        if ($section -eq '114') {
            $cls = $null; $go = $null
            foreach ($l in $body) {
                if ($null -eq $cls -and $l -match '^  m_EditorClassIdentifier: (.*)$') { $cls = $Matches[1].Trim() }
                if ($null -eq $go  -and $l -match '^  m_GameObject: \{fileID: (-?\d+)\}')  { $go = $Matches[1] }
            }
            if ($null -eq $go) { continue }

            if ($cls -eq 'Unity.TextMeshPro::TMPro.TextMeshProUGUI') {
                for ($k = 0; $k -lt $body.Count; $k++) {
                    if ($body[$k] -match '^  m_text: (.*)$') {
                        $texts[$go] = Decode-Yaml (Read-Scalar $body $k $Matches[1])
                        break
                    }
                }
            }
            elseif ($cls -match 'Assembly-CSharp::LocalizedElement$') {
                foreach ($l in $body) {
                    if ($l -match '^  language: (\d+)') { $langOf[$go] = [int]$Matches[1]; break }
                }
            }
        }
    }

    foreach ($tr in $trToGo.Keys) {
        if ($trParent.ContainsKey($tr) -and $trParent[$tr] -ne '0' -and $trToGo.ContainsKey($trParent[$tr])) {
            $goParent[$trToGo[$tr]] = $trToGo[$trParent[$tr]]
        }
    }

    $resolved = @{}
    foreach ($goId in $texts.Keys) {
        $cur = $goId; $guard = 0; $found = 0
        while ($null -ne $cur -and $guard -lt 40) {
            if ($langOf.ContainsKey($cur)) { $found = $langOf[$cur]; break }
            $cur = if ($goParent.ContainsKey($cur)) { $goParent[$cur] } else { $null }
            $guard++
        }
        $resolved[$goId] = $found
    }

    return @{ names = $goNames; texts = $texts; langs = $resolved }
}

$recipes = New-Object System.Collections.ArrayList

$defs = @(
    @{ id = 'recipe_1'; scene = 'Recipe 1'; nameEn = 'Kid-Friendly Sinabawang Gulay'; nameFil = 'Kid-Friendly Sinabawang Gulay'; stage = 'Stage_1' },
    @{ id = 'recipe_2'; scene = 'Recipe 2'; nameEn = 'Filipino Fruit Salad';        nameFil = 'Filipino Fresh Fruit Salad';  stage = 'Stage_2' },
    @{ id = 'recipe_3'; scene = 'Recipe 3'; nameEn = 'Filipino Clubhouse Sandwich'; nameFil = 'Filipino Clubhouse Sandwich'; stage = 'Stage_3' },
    @{ id = 'recipe_4'; scene = 'Recipe 4'; nameEn = 'Veggie Stir-fried Chicken Meal for Kids'; nameFil = 'Veggie Stir-Fried Chicken Meal for Kids'; stage = 'Stage_4' }
)

$order   = @('P1', 'PSF1', 'Steps')
$subOf   = @{ 'P1' = 'M1'; 'PSF1' = 'SF1' }
$labelOf = @{ 'P1' = 'Ingredients'; 'PSF1' = 'Ingredients'; 'Steps' = 'Instructions' }

foreach ($r in $defs) {
    $s = Read-Scene "Assets/Scenes/LibraryModule/Recipes/$($r.scene).unity"

    $blocks = New-Object System.Collections.ArrayList
    foreach ($key in $order) {
        $en = $null; $fil = $null
        foreach ($goId in $s.texts.Keys) {
            if ($s.names[$goId] -ne $key) { continue }
            $txt = $s.texts[$goId]
            if ([string]::IsNullOrEmpty($txt)) { continue }
            if ($s.langs[$goId] -eq 1) { $fil = $txt } else { $en = $txt }
        }
        if ($null -eq $en -and $null -eq $fil) { continue }

        $subEn = $null; $subFil = $null
        if ($subOf.ContainsKey($key)) {
            $sk = $subOf[$key]
            foreach ($goId in $s.texts.Keys) {
                if ($s.names[$goId] -ne $sk) { continue }
                $txt = $s.texts[$goId]
                if ([string]::IsNullOrEmpty($txt)) { continue }
                if ($s.langs[$goId] -eq 1) { $subFil = $txt } else { $subEn = $txt }
            }
        }

        [void]$blocks.Add([ordered]@{
            id          = $key
            labelEn     = $labelOf[$key]
            labelFil    = ''
            subLabelEn  = $(if ($null -ne $subEn)  { $subEn }  else { '' })
            subLabelFil = $(if ($null -ne $subFil) { $subFil } else { '' })
            bodyEn      = $(if ($null -ne $en)  { $en }  else { '' })
            bodyFil     = $(if ($null -ne $fil) { $fil } else { '' })
        })
    }

    [void]$recipes.Add([ordered]@{
        id            = $r.id
        sceneName     = $r.scene
        unlockStageId = $r.stage
        nameEn        = $r.nameEn
        nameFil       = $r.nameFil
        blocks        = $blocks
    })
}

$doc = [ordered]@{ schemaVersion = 1; recipes = $recipes }
$doc | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $OutFile -Encoding UTF8

Write-Output "Wrote $OutFile ($((Get-Item $OutFile).Length) bytes)"
foreach ($rc in $recipes) {
    Write-Output ("  {0}  {1,-42} stage={2}  blocks={3}" -f $rc.id, $rc.nameEn, $rc.unlockStageId, $rc.blocks.Count)
    foreach ($b in $rc.blocks) {
        Write-Output ("      {0,-6} EN={1,4}ch FIL={2,4}ch  sub='{3}'" -f $b.id, $b.bodyEn.Length, $b.bodyFil.Length, $b.subLabelEn)
    }
}
