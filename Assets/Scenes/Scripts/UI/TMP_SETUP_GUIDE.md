# TextMesh Pro Setup Guide

## Fix: Import TextMesh Pro Essential Resources

### Step 1: Import TMP Essential Resources

1. In Unity Editor, go to: **Window → TextMeshPro → Import TMP Essential Resources**
2. A dialog will appear asking to import resources
3. Click **Import** or **Import TMP Essentials**
4. This will import:
   - Font assets
   - Material presets
   - Sprite assets
   - Settings

### Step 2: Import TMP Examples & Extras (Optional but Recommended)

1. Go to: **Window → TextMeshPro → Import TMP Examples & Extras**
2. Click **Import** if you want example scenes and additional resources
3. This is optional but helpful for learning

### Step 3: Verify Import

After importing, you should see:
- **Assets/TextMesh Pro/Resources/** folder created
- Fonts and materials available
- No more error messages

## What This Does

TextMesh Pro needs:
- **Font Assets** - For rendering text
- **Material Presets** - For text styling
- **Settings** - Default configurations

These resources are required for any TextMeshProUGUI component to work properly.

## Quick Fix Checklist

- [ ] Window → TextMeshPro → Import TMP Essential Resources
- [ ] Click Import in the dialog
- [ ] Wait for import to complete
- [ ] Verify no errors in Console
- [ ] Test a TextMeshProUGUI component

## After Import

Once imported, all your TextMeshProUGUI components will work properly:
- LoadingScreenUI text elements
- LandingPageUI text elements
- All button text
- All UI text throughout the game

## Note

This only needs to be done **once per project**. The resources will be saved in your project and available for all scenes.
