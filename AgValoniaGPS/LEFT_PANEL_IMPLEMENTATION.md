# Left Panel Implementation Checklist

**Status**: In Progress (3/8 buttons complete)
**Target**: Implement complete left navigation panel for AgValoniaGPS
**Last Updated**: 2025-01-23

---

## Overview

The left panel is the **main navigation anchor** for AgValoniaGPS. It's a single-column vertical stack of 8 buttons (4 direct action, 4 dropdown menus) that provide access to all major application features.

**Design Pattern**: Persistent left sidebar with icon-only buttons (64x64px), matching WinForms FormGPS `panelLeft` structure.

---

## Button Structure (Top to Bottom)

### ✅ 0. File/Application Menu (Dropdown)
- **Icon**: `fileMenu.png` (hamburger menu ☰)
- **Type**: Dropdown menu
- **Function**: Application-level settings and utilities
- **Sub-menu items**:
  - [ ] Profile → New Profile
  - [ ] Profile → Load Profile
  - [ ] Language selection
  - [ ] Simulator On/Off toggle
  - [ ] Enter Sim Coords
  - [ ] Kiosk Mode toggle
  - [ ] Reset ALL
  - [ ] About dialog
  - [ ] AgShare API
  - [ ] Help

**Notes**: Should have visual separator below it to distinguish from operational buttons.

---

### ✅ 1. Navigation Settings (Direct Button)
- **Icon**: `NavigationSettings.png`
- **Type**: Direct action button
- **Function**: Toggle navigation/display settings panel
- **Target**: Opens floating panel with:
  - [ ] Camera tilt controls
  - [ ] 2D/3D toggle
  - [ ] North-up/Track-up toggle
  - [ ] Grid display toggle
  - [ ] Day/Night mode
  - [ ] GPS Hz display
  - [ ] Brightness controls

**Current Status**: ✅ Partially implemented at `MainWindow.axaml:129` (Navigation Settings Panel exists)

---

### ✅ 2. Tools (Floating Panel) - **RENAMED from Special Functions**
- **Icon**: `SpecialFunctions.png`
- **Type**: Floating panel (2-column layout, 5 items per column)
- **Function**: Advanced tools, wizards, and charts
- **Panel items** (10 items total, matching AgOpenGPS 6.8.0):
  - **Left Column - Wizards & Charts:**
    - [x] Steer Wizard - Icon: `WizardWand.png`
    - [x] Steer Chart - Icon: `AutoSteerOn.png`
    - [x] Heading Chart - Icon: `ConS_SourcesHeading.png`
    - [x] XTE Chart - Icon: `AutoManualIsAuto.png`
    - [x] Roll Correction Chart - Icon: `ConS_SourcesRoll.png`
  - **Right Column - Tools:**
    - [x] Boundary Tool - Icon: `Boundary.png`
    - [x] Smooth AB Curve - Icon: `ABSmooth.png`
    - [x] Delete Contour Paths - Icon: `TrashContourRef.png`
    - [x] Offset Fix - Icon: `YouTurnReverse.png`
    - [x] Log Viewer - Icon: `ABTracks.png`

**Current Status**: ✅ **COMPLETE** - Implemented at `MainWindow.axaml:408-548`
**Layout**: 2-column Grid (5 rows × 2 columns), aligned with left panel top (Canvas.Top="100")
**ViewModel**: `IsToolsPanelVisible`, `ToggleToolsPanelCommand` in MainViewModel.cs
**Drag Handlers**: `ToolsPanel_PointerPressed/Moved/Released` in MainWindow.axaml.cs
**WinForms Reference**: `statusStripLeft` → `toolStripDropDownButton4` (FormGPS.Designer.cs:947-1010)

---

### ✅ 3. Configuration (Dropdown)
- **Icon**: `Settings48.png`
- **Type**: Dropdown menu
- **Function**: Vehicle, implement, and system configuration
- **Sub-menu items**:
  - [ ] Configuration (Vehicle setup)
  - [ ] Auto Steer settings
  - [ ] View All Settings
  - [ ] Directories
  - [ ] GPS Data
  - [ ] Colors
  - [ ] Multi-Section Colors
  - [ ] HotKeys

**WinForms Reference**: `statusStrip2` → `toolStripDropDownButton1` (FormGPS.Designer.cs:1673-1780)

---

### ✅ 4. Job Menu (Direct Button)
- **Icon**: `JobActive.png`
- **Type**: Direct action button
- **Function**: Field/job selection and management
- **Opens**: Job management dialog
  - [ ] Open field
  - [ ] Save field
  - [ ] Field list
  - [ ] Job name/date settings
  - [ ] Field statistics

**WinForms Reference**: `btnJobMenu` (FormGPS.Designer.cs:1868)

---

### ✅ 5. Field Tools (Dropdown)
- **Icon**: `FieldTools.png`
- **Type**: Dropdown menu
- **Function**: Field-specific operations (boundaries, headland, tram)
- **Sub-menu items**:
  - [ ] Boundaries
  - [ ] Headland
  - [ ] Headland Build
  - [ ] Tram Lines
  - [ ] Trams Multi
  - [ ] Delete Applied
  - [ ] Flag by Lat/Lon
  - [ ] Recorded Path

**WinForms Reference**: `statusStrip1` → `toolStripBtnFieldTools` (FormGPS.Designer.cs:1887-1990)

---

### ✅ 6. AutoSteer Config (Direct Button)
- **Icon**: `AutoSteerConf.png`
- **Type**: Direct action button
- **Function**: Open AutoSteer configuration dialog
- **Opens**: Steering parameters setup
  - [ ] Gain settings
  - [ ] Look-ahead distance
  - [ ] Minimum speed
  - [ ] Stanley/Pure Pursuit mode
  - [ ] Wheel/axle settings

**WinForms Reference**: `btnAutoSteerConfig` (FormGPS.Designer.cs:1818)

---

### ✅ 7. AgIO (Direct Button)
- **Icon**: `AgIO.png`
- **Type**: Direct action button
- **Function**: Launch/connect to AgIO communication hub
- **Action**: Start AgIO application or open connection dialog

**WinForms Reference**: `btnStartAgIO` (FormGPS.Designer.cs:1844)

---

## Implementation Tasks

### Phase 1: Asset Preparation
- [x] Verify all icon files exist in Button Images Library
- [x] Copy icons to AgValoniaGPS.Desktop/Assets/Icons/
- [x] Update .csproj to include icon assets (already configured)
- [ ] Create icon resource dictionary (optional, for cleaner XAML)

### Phase 2: XAML Structure
- [x] Design left panel container (StackPanel in FloatingPanel Border)
- [x] Create button style template matching WinForms aesthetic (LeftPanelButton)
- [ ] Implement dropdown/flyout menu system
- [x] Add separators between button groups (after File Menu)
- [x] Ensure proper sizing (64x64 button size)
- [x] Add hover/pressed visual states
- [ ] Implement z-index layering for dropdowns
- [ ] Test visual layout with running application

### Phase 3: ViewModel Layer
- [ ] Create LeftPanelViewModel (or extend MainViewModel)
- [ ] Add ReactiveCommand for each direct button (4 commands)
- [ ] Add ReactiveCommand for each dropdown menu (4 commands)
- [ ] Create observable properties for menu visibility states
- [ ] Add event handlers for sub-menu item clicks
- [ ] Implement command enable/disable logic based on app state

### Phase 4: Service Layer (if needed)
- [ ] Create NavigationService for panel state management
- [ ] Implement menu state persistence (which panels are open)
- [ ] Add analytics/telemetry for button usage (optional)

### Phase 5: Dialog/Panel Implementation
- [ ] Implement dialogs for direct action buttons
  - [ ] Navigation Settings panel (partially exists)
  - [ ] Job Menu dialog
  - [ ] AutoSteer Config dialog
  - [ ] AgIO launcher/connection dialog
- [ ] Create placeholder dialogs for unimplemented features
- [ ] Wire up commands to open dialogs

### Phase 6: Dropdown Menu Content
- [ ] Implement File Menu items
- [ ] Implement Special Functions items
- [ ] Implement Configuration items
- [ ] Implement Field Tools items
- [ ] Add sub-menu navigation where needed

### Phase 7: Integration & Testing
- [ ] Test button clicks and menu navigation
- [ ] Verify icons load correctly
- [ ] Test keyboard navigation (tab order)
- [ ] Test with different window sizes
- [ ] Ensure dropdowns don't clip at screen edges
- [ ] Test state persistence across app sessions

### Phase 8: Polish
- [ ] Add tooltips to all buttons
- [ ] Implement smooth open/close animations for dropdowns
- [ ] Add sound effects (optional, matching WinForms)
- [ ] Ensure proper focus handling
- [ ] Add accessibility labels (screen reader support)

---

## Technical Details

### Icon File Locations
**Source**: `C:\Users\chrisk\Documents\AgValoniaGPS2\AgValoniaGPS\Button Images Library\`
**Target**: `AgValoniaGPS.Desktop/Assets/Icons/`

| Button | Icon File | Status |
|--------|-----------|--------|
| File Menu | fileMenu.png | ✅ Verified |
| Navigation Settings | NavigationSettings.png | ✅ Verified |
| Special Functions | SpecialFunctions.png | ✅ Verified |
| Configuration | Settings48.png | ✅ Verified |
| Job Menu | JobActive.png | ✅ Verified |
| Field Tools | FieldTools.png | ✅ Verified |
| AutoSteer Config | AutoSteerConf.png | ✅ Verified |
| AgIO | AgIO.png | ✅ Verified |

### WinForms References
- **Main Panel**: `FormGPS.Designer.cs:1647-1672` (panelLeft TableLayoutPanel)
- **Button Definitions**: Lines 144-147, 1654-1657, 1782-1868
- **Dropdown Menus**: Lines 947-1780

### Avalonia Control Choices
- **Panel Container**: `StackPanel` (Vertical orientation) or `Grid` with row definitions
- **Buttons**: Standard `Button` controls with custom style
- **Dropdowns**: `Button` with `Flyout` containing `Menu` or `StackPanel`
- **Icons**: `Image` control with `Source` binding to asset paths
- **Separators**: `Separator` control with custom styling

### XAML Structure Pattern
```xml
<StackPanel Classes="LeftPanel">
    <!-- File Menu -->
    <Button Classes="LeftPanelButton">
        <Button.Flyout>
            <MenuFlyout>
                <!-- Sub-menu items -->
            </MenuFlyout>
        </Button.Flyout>
    </Button>

    <Separator Classes="PanelSeparator"/>

    <!-- Navigation Settings -->
    <Button Classes="LeftPanelButton" Command="{Binding NavigationSettingsCommand}"/>

    <!-- ... more buttons ... -->
</StackPanel>
```

### ViewModel Pattern
```csharp
public class MainViewModel : ViewModelBase
{
    // Direct action commands
    public ReactiveCommand<Unit, Unit> NavigationSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> JobMenuCommand { get; }
    public ReactiveCommand<Unit, Unit> AutoSteerConfigCommand { get; }
    public ReactiveCommand<Unit, Unit> AgIOCommand { get; }

    // Menu visibility (if using ViewModel-controlled flyouts)
    private bool _isFileMenuOpen;
    public bool IsFileMenuOpen { get => _isFileMenuOpen; set => this.RaiseAndSetIfChanged(ref _isFileMenuOpen, value); }

    // ... more properties and commands
}
```

---

## Dependencies

### Required Services
- `INavigationService` (panel state management)
- `IDialogService` (for opening config dialogs)
- `IFieldService` (for Job Menu)
- `ISettingsService` (for configuration persistence)

### UI Components to Create
- `LeftPanelButtonStyle` (custom button style)
- `LeftPanelFlyoutStyle` (dropdown menu style)
- `LeftPanelSeparatorStyle` (visual separator)

---

## Testing Checklist

### Functional Tests
- [ ] All 8 buttons are visible and properly aligned
- [ ] Icons load and display correctly at 64x64 size
- [ ] Direct action buttons open their respective dialogs
- [ ] Dropdown menus open on click
- [ ] Sub-menu items are all accessible
- [ ] Clicking outside dropdown closes it
- [ ] Keyboard navigation works (Tab, Arrow keys, Enter)
- [ ] Commands are properly enabled/disabled based on app state

### Visual Tests
- [ ] Button hover states work correctly
- [ ] Button pressed states provide feedback
- [ ] Dropdowns have proper shadow/border styling
- [ ] Separator line is visible and styled correctly
- [ ] Panel matches WinForms aesthetic (floating, rounded corners)
- [ ] Icons are crisp and not blurry
- [ ] Tooltips appear on hover with correct text

### Integration Tests
- [ ] Panel state persists across app restarts
- [ ] Panel doesn't block map interactions
- [ ] Panel renders correctly at different screen resolutions
- [ ] Panel works with all window states (maximized, normal, minimized)
- [ ] No z-index conflicts with other UI elements

---

## Notes & Decisions

### Design Decisions
- **Icon-only buttons**: Match WinForms design for compact vertical space
- **Flyout menus**: Use Avalonia `MenuFlyout` for dropdown functionality
- **Floating style**: Panel should have rounded corners and shadow like top/right panels
- **File Menu at top**: Distinguished with separator to show it's "meta" level
- **Draggable/Rotatable**: Panel can be moved via drag handle (≡) and rotated between vertical/horizontal orientation (⟲)

### Technical Decisions
- **Canvas Structure**: Left panel Canvas simplified to match section control panel structure (no ZIndex/Background/IsHitTestVisible on Canvas, only on Border child)
- **Event Handling**: Border-level event handlers with `e.Source == sender` check to allow child controls (buttons, drag handle) to handle their own events
- **Bounds Detection**: `IsPointerOverUIPanel()` helper method checks if mouse is over any UI panel before map handles events
- **Transparent Overlay Removal**: Removed the transparent Panel overlay that was capturing all pointer events. Instead, pointer events are attached directly to OpenGLMapControl, allowing UI panels (ZIndex=10) to naturally receive events first
- **Touch-Friendly Combined Gesture**: Combined tap-to-rotate and hold-to-drag into a single 40px touch area for better touch UI ergonomics
  - Tap detection: < 300ms and < 5px movement = rotate panel orientation
  - Drag detection: > 5px movement = start dragging panel
  - Visual feedback: Shows both ≡ (drag) and ⟲ (rotate) icons with tooltip "Tap to rotate | Hold and drag to move"

### Open Questions
- [ ] Should File Menu be expandable/collapsible separately from other menus?
- [ ] Should button order be user-configurable?
- [ ] Should we show button labels on hover or always icon-only?
- [ ] Should panel be hideable/minimizable?

### Future Enhancements
- [ ] Customizable button order via drag-drop
- [ ] User-defined quick access buttons
- [ ] Recently used items in menus
- [ ] Search/filter for menu items
- [ ] Compact mode (smaller icons)

---

## Session Progress Tracking

### Session 2025-01-23
- [x] Analyzed WinForms FormGPS.Designer.cs structure
- [x] Identified all 8 left panel buttons
- [x] Mapped icons to button functions
- [x] Verified all icon files exist
- [x] Created this implementation checklist
- [x] Created Assets/Icons directory
- [x] Copied all 8 icon files to Assets/Icons/
- [x] Verified .csproj already includes Assets/** (line 14)
- [x] Created LeftPanelButton style (64x64, rounded, hover states)
- [x] Created LeftPanelSeparator style
- [x] Updated left sidebar XAML with all 8 buttons
- [x] Added separator after File Menu button
- [x] All buttons now use PNG icons instead of emojis
- [x] Implemented drag-to-move functionality (drag handle with ≡ symbol)
- [x] Implemented rotation toggle (⟲ button switches vertical/horizontal)
- [x] Added window bounds constraints for panel positioning
- [x] Fixed Canvas structure (simplified to match section control panel)
- [x] Fixed transparent overlay issue - removed Panel overlay, attached events to OpenGLMapControl
- [x] Tested and confirmed drag/rotation functionality works correctly
- [x] Cleaned up debug output
- [x] Improved touch interface - combined tap-to-rotate and hold-to-drag into single 40px touch area
- [x] Removed separate rotation button for cleaner, more touch-friendly design
- [x] Implemented File Menu as draggable floating panel (proof of concept)
- [x] Added ViewModel command (ToggleFileMenuPanelCommand) and property (IsFileMenuPanelVisible)
- [x] Created File Menu panel with Canvas positioning and drag handlers
- [x] Fixed tooltip lag during drag for both File Menu and Left Panel
- [x] File Menu includes 11 menu items organized in groups (Profile, Settings, Mode & Tools, Info & Help)
- [x] Renamed Navigation Settings to View Settings (more accurate for a navigation app)
- [x] Implemented Tools panel (renamed from Special Functions for consistency)
- [x] Copied 13 icons to Assets/Icons for Tools panel items (WizardWand, Chart, AutoSteerOn, ConS_SourcesHeading, AutoManualIsAuto, ConS_SourcesRoll, Boundary, ConD_ExtraGuides, ABSmooth, TrashContourRef, ABTracks, Webcam, YouTurnReverse)
- [x] Created Tools floating panel with 12 menu items organized in 4 groups (Wizards, Charts, Field & Boundary Tools, Utility Tools)
- [x] Added ViewModel command (ToggleToolsPanelCommand) and property (IsToolsPanelVisible)
- [x] Added drag handlers for Tools panel (ToolsPanel_PointerPressed/Moved/Released)
- [x] Updated IsPointerOverUIPanel() helper to include Tools panel
- [x] Build successful (0 errors, 14 warnings)
- [ ] Next: Implement remaining 5 button panels (Configuration, Job Menu, Field Tools, AutoSteer Config, AgIO)

### Next Session Goals
1. Implement remaining button panels using established pattern:
   - [x] File Menu (floating panel) ✅ COMPLETE
   - [x] View Settings (floating panel) ✅ COMPLETE (renamed from Navigation Settings)
   - [x] Tools (floating panel) ✅ COMPLETE (renamed from Special Functions)
   - [ ] Configuration (floating panel)
   - [ ] Job Menu (direct action/dialog)
   - [ ] Field Tools (floating panel)
   - [ ] AutoSteer Config (direct action/dialog)
   - [ ] AgIO (direct action/dialog)
2. Add ViewModel commands and properties for each panel
3. Wire up placeholder functionality for menu items
4. Consider implementing actual functionality for high-priority items (e.g., Boundary Tool, Charts)

---

## Quick Reference: Button Order

```
┌─────────────────┐
│   ☰ File Menu   │ (Dropdown)
├─────────────────┤ <-- Separator
│   🗺️ Navigation  │ (Direct)
│   ⚙️ Special Fn  │ (Dropdown)
│   ⚡ Config      │ (Dropdown)
│   📋 Job Menu    │ (Direct)
│   🛠️ Field Tools │ (Dropdown)
│   🎯 AutoSteer   │ (Direct)
│   📡 AgIO        │ (Direct)
└─────────────────┘
```

**Direct Actions** (4): Navigation, Job Menu, AutoSteer, AgIO
**Dropdowns** (4): File Menu, Special Functions, Configuration, Field Tools
