param([string]$OutFile = "Assets/Resources/Data/questions.json")

# ---------------------------------------------------------------------------
# Extracts the built-in question pools from the level scenes into JSON.
#
# The questions live serialized inside Level 1-4.unity as the questionPool
# list on the QuestionGeneratorBeta component. A shipped build cannot write
# back to those scene assets, so the teacher/parent admin panel edits an
# override pool instead (see AdminContentStore). This script produces the
# built-in catalogue the panel falls back to and uses to seed overrides.
#
# Run from the repository root:
#   powershell -File Tools/build_question_catalog.ps1
# ---------------------------------------------------------------------------

function Count-RealQuotes($s, $q) {
    if ($q -eq '"') { return ([regex]::Matches($s, '(?<!\\)"')).Count }
    $total = ([regex]::Matches($s, "'")).Count
    $escaped = ([regex]::Matches($s, "''")).Count
    return $total - $escaped
}

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
        $inner = [regex]::Replace($inner, '\\x([0-9a-fA-F]{2})', { param($m) [char][Convert]::ToInt32($m.Groups[1].Value, 16) })
        $inner = [regex]::Replace($inner, '\\u([0-9a-fA-F]{4})', { param($m) [char][Convert]::ToInt32($m.Groups[1].Value, 16) })
        return $inner.TrimEnd()
    }
    return $v.TrimEnd()
}

# Reads a value that may be a multi-line quoted scalar.
# Returns the RAW value (still quoted) and the index of the last line used.
function Read-Scalar($body, $idx, $firstValue) {
    $val = $firstValue
    $last = $idx
    if ($val.StartsWith('"') -or $val.StartsWith("'")) {
        $q = $val[0]
        $acc = $val
        $j = $idx
        while ((Count-RealQuotes $acc $q) -lt 2) {
            $j++
            if ($j -ge $body.Count) { break }
            $acc = $acc + "`n" + $body[$j].Trim()
            $last = $j
        }
        $val = $acc
    }
    return @{ Value = $val; Last = $last }
}

# Parses "  - key: value" / "    key: value" pairs into a list of hashtables.
function Parse-SequenceOfMaps($body, $startIdx) {
    $items = New-Object System.Collections.ArrayList
    $i = $startIdx + 1
    $cur = $null

    while ($i -lt $body.Count) {
        $l = $body[$i]

        if ($l -match '^  - (\w+): (.*)$') {
            if ($null -ne $cur) { [void]$items.Add($cur) }
            $cur = @{}
            $key = $Matches[1]
            $read = Read-Scalar $body $i $Matches[2]
            $cur[$key] = $read.Value
            $i = $read.Last + 1

            # A plain (unquoted) value may continue on deeper-indented lines.
            if (-not $cur[$key].StartsWith('"') -and -not $cur[$key].StartsWith("'")) {
                while ($i -lt $body.Count -and $body[$i] -match '^\s{6,}\S') {
                    $cur[$key] = $cur[$key] + ' ' + $body[$i].Trim()
                    $i++
                }
            }
            continue
        }

        if ($l -match '^    (\w+): (.*)$' -and $null -ne $cur) {
            $key = $Matches[1]
            $read = Read-Scalar $body $i $Matches[2]
            $cur[$key] = $read.Value
            $i = $read.Last + 1

            if (-not $cur[$key].StartsWith('"') -and -not $cur[$key].StartsWith("'")) {
                while ($i -lt $body.Count -and $body[$i] -match '^\s{6,}\S') {
                    $cur[$key] = $cur[$key] + ' ' + $body[$i].Trim()
                    $i++
                }
            }
            continue
        }

        break
    }

    if ($null -ne $cur) { [void]$items.Add($cur) }
    return $items
}

function Read-QuestionPool($path) {
    $lines = Get-Content -LiteralPath $path

    $blockStart = @()
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^--- !u!(\d+) &(-?\d+)') { $blockStart += ,@($i, $Matches[1], $Matches[2]) }
    }

    for ($b = 0; $b -lt $blockStart.Count; $b++) {
        $start = $blockStart[$b][0]
        $section = $blockStart[$b][1]
        $end = if ($b + 1 -lt $blockStart.Count) { $blockStart[$b + 1][0] } else { $lines.Count }
        if ($section -ne '114') { continue }

        $body = $lines[$start..($end - 1)]
        $isGenerator = $false
        foreach ($l in $body) {
            if ($l -match 'Assembly-CSharp::QuestionGeneratorBeta') { $isGenerator = $true; break }
        }
        if (-not $isGenerator) { continue }

        for ($k = 0; $k -lt $body.Count; $k++) {
            if ($body[$k] -match '^  questionPool:\s*$') {
                return Parse-SequenceOfMaps $body $k
            }
        }
    }

    return @()
}

$levels = @(
    @{ key = 'Level 1'; stageId = 'Stage_1'; path = 'Assets/Scenes/Levels/Level 1.unity' },
    @{ key = 'Level 2'; stageId = 'Stage_2'; path = 'Assets/Scenes/Levels/Level 2.unity' },
    @{ key = 'Level 3'; stageId = 'Stage_3'; path = 'Assets/Scenes/Levels/Level 3.unity' },
    @{ key = 'Level 4'; stageId = 'Stage_4'; path = 'Assets/Scenes/Levels/Level 4.unity' }
)

$pools = New-Object System.Collections.ArrayList

foreach ($lv in $levels) {
    $items = Read-QuestionPool $lv.path
    $questions = New-Object System.Collections.ArrayList

    foreach ($it in $items) {
        $q = [ordered]@{
            question           = $(if ($it.ContainsKey('question')) { Decode-Yaml $it['question'] } else { '' })
            answerA            = $(if ($it.ContainsKey('answerA')) { Decode-Yaml $it['answerA'] } else { '' })
            answerB            = $(if ($it.ContainsKey('answerB')) { Decode-Yaml $it['answerB'] } else { '' })
            answerC            = $(if ($it.ContainsKey('answerC')) { Decode-Yaml $it['answerC'] } else { '' })
            answerD            = $(if ($it.ContainsKey('answerD')) { Decode-Yaml $it['answerD'] } else { '' })
            correctAnswer      = $(if ($it.ContainsKey('correctAnswer')) { Decode-Yaml $it['correctAnswer'] } else { 'A' })
            questionFilipino   = $(if ($it.ContainsKey('questionFilipino')) { Decode-Yaml $it['questionFilipino'] } else { '' })
            answerAFilipino    = $(if ($it.ContainsKey('answerAFilipino')) { Decode-Yaml $it['answerAFilipino'] } else { '' })
            answerBFilipino    = $(if ($it.ContainsKey('answerBFilipino')) { Decode-Yaml $it['answerBFilipino'] } else { '' })
            answerCFilipino    = $(if ($it.ContainsKey('answerCFilipino')) { Decode-Yaml $it['answerCFilipino'] } else { '' })
            answerDFilipino    = $(if ($it.ContainsKey('answerDFilipino')) { Decode-Yaml $it['answerDFilipino'] } else { '' })
        }
        [void]$questions.Add($q)
    }

    [void]$pools.Add([ordered]@{
        levelKey  = $lv.key
        stageId   = $lv.stageId
        questions = $questions
    })

    Write-Output ("  {0}: {1} questions" -f $lv.key, $questions.Count)
}

$doc = [ordered]@{ schemaVersion = 1; pools = $pools }
$doc | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $OutFile -Encoding UTF8
Write-Output "Wrote $OutFile ($((Get-Item $OutFile).Length) bytes)"
