param([string[]]$Roots = @("Assets/Scenes/Scripts"))

# ---------------------------------------------------------------------------
# CS0136 checker.
#
# "A local or parameter named 'x' cannot be declared in this scope because
# that name is used in an enclosing local scope to define a local or parameter"
#
# C# does not allow a name to be reused in a nested scope when an enclosing
# scope already declares it, even in a branch that is never taken and even
# when the two are different types. JavaScript allows it; C# does not.
#
# The rule is about NESTING, not depth: two declarations in sibling scopes are
# fine even when those scopes sit at different depths, for example
#
#     foreach (string tag in a) { ... }   // sibling scope
#     foreach (string tag in b) { ... }   // sibling scope, deeper
#
# So this tracks the full scope PATH of every declaration and reports a name
# only when one declaration's scope path is a proper prefix of another's,
# which is exactly the "enclosing scope" condition.
# ---------------------------------------------------------------------------
$ErrorActionPreference = 'Stop'

function Strip-Code([string]$text) {
    if ($null -eq $text) { return '' }
    $text = [regex]::Replace($text, '(?s)/\*.*?\*/', ' ')
    $text = [regex]::Replace($text, '(?m)//.*$', ' ')
    $text = [regex]::Replace($text, '@"(?:[^"]|"")*"', '""')
    $text = [regex]::Replace($text, '\$"(?:[^"\\]|\\.)*"', '""')
    $text = [regex]::Replace($text, '"(?:[^"\\]|\\.)*"', '""')
    $text = [regex]::Replace($text, "'(?:[^'\\]|\\.)*'", "''")
    return $text
}

$types = 'var|int|float|bool|string|double|long|short|byte|char|decimal|uint|ulong|ushort|sbyte|object|dynamic|[A-Z][\w<>\[\],\.]*'

$declRx    = [regex]"(?m)^\s*(?:$types)\s+(\w+)\s*(?:=|;|,)"
$foreachRx = [regex]"(?m)foreach\s*\(\s*(?:$types)\s+(\w+)\s+in\b"
$forRx     = [regex]"(?m)for\s*\(\s*(?:$types)\s+(\w+)\s*="
$catchRx   = [regex]"(?m)catch\s*\(\s*(?:$types)\s+(\w+)\s*\)"
$usingRx   = [regex]"(?m)using\s*\(\s*(?:$types)\s+(\w+)\s*\)"

$files = @()
foreach ($root in $Roots) {
    $files += Get-ChildItem $root -Recurse -File -Filter "*.cs" |
              Where-Object { $_.FullName -notmatch '\\Plugins\\' -and $_.Length -gt 0 }
}

$problems = @()

foreach ($f in $files) {
    $text = Strip-Code (Get-Content -LiteralPath $f.FullName -Raw)
    if ([string]::IsNullOrEmpty($text)) { continue }
    $lines = $text -split "`r?`n"

    $scopeStack = New-Object System.Collections.ArrayList   # scope ids
    $scopeIsMethod = @{}     # scope id -> is this a method body?
    $scopePath = @{}         # scope id -> the path of its enclosing scopes
    $scopeId = 0
    $methodScopes = @{}     # method scope id -> { name -> [scope paths] }
    $pendingMethod = $false

    # A foreach / for / using variable lives in the scope of the loop BODY,
    # not the scope the loop statement sits in. The body's '{' is usually on
    # the next line, so the scope id has to be looked up ahead of time.
    $loopVarScope = @{}     # line index -> scope id of that loop's body
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($foreachRx.IsMatch($lines[$i]) -or $forRx.IsMatch($lines[$i]) -or $usingRx.IsMatch($lines[$i])) {
            for ($j = $i; $j -lt $lines.Count; $j++) {
                if ($lines[$j] -match '\{') { $loopVarScope[$i] = $j; break }
            }
        }
    }
    $scopeIdOfLine = @{}
    $nextId = 1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $n = ([regex]::Matches($lines[$i], '\{')).Count
        if ($n -gt 0 -and -not $scopeIdOfLine.ContainsKey($i)) { $scopeIdOfLine[$i] = $nextId; $nextId += $n }
    }

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]

        $plainNames = @()
        foreach ($m in $declRx.Matches($line))  { $plainNames += $m.Groups[1].Value }
        $loopNames = @()
        foreach ($m in $foreachRx.Matches($line)) { $loopNames += $m.Groups[1].Value }
        foreach ($m in $forRx.Matches($line))     { $loopNames += $m.Groups[1].Value }
        foreach ($m in $usingRx.Matches($line))   { $loopNames += $m.Groups[1].Value }
        foreach ($m in $catchRx.Matches($line))   { $loopNames += $m.Groups[1].Value }

        # A method signature ends with ')'. Its body may open on this line or
        # on the next one.
        if ($line -match '^\s*(?:public|private|protected|internal)\s' -and
            $line -match '^\s*(?:[\w<>\[\],\.]+\s+)*\w+\s*\([^;]*\)\s*\{?\s*$') {
            $pendingMethod = $true
        }

        $opens = ([regex]::Matches($line, '\{')).Count
        $closes = ([regex]::Matches($line, '\}')).Count

        for ($o = 0; $o -lt $opens; $o++) {
            $scopeId++
            [void]$scopeStack.Add($scopeId)
            $scopeIsMethod[$scopeId] = $false
            $scopePath[$scopeId] = ($scopeStack -join '/')
            if ($pendingMethod -and $o -eq 0) {
                $scopeIsMethod[$scopeId] = $true
                $pendingMethod = $false
            }
        }

        if ($plainNames.Count -gt 0 -or $loopNames.Count -gt 0) {
            # Find the innermost METHOD scope these declarations belong to.
            # A declaration that is not inside a method body is a class field,
            # and a field may legally share a name with a method local.
            $owner = $null
            for ($s = $scopeStack.Count - 1; $s -ge 0; $s--) {
                if ($scopeIsMethod[$scopeStack[$s]]) { $owner = $scopeStack[$s]; break }
            }

            if ($null -ne $owner) {
                if (-not $methodScopes.ContainsKey($owner)) { $methodScopes[$owner] = @{} }

                $plainPath = ($scopeStack -join '/')
                foreach ($n in $plainNames) {
                    if (-not $methodScopes[$owner].ContainsKey($n)) {
                        $methodScopes[$owner][$n] = New-Object System.Collections.ArrayList
                    }
                    [void]$methodScopes[$owner][$n].Add($plainPath)
                }

                # A loop variable is scoped to the loop body.
                if ($loopNames.Count -gt 0 -and $loopVarScope.ContainsKey($i)) {
                    $bodyLine = $loopVarScope[$i]
                    if ($scopeIdOfLine.ContainsKey($bodyLine)) {
                        $loopPath = $scopePath[$scopeIdOfLine[$bodyLine]]
                        foreach ($n in $loopNames) {
                            if (-not $methodScopes[$owner].ContainsKey($n)) {
                                $methodScopes[$owner][$n] = New-Object System.Collections.ArrayList
                            }
                            [void]$methodScopes[$owner][$n].Add($loopPath)
                        }
                    }
                }
            }
        }

        for ($c = 0; $c -lt $closes; $c++) { if ($scopeStack.Count -gt 0) { $scopeStack.RemoveAt($scopeStack.Count - 1) } }
    }

    # Now look for names whose scope paths nest.
    foreach ($owner in $methodScopes.Keys) {
        foreach ($n in $methodScopes[$owner].Keys) {
            $paths = @($methodScopes[$owner][$n] | Sort-Object -Unique)
            for ($a = 0; $a -lt $paths.Count; $a++) {
                for ($b = 0; $b -lt $paths.Count; $b++) {
                    if ($a -eq $b) { continue }
                    if ($paths[$b].StartsWith($paths[$a] + '/')) {
                        $problems += "$($f.Name): '$n' is declared in a nested scope (scope '$($paths[$a])' encloses '$($paths[$b])')"
                    }
                }
            }
        }
    }
}

if ($problems.Count -eq 0) {
    Write-Output "OK   no CS0136 scope conflicts (no name reused in a nested scope)."
} else {
    Write-Output "FAIL CS0136 scope conflicts:"
    $problems | Sort-Object -Unique | ForEach-Object { "       $_" }
}
