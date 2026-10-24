param([string[]]$Roots = @("Assets/Scenes/Scripts"))

# ---------------------------------------------------------------------------
# Static reference checker (a stand-in for the compiler).
#
# There is no Unity Editor on this machine, so nothing tells us that
# "AdminGameConfig does not contain a definition for KeyTower4RequiredTotal"
# until Unity is opened. This approximates the errors Unity would report:
#
#   1. collect the public members of every type we can name
#   2. find every Type.Member reference and report the ones that resolve to
#      nothing
#   3. report a missing using directive when a namespace type is used in a
#      declaration position
#   4. check that every AdminTabBase subclass implements the abstract members
#
# Comments are stripped before the scans so that prose like
# "// SceneTransitionManager.Awake() does X" is not mistaken for code.
# ---------------------------------------------------------------------------
$ErrorActionPreference = 'Stop'

function Remove-Comments([string]$text) {
    if ($null -eq $text) { return '' }
    $text = [regex]::Replace($text, '(?s)/\*.*?\*/', ' ')
    $text = [regex]::Replace($text, '(?m)//.*$', ' ')
    return $text
}

function Get-Members([string]$path) {
    $members = New-Object System.Collections.Generic.HashSet[string]
    $text = Remove-Comments (Get-Content -LiteralPath $path -Raw)
    if ([string]::IsNullOrEmpty($text)) { return $members }

    $patterns = @(
        '(?m)^\s*public\s+(?:const|static\s+readonly|readonly)\s+[\w<>\[\],\.\s]+?\s+(\w+)\s*(?:=|;)',
        '(?m)^\s*public\s+(?:static\s+)?(?:volatile\s+)?[\w<>\[\],\.]+\s+(\w+)\s*;',
        '(?m)^\s*public\s+(?:static\s+)?(?:override\s+)?(?:virtual\s+)?[\w<>\[\],\.\s]+?\s+(\w+)\s*\(',
        '(?m)^\s*public\s+(?:static\s+)?(?:override\s+)?[\w<>\[\],\.\s]+?\s+(\w+)\s*(?:=>|\{)',
        '(?m)^\s*public\s+static\s+event\s+[\w<>\[\],\.]+\s+(\w+)',
        '(?m)^\s*public\s+(?:class|struct|enum|interface)\s+(\w+)',
        '(?m)^\s*public\s+(\w+)\s*\('
    )
    foreach ($p in $patterns) {
        foreach ($m in [regex]::Matches($text, $p)) { [void]$members.Add($m.Groups[1].Value) }
    }
    return $members
}

# ---------------------------------------------------------------------------
# Index the types
# ---------------------------------------------------------------------------
$files = @()
foreach ($root in $Roots) {
    $files += Get-ChildItem $root -Recurse -File -Filter "*.cs" |
              Where-Object { $_.FullName -notmatch '\\Plugins\\' -and $_.Length -gt 0 }
}

$classes = @{}
foreach ($f in $files) {
    $name = [System.IO.Path]::GetFileNameWithoutExtension($f.FullName)
    if ($classes.ContainsKey($name)) { continue }
    $text = Remove-Comments (Get-Content -LiteralPath $f.FullName -Raw)
    if ($text -match "(?m)^\s*(?:public\s+|internal\s+)?(?:static\s+|sealed\s+|abstract\s+|partial\s+)*(?:class|struct)\s+$name\b") {
        $classes[$name] = Get-Members $f.FullName
    }
}
Write-Output "Indexed $($classes.Count) types."

# ---------------------------------------------------------------------------
# 1. Unresolved Type.Member references
# ---------------------------------------------------------------------------
$skip = @('Math','System','UnityEngine','TMPro','Object','Input','Screen','Application','Debug','JsonUtility',
          'Mathf','PlayerPrefs','Time','Random','Resources','Color','Vector2','Vector3','Rect','GUI','SceneManager',
          'Keyboard','Mouse','Text','String','Convert','Encoding','File','Path','Directory','Regex','DateTime',
          'List','Dictionary','HashSet','Guid','Array','Type','Console','Environment','Exception','Task','Thread',
          'RandomNumberGenerator','SHA256','Rfc2898DeriveBytes','HashAlgorithmName','RectTransform','Quaternion',
          'LayerMask','Physics','Camera','AudioSource','Coroutine','YieldInstruction','WaitForSeconds',
          'WaitForEndOfFrame','WaitForSecondsRealtime','Rigidbody','Collider','Texture2D','Sprite','AnimationCurve',
          'MonoBehaviour','GameObject','Canvas','SpriteRenderer','TextMeshProUGUI','TMP_Text','Button','Toggle','Image',
          'ScrollRect','LayoutElement','GraphicRaycaster','CanvasScaler','ContentSizeFitter','VerticalLayoutGroup',
          'HorizontalLayoutGroup','RectMask2D','UnityAction','Debug','TextAsset','Serializable','Header','Tooltip',
          'Range','Min','SerializeField','SpaceBefore','SpaceAfter','RequireComponent','AddComponentMenu','ExecuteAlways',
          'ExecuteInEditMode','ContextMenu','SelectionBase','DisallowMultipleComponent','DefaultExecutionOrder',
          'Conditional','HelpURL','PropertyAttribute','SerializeReference','ISerializationCallbackReceiver','Color32')

$problems = @()
foreach ($f in $files) {
    $text = Remove-Comments (Get-Content -LiteralPath $f.FullName -Raw)
    foreach ($m in [regex]::Matches($text, '\b([A-Z][A-Za-z0-9_]*)\.([A-Za-z_][A-Za-z0-9_]*)')) {
        $type = $m.Groups[1].Value
        $member = $m.Groups[2].Value
        if ($skip -contains $type) { continue }
        if (-not $classes.ContainsKey($type)) { continue }
        if ($classes[$type].Contains($member)) { continue }
        $problems += [pscustomobject]@{ File = $f.FullName.Replace((Get-Location).Path + '\',''); Ref = "$type.$member" }
    }
}
if ($problems.Count -eq 0) {
    Write-Output "OK   no unresolved Type.Member references."
} else {
    Write-Output "FAIL unresolved Type.Member references:"
    $problems | Sort-Object Ref, File -Unique | ForEach-Object { "       $($_.Ref)  in  $($_.File)" }
}

# ---------------------------------------------------------------------------
# 2. Missing using directives
# ---------------------------------------------------------------------------
# A fully qualified "UnityEngine.Something" does NOT need a using directive,
# so the namespace clause is deliberately left out of these patterns.
$needUnity = '(:|<|\bnew\s+|,\s*)(RectTransform|Vector2|Vector3|Vector4|Color32?|MonoBehaviour|GameObject|Mathf|Application|PlayerPrefs|TextAsset|SerializeField|Tooltip|Range|Min|Coroutine|IEnumerator|WaitForSeconds|WaitForEndOfFrame|WaitForSecondsRealtime|Canvas|CanvasScaler|GraphicRaycaster|Sprite|Texture2D|AnimationCurve|LayerMask|Rigidbody|Collider)\b'
$needTmp   = '(:|<|\bnew\s+|,\s*)(TMP_Text|TMP_InputField|TMP_FontAsset|TextMeshProUGUI|TMP_Settings)\b'
$needUi    = '(:|<|\bnew\s+|,\s*)(Button|Toggle|Image|ScrollRect|LayoutElement|VerticalLayoutGroup|HorizontalLayoutGroup|ContentSizeFitter|GraphicRaycaster|CanvasScaler|RectMask2D|UnityAction)\b'

$usingProblems = @()
foreach ($f in $files) {
    $raw = Get-Content -LiteralPath $f.FullName -Raw
    if ($null -eq $raw) { continue }
    $body = Remove-Comments $raw
    $body = $body -replace '(?m)^using[^\n]*\n', ''
    $rel = $f.FullName.Replace((Get-Location).Path + '\','')
    if ($body -match $needUnity -and $raw -notmatch '(?m)^using UnityEngine;')  { $usingProblems += "$rel : missing 'using UnityEngine;'" }
    if ($body -match $needTmp   -and $raw -notmatch '(?m)^using TMPro;')        { $usingProblems += "$rel : missing 'using TMPro;'" }
    if ($body -match $needUi    -and $raw -notmatch '(?m)^using UnityEngine.UI;') { $usingProblems += "$rel : missing 'using UnityEngine.UI;'" }
}
if ($usingProblems.Count -eq 0) {
    Write-Output "OK   no missing using directives."
} else {
    Write-Output "FAIL missing using directives:"
    $usingProblems | Sort-Object -Unique | ForEach-Object { "       $_" }
}
# ---------------------------------------------------------------------------
# 3. AdminTabBase subclasses must implement the abstract members
# ---------------------------------------------------------------------------
$tabProblems = @()
foreach ($f in $files) {
    $text = Remove-Comments (Get-Content -LiteralPath $f.FullName -Raw)
    if ($text -notmatch ':\s*AdminTabBase') { continue }
    $cm = [regex]::Match($text, 'class\s+(\w+)\s*:\s*AdminTabBase')
    if (-not $cm.Success) { continue }
    $cls = $cm.Groups[1].Value
    if ($text -notmatch 'override\s+void\s+Build\s*\(\s*RectTransform\s+') { $tabProblems += "$($f.Name): $cls does not implement Build(RectTransform)" }
    if ($text -notmatch 'override\s+string\s+TitleKey') { $tabProblems += "$($f.Name): $cls does not implement TitleKey" }
}
if ($tabProblems.Count -eq 0) {
    Write-Output "OK   every AdminTabBase subclass implements the abstract members."
} else {
    Write-Output "FAIL incomplete AdminTabBase subclasses:"
    $tabProblems | ForEach-Object { "       $_" }
}

# ---------------------------------------------------------------------------
# 4. Implicit Transform -> RectTransform downcast (CS0266)
# ---------------------------------------------------------------------------
$castProblems = @()
foreach ($f in $files) {
    $text = Remove-Comments (Get-Content -LiteralPath $f.FullName -Raw)
    foreach ($m in [regex]::Matches($text, '(?m)^\s*RectTransform\s+\w+\s*=\s*[\w\.]+\.transform\s*;')) {
        $castProblems += "$($f.Name): $($m.Value.Trim())"
    }
    foreach ($m in [regex]::Matches($text, '(?m)^\s*RectTransform\s+\w+\s*=\s*\w+\.transform\s*;')) {
        $castProblems += "$($f.Name): $($m.Value.Trim())"
    }
}
if ($castProblems.Count -eq 0) {
    Write-Output "OK   no implicit Transform to RectTransform downcasts."
} else {
    Write-Output "FAIL implicit Transform to RectTransform downcasts:"
    $castProblems | Sort-Object -Unique | ForEach-Object { "       $_" }
}

# ---------------------------------------------------------------------------
# 6. Layout elements must be created through AdminUi.EnsureLayout
# ---------------------------------------------------------------------------
$layoutProblems = @()
foreach ($f in $files) {
    if ($f.Name -eq 'AdminUi.cs') { continue }
    $text = Get-Content -LiteralPath $f.FullName -Raw
    if ($null -eq $text) { continue }
    if ($text -match 'AddComponent<LayoutElement>\(\)') {
        $layoutProblems += "$($f.FullName.Replace((Get-Location).Path + '\','')) : uses AddComponent<LayoutElement>() directly"
    }
}
if ($layoutProblems.Count -eq 0) {
    Write-Output "OK   every LayoutElement goes through AdminUi.EnsureLayout."
} else {
    Write-Output "FAIL LayoutElement created outside AdminUi:"
    $layoutProblems | ForEach-Object { "       $_" }
}

# ---------------------------------------------------------------------------
# 7. Balanced braces per file
# ---------------------------------------------------------------------------
$braceProblems = @()
foreach ($f in $files) {
    $text = Remove-Comments (Get-Content -LiteralPath $f.FullName -Raw)
    $o = ([regex]::Matches($text,'\{')).Count
    $c = ([regex]::Matches($text,'\}')).Count
    if ($o -ne $c) { $braceProblems += "$($f.Name): $o open / $c close" }
}
if ($braceProblems.Count -eq 0) {
    Write-Output "OK   braces balanced in every file."
} else {
    Write-Output "FAIL unbalanced braces:"
    $braceProblems | ForEach-Object { "       $_" }
}

