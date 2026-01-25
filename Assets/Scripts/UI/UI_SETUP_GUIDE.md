# Unity UI Setup Guide for NutriQuest (3D Project)

This guide will help you set up UI in your 3D Unity scenes.

## Overview
Since you're using a 3D project, each scene needs its own UI Canvas. The UI will overlay on top of your 3D content.

---

## Scene Setup Order

### 1. Bootstrap Scene (First Scene - Entry Point)

**Purpose:** Initializes all systems, shows loading screen

**Steps:**
1. Open `Bootstrap.unity`
2. Create UI Canvas:
   - Right-click Hierarchy → UI → Canvas
   - Name: "LoadingCanvas"
   - Canvas Settings:
     - Render Mode: **Screen Space - Overlay**
     - Add Component → Canvas Scaler
       - UI Scale Mode: **Scale With Screen Size**
       - Reference Resolution: **1920 x 1080**
       - Match: **0.5** (width/height)
   - Add Component → Graphic Raycaster

3. Create Loading Screen UI:
   - Right-click LoadingCanvas → UI → Panel
     - Name: "LoadingPanel"
     - Set background color (e.g., dark blue/black)
   
   - Right-click LoadingPanel → UI → Image
     - Name: "LoadingBarBackground"
     - Set color (e.g., dark gray)
     - Set RectTransform: Width 600, Height 30
     - Anchor: Center
   
   - Right-click LoadingBarBackground → UI → Image
     - Name: "LoadingBarFill"
     - Set color (e.g., green/blue)
     - Image Type: **Filled**
     - Fill Method: **Horizontal**
     - Fill Amount: **0**
     - Anchor: Stretch (all sides to 0)
   
   - Right-click LoadingPanel → UI → Text - TextMeshPro
     - Name: "LoadingText"
     - Text: "Loading..."
     - Font Size: 36
     - Alignment: Center
     - Position: Above loading bar  
   
   - Right-click LoadingPanel → UI → Text - TextMeshPro
     - Name: "ProgressText"
     - Text: "0%"
     - Font Size: 24
     - Alignment: Center
     - Position: Below loading bar

4. Create BootstrapManager GameObject:
   - Right-click Hierarchy → Create Empty
   - Name: "BootstrapManager"
   - Add Component → BootstrapManager
   - Configure:
     - Initialization Delay: 0.5
     - Skip To Main Menu: false
     - Show Loading Screen: true
     - Min Loading Time: 2

5. Create LoadingScreenUI Controller:
   - Right-click Hierarchy → Create Empty
   - Name: "LoadingScreenController"
   - Add Component → LoadingScreenUI
   - Assign References:
     - Loading Panel: LoadingPanel
     - Loading Bar Fill: LoadingBarFill
     - Loading Text: LoadingText
     - Progress Text: ProgressText

6. Set as First Scene:
   - File → Build Settings
   - Drag Bootstrap.unity to index 0 (first in list)

---

### 2. LandingPage Splash Scene (Optional - Title Screen)

**Purpose:** Simple splash screen with game title sprite and tap-to-continue

**Steps:**
1. Create new scene: File → New Scene → Basic (Built-in)
2. Save as `LandingPageSplash.unity` in Assets/Scenes/

3. Create UI Canvas:
   - Right-click Hierarchy → UI → Canvas
   - Name: "SplashCanvas"
   - Canvas Settings:
     - Render Mode: **Screen Space - Overlay**
     - Add Component → Canvas Scaler
       - UI Scale Mode: **Scale With Screen Size**
       - Reference Resolution: **1080 x 1920** (Portrait!)
       - Match: **0.5**
   - Add Component → Graphic Raycaster

4. Create Background:
   - Right-click SplashCanvas → UI → Panel
   - Name: "SplashPanel"
   - Set background color/image
   - Anchor: Stretch (all sides to 0)

5. Create Game Title Sprite:
   - Right-click SplashCanvas → UI → Image
   - Name: "GameTitleSprite"
   - **Important:** This should be a Sprite Image, not Text!
   - Add your game title sprite/logo
   - Position: **Center** (middle of screen)
   - Size: Adjust to fit your sprite (e.g., 600x200 or based on your sprite dimensions)
   - Preserve Aspect: **Enabled**

6. Create Tap to Continue Text:
   - Right-click SplashCanvas → UI → Text - TextMeshPro
   - Name: "TapToContinueText"
   - Text: "Tap the screen to continue"
   - Font Size: 32
   - Alignment: Center
   - Position: **Bottom** (anchor to bottom center)
   - Bottom offset: 100 (or adjust as needed)
   - Color: White or your preferred color

7. Create LandingPageSplashController:
   - Right-click Hierarchy → Create Empty
   - Name: "LandingPageSplashController"
   - Add Component → LandingPageSplashUI
   - Add Component → SceneController
   - Assign References:
     - Splash Panel: SplashPanel
     - Game Title Sprite: GameTitleSprite
     - Tap To Continue Text: TapToContinueText
   - Configure Settings:
     - Title Fade In Duration: 1.5
     - Tap Text Fade In Duration: 1.0
     - Tap Text Delay: 0.5
     - Title Scale Animation: true (optional)
     - Animate Tap Text: true (pulsing effect)
     - Min Display Time: 2.0 (minimum seconds before tap works)
     - Next Scene: "LoginPage" (or your desired next scene)

8. Add to Build Settings:
   - File → Build Settings
   - Add Open Scenes

**Note:** If you use this splash screen, update BootstrapManager to load `LandingPageSplash` instead of `LoginPage`, or set the splash to navigate to LoginPage.

---

### 3. LoginPage Scene (First User-Facing Screen)

**Purpose:** User authentication (login/register/guest)

**Steps:**
1. Create new scene: File → New Scene → Basic (Built-in)
2. Save as `LoginPage.unity` in Assets/Scenes/

3. Create UI Canvas:
   - Right-click Hierarchy → UI → Canvas
   - Name: "LoginCanvas"
   - Canvas Settings:
     - Render Mode: **Screen Space - Overlay**
     - Add Component → Canvas Scaler
       - UI Scale Mode: **Scale With Screen Size**
       - Reference Resolution: **1080 x 1920** (Portrait!)
       - Match: **0.5**
   - Add Component → Graphic Raycaster

4. Create Background:
   - Right-click LoginCanvas → UI → Panel
   - Name: "LoginPanel"
   - Set background color/image

5. Create Logo:
   - Right-click LoginCanvas → UI → Image
   - Name: "GameLogo"
   - Add your logo sprite
   - Position: Top center
   - Size: 200x200

6. Create Title:
   - Right-click LoginCanvas → UI → Text - TextMeshPro
   - Name: "GameTitle"
   - Text: "NutriQuest"
   - Font Size: 64
   - Alignment: Center
   - Position: Below logo

7. Create Login Form:
   - **Username Input:**
     - Right-click LoginCanvas → UI → Input Field - TextMeshPro
     - Name: "UsernameInput"
     - Placeholder: "Username"
     - Position: Center
     - Size: 600x60
   
   - **Password Input:**
     - Right-click LoginCanvas → UI → Input Field - TextMeshPro
     - Name: "PasswordInput"
     - Placeholder: "Password"
     - Content Type: **Password**
     - Position: Below username
     - Size: 600x60

8. Create Buttons:
   - **Login Button:**
     - Right-click LoginCanvas → UI → Button - TextMeshPro
     - Name: "LoginButton"
     - Text: "LOGIN"
     - Font Size: 36
     - Position: Below password
     - Size: 600x70
   
   - **Register Button:**
     - Right-click LoginCanvas → UI → Button - TextMeshPro
     - Name: "RegisterButton"
     - Text: "REGISTER"
     - Font Size: 36
     - Position: Below login button
     - Size: 600x70
   
   - **Guest Button:**
     - Right-click LoginCanvas → UI → Button - TextMeshPro
     - Name: "GuestButton"
     - Text: "PLAY AS GUEST"
     - Font Size: 28
     - Position: Below register button
     - Size: 400x50

9. Create Error Text:
   - Right-click LoginCanvas → UI → Text - TextMeshPro
   - Name: "ErrorText"
   - Text: ""
   - Font Size: 24
   - Color: Red
   - Position: Above username input
   - Initially hidden

10. Create Loading Indicator (optional):
    - Right-click LoginCanvas → UI → Image (spinner)
    - Name: "LoadingIndicator"
    - Add rotation animation or use a spinner sprite
    - Initially hidden

11. Create LoginPageController:
    - Right-click Hierarchy → Create Empty
    - Name: "LoginPageController"
    - Add Component → LoginPageUI
    - Add Component → SceneController
    - Assign all UI references in Inspector

12. Add to Build Settings:
    - File → Build Settings
    - Add Open Scenes

---

### 3. MainMenu Scene (Hub with Navigation Bar)

**Purpose:** Welcome screen with game logo and navigation

**Steps:**
1. Create new scene: File → New Scene → Basic (Built-in)
2. Save as `LandingPage.unity` in Assets/Scenes/

3. Create UI Canvas:
   - Right-click Hierarchy → UI → Canvas
   - Name: "LandingCanvas"
   - Same settings as Bootstrap Canvas

4. Create Background:
   - Right-click LandingCanvas → UI → Panel
   - Name: "BackgroundPanel"
   - Set background color/image

5. Create Logo:
   - Right-click LandingCanvas → UI → Image
   - Name: "GameLogo"
   - Add your logo sprite
   - Position: Top center
   - Size: 300x300

6. Create Title:
   - Right-click LandingCanvas → UI → Text - TextMeshPro
   - Name: "GameTitle"
   - Text: "NutriQuest"
   - Font Size: 72
   - Alignment: Center
   - Position: Below logo

7. Create Subtitle:
   - Right-click LandingCanvas → UI → Text - TextMeshPro
   - Name: "GameSubtitle"
   - Text: "Learn Nutrition Through Play"
   - Font Size: 32
   - Position: Below title

8. Create Buttons:
   - **Play Button:**
     - Right-click LandingCanvas → UI → Button - TextMeshPro
     - Name: "PlayButton"
     - Text: "PLAY"
     - Font Size: 48
     - Position: Center
     - Size: 300x80
   
   - **Continue Button:**
     - Right-click LandingCanvas → UI → Button - TextMeshPro
     - Name: "ContinueButton"
     - Text: "CONTINUE"
     - Font Size: 48
     - Position: Below Play button
     - Size: 300x80
   
   - **Settings Button:**
     - Right-click LandingCanvas → UI → Button - TextMeshPro
     - Name: "SettingsButton"
     - Text: "SETTINGS"
     - Font Size: 32
     - Position: Top right
     - Size: 150x50
   
   - **Profile Button:**
     - Right-click LandingCanvas → UI → Button - TextMeshPro
     - Name: "ProfileButton"
     - Text: "PROFILE"
     - Font Size: 32
     - Position: Next to Settings
     - Size: 150x50

9. Create Version Text:
   - Right-click LandingCanvas → UI → Text - TextMeshPro
   - Name: "VersionText"
   - Text: "Version 1.0.0"
   - Font Size: 16
   - Position: Bottom right
   - Color: Gray

10. Create LandingPageController:
    - Right-click Hierarchy → Create Empty
    - Name: "LandingPageController"
    - Add Component → LandingPageUI
    - Add Component → SceneController
    - Assign all UI references in Inspector

11. Add to Build Settings:
    - File → Build Settings
    - Add Open Scenes

---

**Purpose:** Main hub with tower/level selection and navigation bar

**Steps:**
1. Open `MainMenu.unity`

2. Create UI Canvas:
   - Right-click Hierarchy → UI → Canvas
   - Name: "MainMenuCanvas"
   - Same settings as LoginPage (1080x1920 portrait)

3. Create Main Content Area:
   - Right-click MainMenuCanvas → UI → Panel
   - Name: "MainContentPanel"
   - Anchor: Stretch (all sides)
   - Bottom offset: 100 (space for nav bar)

4. Create Header:
   - Right-click MainContentPanel → UI → Text - TextMeshPro
   - Name: "WelcomeText"
   - Text: "Welcome, Player!"
   - Font Size: 48
   - Position: Top center
   
   - Right-click MainContentPanel → UI → Text - TextMeshPro
   - Name: "PlayerLevelText"
   - Text: "Level 1"
   - Font Size: 32
   - Position: Below welcome
   
   - Right-click MainContentPanel → UI → Text - TextMeshPro
   - Name: "PlayerScoreText"
   - Text: "Score: 0"
   - Font Size: 32
   - Position: Below level

5. Create Tower/Level Grid:
   - Right-click MainContentPanel → UI → Scroll View
   - Name: "TowerScrollView"
   - Position: Center
   - Size: Full width, height to fit
   
   - Inside ScrollView → Viewport → Content:
     - Name: "TowerGrid"
     - Add Component → Grid Layout Group
       - Cell Size: 400x200
       - Spacing: 20x20
       - Start Corner: Upper Left
       - Start Axis: Horizontal
       - Child Alignment: Upper Left
       - Constraint: Fixed Column Count
       - Constraint Count: 2

6. Create Tower Button Prefab:
   - Right-click Project → Create → UI → Button - TextMeshPro
   - Name: "TowerButtonPrefab"
   - Save in Assets/Prefabs/UI/
   - Configure:
     - Size: 400x200
     - Text: "Tower Name"
     - Add icon/image if desired
   - **Important:** Drag this prefab to MainMenuUI's Tower Button Prefab field

7. Create Navigation Bar:
   - Right-click MainMenuCanvas → UI → Panel
   - Name: "NavBarPanel"
   - Anchor: Bottom
   - Height: 100
   - Background: Semi-transparent or solid color
   
   - Create Nav Buttons (inside NavBarPanel):
     - **Home Button:**
       - Right-click NavBarPanel → UI → Button - TextMeshPro
       - Name: "HomeButton"
       - Text: "HOME"
       - Size: 200x80
       - Position: Left
     
     - **Recipes Button:**
       - Right-click NavBarPanel → UI → Button - TextMeshPro
       - Name: "RecipesButton"
       - Text: "RECIPES"
       - Size: 200x80
       - Position: Center-left
     
     - **Settings Button:**
       - Right-click NavBarPanel → UI → Button - TextMeshPro
       - Name: "SettingsButton"
       - Text: "SETTINGS"
       - Size: 200x80
       - Position: Center-right
     
     - **Profile Button:**
       - Right-click NavBarPanel → UI → Button - TextMeshPro
       - Name: "ProfileButton"
       - Text: "PROFILE"
       - Size: 200x80
       - Position: Right

8. Create Stamina Display (optional):
   - Right-click MainContentPanel → UI → Panel
   - Name: "StaminaDisplay"
   - Add Component → StaminaDisplayUI
   - Position: Top right

9. Create Controllers:
   - **NavBar Controller:**
     - Right-click Hierarchy → Create Empty
     - Name: "NavBarController"
     - Add Component → NavBarController
     - Assign NavBar reference
   
   - **MainMenu Controller:**
     - Right-click Hierarchy → Create Empty
     - Name: "MainMenuController"
     - Add Component → MainMenuUI
     - Add Component → SceneController
     - Assign all references:
       - Main Menu Panel: MainContentPanel
       - Nav Bar: NavBarController component
       - Welcome Text, Level Text, Score Text
       - Tower Grid: TowerGrid
       - Tower Button Prefab: TowerButtonPrefab
       - Tower Scroll View: TowerScrollView

10. Add to Build Settings:
    - File → Build Settings
    - Add Open Scenes

---

### 5. Other Scenes Setup

For each scene (Gameplay, TowerSelection, Results, Profile, Recipes, Settings):

1. Open the scene
2. Create UI Canvas with same settings
3. Create scene-specific UI elements
4. Create controller GameObject with:
   - SceneController component
   - Scene-specific UI component (e.g., GameplayUI, ProfileUI, etc.)

---

## Quick Setup Template for Any Scene

1. **Create Canvas:**
   ```
   Hierarchy → Right-click → UI → Canvas
   - Name: "[SceneName]Canvas"
   - Render Mode: Screen Space - Overlay
   - Add Canvas Scaler (Scale With Screen Size, 1920x1080)
   - Add Graphic Raycaster
   ```

2. **Create Controller:**
   ```
   Hierarchy → Right-click → Create Empty
   - Name: "[SceneName]Controller"
   - Add Component → SceneController
   - Add Component → [SceneName]UI (if exists)
   ```

3. **Add to Build Settings:**
   ```
   File → Build Settings → Add Open Scenes
   ```

---

## Build Settings Order

Set scenes in this order:
1. **Bootstrap** (index 0) - Entry point
2. **LoginPage** (index 1) - First user screen
3. **MainMenu** (index 2) - Hub with nav bar
4. **LandingPage** (index 3) - Optional/alternative entry
4. **TowerSelection** (index 3)
5. **Gameplay** (index 4)
6. **Results** (index 5)
7. **Profile** (index 6)
8. **Recipes** (index 7)
9. **Settings** (index 8)

---

## Testing Without Full Setup

If you want to test scripts without full UI:

1. **Bootstrap Scene:**
   - Just add BootstrapManager GameObject
   - Scripts will create managers automatically
   - Loading screen will work if you add the UI later

2. **Other Scenes:**
   - Add SceneController to handle Android back button
   - UI scripts will work once you add UI elements

---

## Common Issues & Solutions

**Issue:** UI not showing
- Check Canvas Render Mode is "Screen Space - Overlay"
- Check Canvas is active in hierarchy
- Check UI elements are children of Canvas

**Issue:** UI too small/large
- Adjust Canvas Scaler settings
- Check Reference Resolution matches your target

**Issue:** Buttons not clickable
- Ensure Graphic Raycaster is on Canvas
- Check EventSystem exists (Unity creates automatically)

**Issue:** Scripts not finding UI
- Make sure references are assigned in Inspector
- Check GameObject names match what script expects

---

## Updated Flow

**New User Flow:**
1. Bootstrap (Loading) → LandingPageSplash (optional) → LoginPage → MainMenu → Gameplay/Other screens

**Navigation Flow:**
- MainMenu has NavBar at bottom
- NavBar buttons: Home, Recipes, Settings, Profile
- Home returns to MainMenu
- Other buttons navigate to respective scenes

## Next Steps

1. Set up Bootstrap scene first (most important)
2. Set up LoginPage scene (first user screen)
3. Set up MainMenu scene with NavBar
4. Test the flow: Bootstrap → LoginPage → MainMenu
5. Then set up other scenes (TowerSelection, Gameplay, etc.)
