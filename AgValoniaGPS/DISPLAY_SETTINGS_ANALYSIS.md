# Display Settings Button - Feature Analysis

## Overview

The **Display Settings** button is the top button on the left menu in FormConfig (Settings dialog) in the old AgOpenGPS. This document analyzes all functions and settings controlled by this menu.

**Location in SourceCode**:
- Button: `SourceCode/GPS/Forms/Settings/FormConfig.Designer.cs` - `btnDisplay`
- Tab content: `tabDisplay` in FormConfig
- Click handler: `btnDisplay_Click()` in `ConfigMenu.Designer.cs`

## Display Settings Categories

### 1. Units Selection (Metric vs Imperial)
**Controls**:
- `rbtnDisplayMetric` - Radio button for metric units (meters, km/h, hectares)
- `rbtnDisplayImperial` - Radio button for imperial units (feet, mph, acres)

**Settings**:
- `isMetric` (bool) - Global metric/imperial flag

**Functionality**:
- Changes all distance, speed, and area measurements throughout the app
- Affects labels, speedometer, field area calculations, work rate displays

---

### 2. Grid Display
**Controls**:
- `chkDisplayGrid` - Toggle grid overlay on map

**Settings**:
- `isGridOn` (bool) - Show/hide grid lines on field map

**Functionality**:
- Renders coordinate grid lines on the OpenGL map
- Helps with distance estimation and orientation

---

### 3. Extra Guidelines
**Controls**:
- `chkDisplayExtraGuides` - Toggle extra guidance lines
- `nudNumGuideLines` - NumericUpDown for number of guide lines (1-5000)

**Settings**:
- `isExtraGuidelinesOn` (bool) - Show parallel guidelines alongside main AB line
- `numGuideLines` (int) - Number of parallel lines to display

**Functionality**:
- Shows additional parallel lines on either side of the AB line
- Helps visualize the working area and plan passes
- Spacing matches implement width

---

### 4. Speedo (Speedometer)
**Controls**:
- `chkDisplaySpeedo` - Toggle speedometer display

**Settings**:
- `isSpeedoOn` (bool) - Show/hide speedometer on main screen

**Functionality**:
- Displays large digital speedometer on main screen
- Shows current speed in selected units (km/h or mph)
- Visual reference for maintaining consistent speed

---

### 5. Polygons Display
**Controls**:
- `chkDisplayPolygons` - Toggle polygon boundary display

**Settings**:
- `isDrawPolygons` (bool) - Render field boundaries as filled polygons

**Functionality**:
- Fills field boundaries with semi-transparent color
- Helps visualize field area vs outside area
- Can toggle between outlined and filled boundaries

---

### 6. Keyboard Display
**Controls**:
- `chkDisplayKeyboard` - Toggle on-screen keyboard

**Settings**:
- `setDisplay_isKeyboardOn` (bool) - Show on-screen keyboard when text input needed

**Functionality**:
- Shows touch-friendly on-screen keyboard for tablet/touchscreen use
- Auto-appears when text fields are focused
- Can be disabled if using physical keyboard

---

### 7. Start Fullscreen
**Controls**:
- `chkDisplayStartFullScreen` - Auto-start in fullscreen mode

**Settings**:
- `setDisplay_isStartFullScreen` (bool) - Launch app in fullscreen on startup

**Functionality**:
- Removes window borders and maximizes to full screen on launch
- Useful for dedicated in-cab displays
- Can press F11 to toggle fullscreen anytime

---

### 8. Field Texture
**Controls**:
- `chkDisplayFloor` - Toggle background texture/image

**Settings**:
- `setDisplay_isTextureOn` (bool) - Show background field texture or aerial imagery

**Functionality**:
- Displays satellite/aerial imagery as background (if available)
- Can show Bing Maps tiles for realistic field view
- Toggle between plain background and textured for performance

---

### 9. Brightness Control
**Controls**:
- `chkDisplayBrightness` - Enable automatic brightness control

**Settings**:
- `setDisplay_isBrightnessOn` (bool) - Auto-adjust screen brightness
- `setDisplay_brightness` (int) - Current brightness level (0-100)
- `setDisplay_brightnessSystem` (int) - System brightness level before AOG control

**Functionality**:
- Automatically adjusts monitor brightness based on day/night mode
- Uses WMI to control hardware brightness
- Only works with compatible monitors
- Two buttons on main screen: `btnBrightnessUp` / `btnBrightnessDn`

---

### 10. Svenn Arrow
**Controls**:
- `chkSvennArrow` - Toggle Svenn arrow guidance indicator

**Settings**:
- `setDisplay_isSvennArrowOn` (bool) - Show Svenn arrow

**Functionality**:
- Shows a directional arrow indicating which way to steer
- Named after contributor Svenn
- Alternative/supplement to lightbar guidance
- Points left/right based on cross-track error

---

### 11. Elevation Display
**Controls**:
- `chkDisplayLogElevation` - Log and display elevation data

**Settings**:
- `setDisplay_isLogElevation` (bool) - Record elevation with position data

**Functionality**:
- Records altitude from GPS alongside position
- Can display elevation map/heatmap of field
- Useful for drainage planning and topography analysis
- Shows elevation changes during operation

---

### 12. Direction Markers
**Controls**:
- `chkDirectionMarkers` - Toggle direction indicators on field

**Settings**:
- `isDirectionOn` (bool) - Show directional arrows on coverage map

**Functionality**:
- Draws small arrows on the applied sections showing pass direction
- Helps identify which way each pass was made
- Useful for planning next season's direction (perpendicular)

---

### 13. Section Lines
**Controls**:
- `chkSectionLines` - Show section boundary lines

**Settings**:
- `setDisplay_isSectionLinesOn` (bool) - Draw vertical lines between sections

**Functionality**:
- Renders vertical lines separating each section of the implement
- Helps visualize section control zones
- Shows which sections are on/off during operation

---

### 14. Line Smooth
**Controls**:
- `chkLineSmooth` - Toggle line smoothing/anti-aliasing

**Settings**:
- `setDisplay_isLineSmooth` (bool) - Enable anti-aliased line rendering

**Functionality**:
- Smooths guidance lines and boundaries using anti-aliasing
- Makes lines look better but can reduce performance on older hardware
- OpenGL line smoothing setting (GL_LINE_SMOOTH)

---

### 15. Headland Distance Display
**Controls**:
- `chkboxHeadlandDist` - Show distance to headland boundary

**Settings**:
- `isHeadlandDistance` (bool) - Display headland proximity indicator

**Functionality**:
- Shows distance remaining before reaching headland boundary
- Helps prepare for U-turn and headland operations
- Visual and numeric indicator

---

## Additional Display-Related Settings

### Camera Settings
**Settings**:
- `setDisplay_camZoom` (double) - Default camera zoom level (default: 9.0)
- `setDisplay_camPitch` (double) - Camera pitch angle in degrees (default: -62)
- `setDisplay_camSmooth` (int) - Camera movement smoothing (0-100, default: 50)

**Functionality**:
- Controls 3D view camera position and movement
- Higher smoothing = slower, smoother camera following
- Zoom affects how much of field is visible

---

### Color Scheme (Day/Night Mode)
**Settings**:
- `setDisplay_isDayMode` (bool) - Current mode (Day = true, Night = false)
- `setDisplay_colorDayFrame` (Color) - Day mode background color
- `setDisplay_colorNightFrame` (Color) - Night mode background color
- `setDisplay_colorSectionsDay` (Color) - Applied sections color (day)
- `setDisplay_colorFieldDay` (Color) - Field background color (day)
- `setDisplay_colorFieldNight` (Color) - Field background color (night)
- `setDisplay_colorTextDay` (Color) - Text color for day mode
- `setDisplay_colorTextNight` (Color) - Text color for night mode
- `setDisplay_customColors` (string) - Custom color palette for sections

**Functionality**:
- Switches entire UI between high-contrast day mode and dim night mode
- Controlled by `btnDayNightMode` button on main screen
- Auto-adjusts all colors, text, and graphics

---

### Vehicle Display
**Settings**:
- `setDisplay_colorVehicle` (Color) - Vehicle indicator color
- `setDisplay_vehicleOpacity` (int) - Vehicle transparency (0-100)
- `setDisplay_isVehicleImage` (bool) - Use textured vehicle vs simple shape

**Functionality**:
- Controls how the vehicle/tractor is rendered on map
- Can use image texture or simple colored shape
- Opacity allows seeing through vehicle to guidance lines

---

### Other Display Settings
**Settings**:
- `setDisplay_lineWidth` (int) - Width of guidance lines in pixels (default: 2)
- `setDisplay_lightbarCmPerPixel` (int) - Lightbar scale (default: 5)
- `setDisplay_buttonOrder` (string) - Order of buttons on right panel (e.g., "0,1,2,3,4,5,6,7")

---

## Properties/Settings That Control Display Features

All stored in `Properties.Settings.Default`:

| Setting Property | Type | Default | Description |
|-----------------|------|---------|-------------|
| `setDisplay_isStartFullScreen` | bool | false | Launch in fullscreen |
| `setDisplay_isKeyboardOn` | bool | true | Show on-screen keyboard |
| `setDisplay_isTextureOn` | bool | true | Show field texture/imagery |
| `setDisplay_isBrightnessOn` | bool | false | Auto brightness control |
| `setDisplay_brightness` | int | 40 | Current brightness level |
| `setDisplay_brightnessSystem` | int | 40 | System brightness backup |
| `setDisplay_isSvennArrowOn` | bool | false | Show Svenn arrow |
| `setDisplay_isLogElevation` | bool | false | Log elevation data |
| `setDisplay_isSectionLinesOn` | bool | true | Show section lines |
| `setDisplay_isLineSmooth` | bool | false | Anti-aliased lines |
| `setDisplay_isDayMode` | bool | true | Day vs night mode |
| `setDisplay_camZoom` | double | 9.0 | Camera zoom level |
| `setDisplay_camPitch` | double | -62 | Camera pitch angle |
| `setDisplay_camSmooth` | int | 50 | Camera smoothing |
| `setDisplay_lineWidth` | int | 2 | Guidance line width |
| `setDisplay_vehicleOpacity` | int | 100 | Vehicle transparency |
| `setDisplay_isVehicleImage` | bool | true | Use vehicle texture |
| `setDisplay_buttonOrder` | string | "0,1,2,3,4,5,6,7" | Button order |
| `setDisplay_customColors` | string | (color list) | Custom section colors |
| `setDisplay_customSectionColors` | string | (color list) | Section palette |
| `setDisplay_isAutoStartAgIO` | bool | true | Auto-start AgIO |
| `setDisplay_isAutoOffAgIO` | bool | true | Auto-close AgIO |
| `setDisplay_isShutdownWhenNoPower` | bool | false | Shutdown on battery |
| `setDisplay_isHardwareMessages` | bool | false | Show hardware messages |
| `setDisplay_isTermsAccepted` | bool | false | Terms accepted flag |

---

## Main Form Variables Referenced

In addition to Properties.Settings, FormGPS has these display-related variables:

```csharp
public bool isMetric;               // Metric vs imperial
public bool isDay;                  // Day mode flag
public bool isGridOn;               // Grid display
public bool isExtraGuidelinesOn;    // Extra guide lines
public bool isSpeedoOn;             // Speedometer
public bool isDrawPolygons;         // Filled polygons
public bool isKeyboardOn;           // On-screen keyboard
public bool isTextureOn;            // Field texture
public bool isBrightnessOn;         // Auto brightness
public bool isSvennArrowOn;         // Svenn arrow
public bool isLogElevation;         // Elevation logging
public bool isDirectionOn;          // Direction markers
public bool isSectionlinesOn;       // Section lines
public bool isLineSmooth;           // Line anti-aliasing
public bool isHeadlandDistance;     // Headland distance
public int numGuideLines;           // Number of extra guides
```

---

## Migration Priority for AgValoniaGPS

### High Priority (Core Display Features)
1. ✅ **Grid Display** - Already have grid in OpenGLMapControl
2. ✅ **Metric/Imperial Units** - Needed for all measurements
3. **Day/Night Mode** - Color scheme switching
4. **Speedometer Display** - Speed indicator on main screen
5. **Camera Zoom/Pitch** - Already have zoom, need pitch control

### Medium Priority (Guidance Visualization)
6. **Extra Guidelines** - Parallel lines alongside AB line (requires AB line first)
7. **Direction Markers** - Show pass direction on coverage map
8. **Section Lines** - Section boundary indicators
9. **Svenn Arrow** - Steering direction indicator

### Low Priority (Optional/Convenience)
10. **Fullscreen Mode** - Avalonia supports this natively
11. **Polygons Display** - Filled vs outlined boundaries
12. **Field Texture/Imagery** - Background satellite imagery
13. **Elevation Display** - Topography visualization
14. **Line Smooth** - Anti-aliasing (OpenGL ES might handle differently)
15. **Headland Distance** - Proximity indicator
16. **Brightness Control** - Hardware brightness (platform-specific)
17. **On-Screen Keyboard** - Avalonia might have alternatives

---

## Implementation Notes for AgValoniaGPS

### Quick Wins (Already Partially Done)
- ✅ Grid rendering exists in OpenGLMapControl
- ✅ Camera zoom exists (mouse wheel)
- ✅ Vehicle rendering with texture exists

### Service Architecture Needed
```csharp
// AgValoniaGPS.Services/IDisplaySettingsService.cs
public interface IDisplaySettingsService
{
    // Units
    bool IsMetric { get; set; }
    event EventHandler<UnitsChangedEventArgs> UnitsChanged;

    // Display toggles
    bool IsGridOn { get; set; }
    bool IsSpeedoOn { get; set; }
    bool IsDayMode { get; set; }
    bool IsExtraGuideLinesOn { get; set; }
    int NumGuideLines { get; set; }
    bool IsDirectionMarkersOn { get; set; }
    bool IsSectionLinesOn { get; set; }
    bool IsLineSmoothOn { get; set; }
    bool IsSvennArrowOn { get; set; }
    bool IsHeadlandDistanceOn { get; set; }
    bool IsPolygonsFilledOn { get; set; }
    bool IsElevationOn { get; set; }
    bool IsFieldTextureOn { get; set; }

    // Camera
    double CameraZoom { get; set; }
    double CameraPitch { get; set; }
    int CameraSmoothing { get; set; }

    // Colors
    Color DayFrameColor { get; set; }
    Color NightFrameColor { get; set; }
    Color VehicleColor { get; set; }
    int VehicleOpacity { get; set; }

    // Methods
    void ToggleDayNightMode();
    void SaveSettings();
    void LoadSettings();
}
```

### UI Components Needed
- Settings dialog (similar to DataIODialog pattern)
- Toggle buttons for each display option
- Numeric input for guide line count
- Radio buttons for metric/imperial
- Color pickers for customization

---

## Summary

The **Display Settings** menu controls **15+ visual display options** plus units selection and numerous color/appearance settings. It's a relatively straightforward feature to implement in AgValoniaGPS because:

1. Most settings are simple boolean toggles
2. No complex business logic - just show/hide rendering features
3. Settings are independent and can be implemented incrementally
4. Can reuse existing OpenGL rendering infrastructure

**Recommended migration order:**
1. Create DisplaySettingsService with settings persistence
2. Add Units selection (Metric/Imperial) - affects all measurements
3. Add Day/Night color scheme toggle
4. Add Speedometer display to main window
5. Add other toggles as needed for field/guidance visualization

This would be a **good Phase 8 candidate** since it's lower complexity than field boundaries or guidance lines, and provides immediate visual/UX improvements to the application.
