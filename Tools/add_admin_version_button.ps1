param(
  [string]$Scene = "Assets/Scenes/MainMenu.unity",
  [string]$PanelTransformId = "325106521",     # SettingsWindow RectTransform
  [string]$ScriptGuid = "7b3c9e1f4a2d48c6b0e5f1a93c7d2e84"
)

$ErrorActionPreference = 'Stop'
$full = (Resolve-Path $Scene).Path

# --- read while preserving the file's encoding and line endings ---------------
$bytes = [System.IO.File]::ReadAllBytes($full)
$hasBom = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
$utf8Strict = New-Object System.Text.UTF8Encoding($false, $true)
$isUtf8 = $true
try { [void]$utf8Strict.GetString($bytes) } catch { $isUtf8 = $false }
if ($isUtf8) { $enc = New-Object System.Text.UTF8Encoding($hasBom) } else { $enc = [System.Text.Encoding]::GetEncoding(1252) }

$text = $enc.GetString($bytes)
if ($hasBom) { $text = $text.TrimStart([char]0xFEFF) }
$crlf = $text.Contains("`r`n")
$lines = New-Object System.Collections.ArrayList
foreach ($l in ($text -split "`r?`n")) { [void]$lines.Add($l) }

# --- sanity: the file IDs must not already exist -----------------------------
$ids = @('9900000001','9900000002','9900000003','9900000004','9900000005','9900000006',
          '9900000007','9900000008','9900000009','9900000010')
foreach ($id in $ids) {
  if ($text -match "&$id\b") { Write-Error "fileID $id already exists in $Scene"; exit 1 }
}

# --- 1. attach the new RectTransform to the settings panel -------------------
# The panel's m_Children list may already hold entries, so insert rather than
# replace.
$changed = $false
for ($i = 0; $i -lt $lines.Count; $i++) {
  if ($lines[$i] -match "^--- !u!224 &$PanelTransformId$") {
    for ($j = $i; $j -lt $i + 40; $j++) {
      if ($lines[$j] -match '^\s+m_Children: \[\]$') {
        $indent = ($lines[$j] -replace 'm_Children.*$','')
        $lines[$j] = "$indent" + "m_Children:"
        [void]$lines.Insert($j + 1, "$indent- {fileID: 9900000002}")
        $changed = $true
        break
      }
      if ($lines[$j] -match '^\s+m_Children:$') {
        # find the end of the existing list and append
        $indent = ($lines[$j] -replace 'm_Children.*$','')
        $k = $j + 1
        while ($k -lt $lines.Count -and $lines[$k] -match '^\s+- \{fileID:') { $k++ }
        [void]$lines.Insert($k, "$indent- {fileID: 9900000002}")
        $changed = $true
        break
      }
    }
    break
  }
}
if (-not $changed) { Write-Error "Could not find m_Children on the settings panel transform $PanelTransformId"; exit 1 }

# --- 2. build the new blocks ------------------------------------------------
$fontGuid = '8fef3936f024e4a428352e2061e0347c'
$fontMaterial = '-988498651693161367'

$blocks = @"
--- !u!1 &9900000001
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 9900000002}
  - component: {fileID: 9900000003}
  - component: {fileID: 9900000004}
  - component: {fileID: 9900000005}
  - component: {fileID: 9900000006}
  m_Layer: 5
  m_Name: AdminVersionButton
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &9900000002
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 9900000001}
  m_LocalRotation: {x: -0, y: -0, z: -0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 9900000008}
  m_Father: {fileID: $PanelTransformId}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0.5, y: 0.5}
  m_AnchorMax: {x: 0.5, y: 0.5}
  m_AnchoredPosition: {x: -193, y: -300}
  m_SizeDelta: {x: 430, y: 56}
  m_Pivot: {x: 0, y: 0.5}
--- !u!222 &9900000003
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 9900000001}
  m_CullTransparentMesh: 1
--- !u!114 &9900000004
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 9900000001}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}
  m_Name:
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 0}
  m_RaycastTarget: 1
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {fileID: 0}
  m_Type: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
--- !u!114 &9900000005
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 9900000001}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 4e29b1a8efbd4b44bb3f3716e73f07ff, type: 3}
  m_Name:
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Button
  m_Navigation:
    m_Mode: 3
    m_WrapAround: 0
    m_SelectOnUp: {fileID: 0}
    m_SelectOnDown: {fileID: 0}
    m_SelectOnLeft: {fileID: 0}
    m_SelectOnRight: {fileID: 0}
  m_Transition: 1
  m_Colors:
    m_NormalColor: {r: 1, g: 1, b: 1, a: 1}
    m_HighlightedColor: {r: 0.9607843, g: 0.9607843, b: 0.9607843, a: 1}
    m_PressedColor: {r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 1}
    m_SelectedColor: {r: 0.9607843, g: 0.9607843, b: 0.9607843, a: 1}
    m_DisabledColor: {r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 0.5019608}
    m_ColorMultiplier: 1
    m_FadeDuration: 0.1
  m_SpriteState:
    m_HighlightedSprite: {fileID: 0}
    m_PressedSprite: {fileID: 0}
    m_SelectedSprite: {fileID: 0}
    m_DisabledSprite: {fileID: 0}
  m_AnimationTriggers:
    m_NormalTrigger: Normal
    m_HighlightedTrigger: Highlighted
    m_PressedTrigger: Pressed
    m_SelectedTrigger: Selected
    m_DisabledTrigger: Disabled
  m_Interactable: 1
  m_TargetGraphic: {fileID: 9900000004}
  m_OnClick:
    m_PersistentCalls:
      m_Calls:
      - m_Target: {fileID: 9900000006}
        m_TargetAssemblyTypeName: AdminVersionButton, Assembly-CSharp
        m_MethodName: OpenAdminPanel
        m_Mode: 1
        m_Arguments:
          m_ObjectArgument: {fileID: 0}
          m_ObjectArgumentAssemblyTypeName: UnityEngine.Object, UnityEngine
          m_IntArgument: 0
          m_FloatArgument: 0
          m_StringArgument:
          m_BoolArgument: 0
        m_CallState: 2
  m_OnSubmit: {fileID: 0}
  m_OnCancel: {fileID: 0}
--- !u!114 &9900000006
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 9900000001}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $ScriptGuid, type: 3}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::AdminVersionButton
  versionLabel:
  label: {fileID: 9900000010}
  button: {fileID: 9900000005}
--- !u!1 &9900000007
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 9900000008}
  - component: {fileID: 9900000009}
  - component: {fileID: 9900000010}
  m_Layer: 5
  m_Name: Label
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &9900000008
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 9900000007}
  m_LocalRotation: {x: -0, y: -0, z: -0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 9900000002}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!222 &9900000009
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 9900000007}
  m_CullTransparentMesh: 1
--- !u!114 &9900000010
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 9900000007}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: f4688fdb7df04437aeb418b961361dc5, type: 3}
  m_Name:
  m_EditorClassIdentifier: Unity.TextMeshPro::TMPro.TextMeshProUGUI
  m_Material: {fileID: 0}
  m_Color: {r: 0.85, g: 0.89, b: 0.94, a: 1}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_text: Version
  m_isRightToLeft: 0
  m_fontAsset: {fileID: 11400000, guid: $fontGuid, type: 2}
  m_sharedMaterial: {fileID: $fontMaterial, guid: $fontGuid, type: 2}
  m_fontSharedMaterials: []
  m_fontMaterial: {fileID: 0}
  m_fontMaterials: []
  m_fontColor32:
    serializedVersion: 2
    rgba: 4294967295
  m_fontColor: {r: 0.85, g: 0.89, b: 0.94, a: 1}
  m_enableVertexGradient: 0
  m_colorMode: 3
  m_fontColorGradient:
    topLeft: {r: 0.85, g: 0.89, b: 0.94, a: 1}
    topRight: {r: 0.85, g: 0.89, b: 0.94, a: 1}
    bottomLeft: {r: 0.85, g: 0.89, b: 0.94, a: 1}
    bottomRight: {r: 0.85, g: 0.89, b: 0.94, a: 1}
  m_fontColorGradientPreset: {fileID: 0}
  m_spriteAsset: {fileID: 0}
  m_tintAllSprites: 0
  m_StyleSheet: {fileID: 0}
  m_TextStyleHashCode: -1183493901
  m_overrideHtmlColors: 0
  m_faceColor:
    serializedVersion: 2
    rgba: 4294967295
  m_fontSize: 32
  m_fontSizeBase: 32
  m_fontWeight: 400
  m_enableAutoSizing: 0
  m_fontSizeMin: 18
  m_fontSizeMax: 72
  m_fontStyle: 0
  m_HorizontalAlignment: 0
  m_VerticalAlignment: 256
  m_textAlignment: 65535
  m_characterSpacing: 0
  m_wordSpacing: 0
  m_lineSpacing: 0
  m_lineSpacingMax: 0
  m_paragraphSpacing: 0
  m_charWidthMaxAdj: 0
  m_TextWrappingMode: 1
  m_wordWrappingRatios: 0.4
  m_overflowMode: 0
  m_linkedTextComponent: {fileID: 0}
  parentLinkedComponent: {fileID: 0}
  m_enableKerning: 0
  m_ActiveFontFeatures: 6e72656b
  m_enableExtraPadding: 0
  checkPaddingRequired: 0
  m_isRichText: 1
  m_EmojiFallbackSupport: 1
  m_parseCtrlCharacters: 1
  m_isOrthographic: 1
  m_isCullingEnabled: 0
  m_horizontalMapping: 0
  m_verticalMapping: 0
  m_uvLineOffset: 0
  m_geometrySortingOrder: 0
  m_IsTextObjectScaleStatic: 0
  m_VertexBufferAutoSizeReduction: 0
  m_useMaxVisibleDescender: 1
  m_pageToDisplay: 1
  m_margin: {x: 0, y: 0, z: 0, w: 0}
  m_isUsingLegacyAnimationComponent: 0
  m_isVolumetricText: 0
  m_hasFontAssetChanged: 0
  m_baseMaterial: {fileID: 0}
  m_maskOffset: {x: 0, y: 0, z: 0, w: 0}
"@

$blockLines = $blocks -split "`r?`n"

# --- 3. insert the blocks before the SceneRoots section ----------------------
$insertAt = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
  if ($lines[$i] -match '^--- !u!1660057539') { $insertAt = $i; break }
}
if ($insertAt -lt 0) { Write-Error "SceneRoots section not found"; exit 1 }

for ($i = $lines.Count - 1; $i -ge $insertAt; $i--) { [void]$lines.RemoveAt($i) }
for ($i = $blockLines.Count - 1; $i -ge 0; $i--) { [void]$lines.Insert($insertAt, $blockLines[$i]) }
$out = $lines

$newText = [string]::Join("`n", [string[]]$out.ToArray())
if ($crlf) { $newText = $newText -replace "(?<!`r)`n", "`r`n" }
[System.IO.File]::WriteAllText($full, $newText, $enc)

Write-Output "Patched $Scene"
Write-Output "  inserted 10 blocks before line $insertAt"
Write-Output "  attached the new RectTransform to the settings panel's m_Children"



