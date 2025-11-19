# Navigation Settings Panel - Feature Analysis

## Overview

The **Navigation Settings** button (`btnNavigationSettings`) is the **top button on the left panel** in the main FormGPS window during field operation. This button toggles a floating panel (`panelNavigation`) that provides quick access to display and camera controls.

**Location in SourceCode**:
- Button: `btnNavigationSettings` in FormGPS.Designer.cs (line 1797)
- Panel: `panelNavigation` - floating panel that appears/disappears on click
- Click handler: `btnNavigationSettings_Click()` in Controls.Designer.cs (line 1010)
- Icon: `Resources.NavigationSettings`

## Panel Contents

When clicked, the `panelNavigation` floating panel appears with a **2x5 grid of controls**:

### Row 0: Camera Tilt Controls
- **btnTiltDn** (left) - Decrease camera pitch angle (tilt down)
- **btnTiltUp** (right) - Increase camera pitch angle (tilt up)

**Functionality**:
- Adjusts the 3D view pitch angle
- Changes the viewing angle of the field
- Stored in: `Properties.Settings.Default.setDisplay_camPitch` (default: -62 degrees)
- Updates `camera.camPitch`

---

### Row 1: Camera View Mode
- **btn2D** (left) - Switch to 2D overhead view
- **btn3D** (right) - Switch to 3D perspective view

**Functionality**:
- **2D mode**: Pure overhead view, straight down
- **3D mode**: Angled view with perspective
- Affects camera pitch and rendering mode

---

### Row 2: North-Up vs Track-Up & Grid
- **btnN2D** (left) - "North 2D" - Camera rotation mode toggle
- **btnGrid** (right) - Toggle grid display on/off

**Functionality**:
- **btnN2D**: Toggles between north-up and track-up modes
  - North-up: North is always at top of screen
  - Track-up: Vehicle direction is at top of screen (map rotates)
- **btnGrid**: Shows/hides the grid overlay on the map
  - Controlled by `isGridOn` variable
  - Renders coordinate grid lines in OpenGL
  - Only enabled when field/job is active

---

### Row 3: Day/Night Mode & GPS Hz Display
- **btnDayNightMode** (left) - Toggle between day and night color schemes
- **lblHz** (right) - Display showing GPS update rate (Hz)

**Functionality**:
- **Day/Night**: Switches entire UI color scheme
  - Day mode: Light colors, high contrast
  - Night mode: Dark colors, reduced brightness for night operation
  - Controlled by `isDay` variable
  - Changes frame colors, text colors, section colors, field background
  - Stored in: `Settings.Default.setDisplay_isDayMode`
- **lblHz**: Read-only label showing GPS data rate (e.g., "10 Hz")

---

### Row 4: Brightness Controls
- **btnBrightnessDn** (left) - Decrease screen brightness
- **btnBrightnessUp** (right) - Increase screen brightness

**Functionality**:
- Adjusts monitor hardware brightness via WMI
- Shows current brightness percentage on btnBrightnessDn (e.g., "20%")
- Increments/decrements brightness by a fixed amount
- Only works with compatible monitors (uses Windows WMI API)
- Controlled by `displayBrightness` object (CWindowsSettingsBrightnessController)
- Stored in: `Settings.Default.setDisplay_brightness` (0-100)
- If monitor doesn't support WMI control, shows "??" and buttons are disabled

---

## Code Locations

### Button Definition
**File**: `SourceCode/GPS/Forms/FormGPS.Designer.cs:1784-1804`
```csharp
this.btnNavigationSettings.Image = global::AgOpenGPS.Properties.Resources.NavigationSettings;
this.btnNavigationSettings.Size = new System.Drawing.Size(72, 62);
// Located at row 1 in panelLeft
```

### Click Handler
**File**: `SourceCode/GPS/Forms/Controls.Designer.cs:1010-1046`
```csharp
private void btnNavigationSettings_Click(object sender, EventArgs e)
{
    // Close any open GPS data or field data forms
    Form f = Application.OpenForms["FormGPSData"];
    if (f != null) { f.Focus(); f.Close(); }

    Form f1 = Application.OpenForms["FormFieldData"];
    if (f1 != null) { f1.Focus(); f1.Close(); }

    // Position and toggle panel
    panelNavigation.Location = new System.Drawing.Point(90, 100);

    if (panelNavigation.Visible)
    {
        panelNavigation.Visible = false;
    }
    else
    {
        panelNavigation.Visible = true;
        navPanelCounter = 2;  // Auto-hide timer

        // Update brightness display
        if (displayBrightness.isWmiMonitor)
            btnBrightnessDn.Text = (displayBrightness.GetBrightness().ToString()) + "%";
        else
            btnBrightnessDn.Text = "??";
    }

    // Enable/disable grid button based on job status
    if (isJobStarted) btnGrid.Enabled = true;
    else btnGrid.Enabled = false;
}
```

### Panel Definition
**File**: `SourceCode/GPS/Forms/FormGPS.Designer.cs:1265-1280`
```csharp
this.panelNavigation.Controls.Add(this.btnTiltDn, 0, 0);
this.panelNavigation.Controls.Add(this.btnTiltUp, 1, 0);
this.panelNavigation.Controls.Add(this.btnBrightnessDn, 0, 4);
this.panelNavigation.Controls.Add(this.btnBrightnessUp, 1, 4);
this.panelNavigation.Controls.Add(this.btnDayNightMode, 0, 3);
this.panelNavigation.Controls.Add(this.lblHz, 1, 3);
this.panelNavigation.Controls.Add(this.btn3D, 1, 1);
this.panelNavigation.Controls.Add(this.btn2D, 0, 1);
this.panelNavigation.Controls.Add(this.btnGrid, 1, 2);
this.panelNavigation.Controls.Add(this.btnN2D, 0, 2);
```

---

## Auto-Hide Behavior

The panel has an auto-hide feature controlled by `navPanelCounter`:
- When panel is opened, `navPanelCounter` is set to 2
- The watchdog timer (`tmrWatchdog_tick` at 250ms interval) decrements this counter
- After ~6 seconds of inactivity, the panel automatically hides
- Any interaction with the panel resets the counter

**Code in tmrWatchdog_tick**:
```csharp
if (navPanelCounter > 0) navPanelCounter--;
if (navPanelCounter == 0 && panelNavigation.Visible) panelNavigation.Visible = false;
```

---

## Settings/Variables Affected

| Control | Variable | Settings Key | Type | Description |
|---------|----------|--------------|------|-------------|
| Tilt Up/Dn | `camera.camPitch` | `setDisplay_camPitch` | double | Camera pitch angle (default: -62) |
| 2D/3D | `camera` mode | N/A | Camera state | 2D vs 3D rendering mode |
| N2D | Map rotation | N/A | bool | North-up vs track-up |
| Grid | `isGridOn` | N/A | bool | Show/hide grid overlay |
| Day/Night | `isDay` / `isDayTime` | `setDisplay_isDayMode` | bool | Color scheme mode |
| Brightness | `displayBrightness` | `setDisplay_brightness` | int (0-100) | Hardware brightness level |

---

## Visual Layout

```
┌─────────────────────┐
│ Navigation Settings │
├──────────┬──────────┤
│  TiltDn  │  TiltUp  │  Row 0: Camera Pitch
├──────────┼──────────┤
│   2D     │    3D    │  Row 1: View Mode
├──────────┼──────────┤
│   N2D    │   Grid   │  Row 2: Rotation & Grid
├──────────┼──────────┤
│ Day/Night│  10 Hz   │  Row 3: Colors & GPS Rate
├──────────┼──────────┤
│ Bright-  │ Bright+  │  Row 4: Brightness
│   20%    │          │
└──────────┴──────────┘
```

---

## Migration to AgValoniaGPS

### Complexity: MEDIUM

**Why Medium Complexity:**
- ✅ Most controls are simple toggles
- ✅ Camera controls already partially exist (zoom via mouse wheel)
- ⚠️ Need to implement camera pitch (tilt) control
- ⚠️ Need 2D/3D view mode switching
- ⚠️ Day/Night color scheme system
- ⚠️ Hardware brightness control is platform-specific (Windows WMI)
- ⚠️ Need floating panel UI pattern in Avalonia

### Implementation Approach

#### 1. Create NavigationSettingsViewModel
```csharp
// AgValoniaGPS.ViewModels/NavigationSettingsViewModel.cs
public class NavigationSettingsViewModel : ReactiveObject
{
    private bool _isGridVisible;
    public bool IsGridVisible
    {
        get => _isGridVisible;
        set => this.RaiseAndSetIfChanged(ref _isGridVisible, value);
    }

    private bool _isDayMode = true;
    public bool IsDayMode
    {
        get => _isDayMode;
        set => this.RaiseAndSetIfChanged(ref _isDayMode, value);
    }

    private double _cameraPitch = -62;
    public double CameraPitch
    {
        get => _cameraPitch;
        set => this.RaiseAndSetIfChanged(ref _cameraPitch, value);
    }

    private int _brightness = 40;
    public int Brightness
    {
        get => _brightness;
        set => this.RaiseAndSetIfChanged(ref _brightness, value);
    }

    // Commands
    public ReactiveCommand<Unit, Unit> TiltUpCommand { get; }
    public ReactiveCommand<Unit, Unit> TiltDownCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleDayNightCommand { get; }
    public ReactiveCommand<Unit, Unit> Toggle2D3DCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleGridCommand { get; }
    public ReactiveCommand<Unit, Unit> IncreaseBrightnessCommand { get; }
    public ReactiveCommand<Unit, Unit> DecreaseBrightnessCommand { get; }
}
```

#### 2. Update OpenGLMapControl
```csharp
// Add camera pitch property
public double CameraPitch { get; set; } = -62;
public bool Is2DMode { get; set; } = false;

// Update render logic to use camera pitch
// Apply pitch transformation in view matrix
```

#### 3. Create Floating Panel in MainWindow
```xml
<!-- AgValoniaGPS.Desktop/Views/MainWindow.axaml -->
<Panel x:Name="NavigationPanel"
       IsVisible="{Binding IsNavigationPanelVisible}"
       HorizontalAlignment="Left"
       VerticalAlignment="Top"
       Margin="90,100,0,0"
       ZIndex="100">
    <Border Background="#CC1E1E1E" CornerRadius="8" Padding="10">
        <Grid RowDefinitions="Auto,Auto,Auto,Auto,Auto" ColumnDefinitions="*,*">
            <!-- Row 0: Tilt -->
            <Button Grid.Row="0" Grid.Column="0" Content="Tilt-" Command="{Binding TiltDownCommand}"/>
            <Button Grid.Row="0" Grid.Column="1" Content="Tilt+" Command="{Binding TiltUpCommand}"/>

            <!-- Row 1: View Mode -->
            <Button Grid.Row="1" Grid.Column="0" Content="2D" Command="{Binding Toggle2DCommand}"/>
            <Button Grid.Row="1" Grid.Column="1" Content="3D" Command="{Binding Toggle3DCommand}"/>

            <!-- Row 2: Grid -->
            <Button Grid.Row="2" Grid.Column="0" Content="North" Command="{Binding ToggleNorthUpCommand}"/>
            <Button Grid.Row="2" Grid.Column="1" Content="Grid" Command="{Binding ToggleGridCommand}"/>

            <!-- Row 3: Day/Night -->
            <Button Grid.Row="3" Grid.Column="0" Command="{Binding ToggleDayNightCommand}">
                <Image Source="{Binding DayNightIcon}"/>
            </Button>
            <TextBlock Grid.Row="3" Grid.Column="1" Text="{Binding GpsHz, StringFormat='{}{0} Hz'}"/>

            <!-- Row 4: Brightness -->
            <Button Grid.Row="4" Grid.Column="0" Content="{Binding Brightness, StringFormat='{}Bright- {0}%'}"
                    Command="{Binding DecreaseBrightnessCommand}"/>
            <Button Grid.Row="4" Grid.Column="1" Content="Bright+"
                    Command="{Binding IncreaseBrightnessCommand}"/>
        </Grid>
    </Border>
</Panel>
```

#### 4. Day/Night Theme System
Create theme resource dictionaries:
- `Themes/DayTheme.axaml` - Light colors
- `Themes/NightTheme.axaml` - Dark colors
- Switch themes dynamically via `Application.Current.Resources.MergedDictionaries`

#### 5. Platform-Specific Brightness Control
```csharp
// AgValoniaGPS.Services/BrightnessService.cs (Windows-specific)
public interface IBrightnessService
{
    bool IsSupported { get; }
    int GetBrightness();
    void SetBrightness(int level);
}

// Windows implementation using WMI (like original)
// Linux/Mac: Return IsSupported = false or use platform-specific APIs
```

---

## What You Already Have

In AgValoniaGPS Phase 7:
- ✅ Grid rendering in OpenGLMapControl
- ✅ Camera zoom (mouse wheel)
- ✅ Basic camera transform (translation for pan)

---

## What's Needed

1. **Camera pitch/tilt** - Add pitch angle to camera transform
2. **2D/3D view modes** - Toggle perspective vs orthographic
3. **Day/Night theme system** - Resource dictionary switching
4. **Floating panel UI** - Avalonia Popup or Panel with z-index
5. **Brightness control** - Platform-specific service (Windows WMI)
6. **Grid toggle** - Wire existing grid render to on/off control
7. **GPS Hz display** - Calculate update rate from GPS service
8. **Auto-hide behavior** - Timer-based panel visibility

---

## Priority Assessment

### High Priority
1. **Grid Toggle** - Easy win, already rendered
2. **Day/Night Mode** - Important for usability
3. **Camera Pitch/Tilt** - Needed for 3D view

### Medium Priority
4. **2D/3D View Toggle** - Nice to have
5. **Floating Panel UI** - For complete UX

### Low Priority
6. **Brightness Control** - Platform-specific, can stub for now
7. **GPS Hz Display** - Informational only

---

## Summary

The **Navigation Settings** button provides quick access to visual and camera controls during field operation:

- **Camera controls**: Tilt, 2D/3D, rotation mode
- **Display toggles**: Grid, Day/Night colors
- **Brightness**: Hardware monitor control
- **Info display**: GPS update rate

This is a **good candidate for incremental implementation** since controls are independent and can be added one at a time. Start with grid toggle and day/night mode, then add camera controls.

**Estimated Complexity**: Medium (camera transforms + theme system + floating UI)
**Estimated Effort**: 2-3 days for full implementation
**Dependencies**: None - can be done standalone
