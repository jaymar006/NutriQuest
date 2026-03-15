# Android Portrait Mode Setup Guide

## Step 1: Set Screen Orientation (Project Settings)

1. **Edit → Project Settings → Player**
2. **Android Tab** (or iOS Tab if needed)
3. **Resolution and Presentation** section:
   - **Default Orientation:** Select **Portrait**
   - **Allowed Orientations for Auto Rotation:** 
     - ✅ Portrait
     - ❌ Portrait Upside Down (optional)
     - ❌ Landscape Left
     - ❌ Landscape Right
   - **Use OS Autorotation:** ✅ (checked)

## Step 2: Configure Game View (Editor)

1. **Game View** (top of Scene/Game tabs)
2. Click the **Aspect Ratio** dropdown (shows "Free Aspect" by default)
3. Click **+** to add custom aspect ratio
4. Create: **Portrait (9:16)** or **1080 x 1920**
   - Name: "Portrait Android"
   - Type: Fixed Resolution
   - Width: **1080**
   - Height: **1920**

5. Select this aspect ratio to test in portrait mode

## Step 3: Canvas Setup for Portrait

For **every Canvas** in your scenes:

1. Select the **Canvas**
2. In Inspector, find **Canvas Scaler** component:
   - **UI Scale Mode:** Scale With Screen Size
   - **Reference Resolution:**
     - X: **1080** (width)
     - Y: **1920** (height) ⚠️ **Portrait: Height > Width**
   - **Match:** **0.5** (or 0 for width, 1 for height)
   - **Screen Match Mode:** Match Width Or Height

## Step 4: Update Project Settings File

The project settings should have:
- `defaultScreenOrientation: 4` (Portrait)
- `androidDefaultWindowWidth: 1080` (or 1440 for higher res)
- `androidDefaultWindowHeight: 1920` (or 2560 for higher res)

## Common Portrait Resolutions

- **Standard:** 1080 x 1920 (Full HD Portrait)
- **High Res:** 1440 x 2560 (QHD Portrait)
- **Modern:** 1080 x 2340 (with notch/safe area)

**Recommendation:** Use **1080 x 1920** as your reference resolution.

## Testing in Editor

1. **Game View** → Select "Portrait Android" aspect ratio
2. Your UI should now display in portrait orientation
3. Test all screens to ensure UI fits properly

## Android Build Settings

When building for Android:
- Unity will use Portrait orientation
- App will lock to portrait (unless auto-rotation is enabled)
- UI will scale properly on different Android screen sizes

## UI Layout Tips for Portrait

1. **Stack vertically:** Place elements top to bottom
2. **Use anchors:** Top, Center, Bottom anchors work well
3. **Safe area:** Account for notches/status bars at top
4. **Button sizes:** Minimum 60x60 pixels for touch targets
5. **Spacing:** Adequate spacing between interactive elements

## Quick Checklist

- [ ] Project Settings → Player → Default Orientation: Portrait
- [ ] Game View: Custom aspect ratio 1080x1920
- [ ] All Canvases: Reference Resolution 1080x1920 (portrait)
- [ ] Test UI in portrait Game View
- [ ] Build and test on Android device
