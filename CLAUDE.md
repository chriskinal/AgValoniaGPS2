# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

This repository contains **two parallel implementations** of AgOpenGPS, a precision agriculture guidance software for GPS-based field navigation and section control:

1. **SourceCode/** - Production-ready Windows Forms application (.NET Framework 4.8)
2. **AgValoniaGPS/** - Modern cross-platform reimplementation (Avalonia UI, .NET 8)

Both solutions are independent and maintained in parallel. The Avalonia version represents the strategic future direction with clean architecture, while the SourceCode version remains the stable, feature-complete option.

## Quick Start

### Working with Legacy SourceCode (Windows Forms)
```bash
# Build entire solution
dotnet build SourceCode/AgOpenGPS.sln

# Run tests
dotnet test SourceCode/AgOpenGPS.sln

# Publish all applications to AgOpenGPS/ folder
dotnet publish SourceCode/AgOpenGPS.sln
```

**Key files**: See `SourceCode/CLAUDE.md` for detailed guidance

### Working with AgValoniaGPS (Modern Avalonia)
```bash
# Navigate to modern solution
cd AgValoniaGPS

# Build
dotnet build AgValoniaGPS.Desktop/AgValoniaGPS.Desktop.csproj

# Run
dotnet run --project AgValoniaGPS.Desktop/AgValoniaGPS.Desktop.csproj

# Build entire solution
dotnet build AgValoniaGPS.sln
```

**Key files**: See `AgValoniaGPS/CONTINUATION_GUIDE.md` and `AgValoniaGPS/MIGRATION_ACHIEVEMENTS.md` for detailed guidance

## Architecture Comparison

### SourceCode Solution (Legacy)
- **Framework**: .NET Framework 4.8 + Windows Forms
- **Architecture**: Monolithic FormGPS.cs (~47,000 lines) + MVP-pattern library (AgOpenGPS.Core)
- **Graphics**: OpenTK.GLControl (OpenGL desktop)
- **Platform**: Windows-only
- **Status**: Production-ready with full feature set
- **Entry Point**: `SourceCode/GPS/Program.cs`

### AgValoniaGPS Solution (Modern)
- **Framework**: .NET 8 + Avalonia UI
- **Architecture**: Clean MVVM with ReactiveUI, dependency injection, service layer
- **Graphics**: Silk.NET.OpenGL (OpenGL ES 3.0 with ANGLE)
- **Platform**: Cross-platform (Windows, Linux, macOS)
- **Status**: Phase 7 complete - foundation ready for feature expansion
- **Entry Point**: `AgValoniaGPS/AgValoniaGPS.Desktop/Program.cs`

## Solution Structure

### SourceCode/ - Windows Forms Solution
```
SourceCode/
├── AgOpenGPS.sln                    # Main solution (14 projects)
├── GPS/                             # Main application (Windows Forms)
│   ├── Program.cs                  # Entry point
│   ├── Forms/FormGPS.cs           # Monolithic main form (~47K lines)
│   └── Classes/                    # Business logic (guidance, boundaries, rendering)
├── AgIO/                            # Communication hub application
├── AgOpenGPS.Core/                  # MVP business logic library
│   ├── ApplicationCore.cs          # Composition root
│   ├── Models/                     # Domain models
│   ├── Presenters/                 # MVP presenters
│   └── ViewModels/                 # MVVM view models
├── AgLibrary/                       # Shared utilities
├── AgOpenGPS.WpfApp/               # WPF prototype
├── AgOpenGPS.Avalonia.*/           # Avalonia migration attempt (Phase 2)
└── [Utility apps]                   # ModSim, GPS_Out, Keypad, AgDiag
```

### AgValoniaGPS/ - Modern Avalonia Solution
```
AgValoniaGPS/
├── AgValoniaGPS.sln                 # Modern solution (5 projects)
├── AgValoniaGPS.Desktop/            # Avalonia UI application
│   ├── Program.cs                  # Entry point
│   ├── App.axaml.cs                # DI configuration
│   ├── Views/
│   │   ├── MainWindow.axaml       # Main window with floating panels
│   │   └── DataIODialog.axaml     # NTRIP configuration
│   ├── Controls/
│   │   └── OpenGLMapControl.cs    # Custom OpenGL renderer
│   └── DependencyInjection/
│       └── ServiceCollectionExtensions.cs
├── AgValoniaGPS.Services/           # Service layer
│   ├── UdpCommunicationService.cs  # UDP networking (ports 9999/8888/7777/5544)
│   ├── GpsService.cs               # NMEA parsing, WGS84→UTM conversion
│   ├── NtripClientService.cs       # NTRIP RTK corrections
│   └── [FieldService, GuidanceService - stubs]
├── AgValoniaGPS.ViewModels/         # MVVM layer
│   └── MainViewModel.cs            # Main UI state
├── AgValoniaGPS.Models/             # Data models
└── AgValoniaGPS.Core/               # Business logic (stub)
```

## Technology Stack

### SourceCode Technologies
- **.NET Framework 4.8** (Windows-only)
- **Windows Forms** (GPS app) + **WPF** (newer components)
- **OpenTK.GLControl 3.3.3** - OpenGL rendering
- **GMap.NET.WinForms 2.1.7** - Map control
- **System.Data.SQLite 2.0.1** - Database
- **Dev4Agriculture.ISO11783.ISOXML** - ISO XML field format
- **NUnit 4.3.2** - Testing

### AgValoniaGPS Technologies
- **.NET 8.0** (cross-platform)
- **Avalonia 11.3.6** - UI framework with Fluent theme
- **ReactiveUI 20.1.1** - MVVM with reactive extensions
- **Silk.NET.OpenGL 2.22.0** - OpenGL ES 3.0 bindings
- **StbImageSharp 2.30.15** - Image loading
- **Microsoft.Extensions.DependencyInjection 8.0.0** - DI container
- **System.IO.Ports 8.0.0** - Serial communication

## Key Architectural Patterns

### SourceCode Patterns
- **MVP (Model-View-Presenter)** - Separation layer in AgOpenGPS.Core
- **Monolithic Main Form** - FormGPS.cs handles UI, rendering, communication, calculations
- **Settings Management** - Registry + XML file persistence
- **Hardware Communication** - UDP packet-based protocol with PGN numbers

### AgValoniaGPS Patterns
- **MVVM (Model-View-ViewModel)** - ReactiveUI throughout
- **Dependency Injection** - Constructor injection with Microsoft.Extensions.DI
- **Service Layer Abstraction** - Interfaces for all services (IUdpCommunicationService, IGpsService, etc.)
- **Event-Driven Communication** - Services communicate via events for loose coupling
- **Reactive Programming** - ObservableCollection, ReactiveCommand, IObservable

## Communication Protocol

Both solutions share the same UDP-based communication protocol:

### UDP Ports
- **9999** - Primary GPS/AutoSteer data (NMEA input, PGN messages)
- **8888** - Machine module communication
- **7777** - IMU data
- **5544** - Additional module communication
- **2233** - NTRIP RTCM data forwarding to GPS modules

### PGN Message System
- **PGN 126** - GPS module hello
- **PGN 127** - Machine module hello
- **PGN 128** - AutoSteer module hello
- **PGN 129** - IMU module hello
- **PGN 250** - AutoSteer data (heading, roll)
- **PGN 253** - Steer command from application
- **PGN 254** - Machine data

### Module Status Tracking
- **Hello-based** - Machine/IMU modules (periodic hello packets)
- **Data-flow based** - AutoSteer/GPS modules (detect incoming data)
- **3-second timeout** - Module marked as disconnected if no data

## GPS & Coordinate Systems

### NMEA Sentences
- **$GPGGA** - Position, altitude, fix quality, satellites
- **$GPVTG** - Speed and course over ground
- **$GPRMC** - Recommended minimum specific GPS/Transit data

### Coordinate Conversions
- **Input**: WGS84 (latitude/longitude in degrees)
- **Processing**: UTM (Universal Transverse Mercator) in meters
- **Output**: OpenGL coordinates for rendering

**SourceCode**: `GPS/Classes/` - Various coordinate helper methods
**AgValoniaGPS**: `AgValoniaGPS.Models/Position.cs` - Clean WGS84→UTM conversion in constructor

## OpenGL Rendering

### SourceCode Approach
- **OpenTK.GLControl** - Desktop OpenGL (version 3.3+)
- **Fixed-function pipeline** - Legacy GL calls mixed with modern shaders
- **Texture loading** - OpenTK utilities
- **Rendering location**: Integrated into FormGPS.cs

### AgValoniaGPS Approach
- **Silk.NET.OpenGL** - OpenGL ES 3.0 via ANGLE
- **Modern shader-based** - All rendering via GLSL ES 3.0 shaders
- **Texture loading** - StbImageSharp
- **Rendering location**: `AgValoniaGPS.Desktop/Controls/OpenGLMapControl.cs`

**Important**: GLSL ES 3.0 shaders must start with `#version 300 es` (no leading whitespace!) and use `in`/`out` instead of `attribute`/`varying`

## Development Workflows

### Adding Features to SourceCode
1. **Business logic** → `AgOpenGPS.Core/Models/` or `GPS/Classes/`
2. **UI components** → `GPS/Forms/` (Windows Forms) or `AgOpenGPS.WpfApp/` (WPF)
3. **Shared utilities** → `AgLibrary/`
4. **Tests** → `AgLibrary.Tests/` or `AgOpenGPS.Core.Tests/` (NUnit, AAA pattern)

### Adding Features to AgValoniaGPS
1. **Create service interface** → `AgValoniaGPS.Services/Interfaces/IYourService.cs`
2. **Implement service** → `AgValoniaGPS.Services/YourService.cs`
3. **Register in DI** → `AgValoniaGPS.Desktop/DependencyInjection/ServiceCollectionExtensions.cs`
4. **Create ViewModel** → `AgValoniaGPS.ViewModels/YourViewModel.cs`
5. **Create View** → `AgValoniaGPS.Desktop/Views/YourView.axaml` + code-behind
6. **Wire up events** → Subscribe to service events in ViewModel

### Testing Hardware Communication
- **Simulator**: Use `SourceCode/ModSim/` project to simulate GPS/modules
- **Real hardware**: Arduino-based modules on UDP network (AutoSteer, Machine, IMU)
- **NTRIP testing**: Free casters at rtk2go.com (port 2101)

## AgValoniaGPS Migration Strategy: Incremental Button-by-Button Approach

### Why This Approach Works

**CRITICAL**: Previous migration attempts using "big-bang" rewrites **failed**. The successful strategy is **incremental feature-by-feature migration**.

#### Failed Approaches (Don't Do This)
```
❌ Extract ALL business logic → Build complete Avalonia UI → Connect everything
❌ Rewrite FormGPS.cs entirely before testing
❌ Try to separate all 47,000 lines of coupled code at once
```

**Problems with big-bang rewrites:**
- FormGPS.cs has ~47,000 lines of tightly coupled UI + business logic
- Business logic deeply intertwined with WinForms controls and event handlers
- Overwhelming scope - hard to know where to start
- No working application until everything is done
- Difficult to test incrementally
- High risk of abandonment

#### Successful Approach: Strangler Fig Pattern (Do This)
```
✅ Build foundation → Add one feature at a time → Test each feature → Repeat
✅ Each feature corresponds to a button/panel in FormGPS.cs
✅ Always have a working application
```

**Benefits:**
- Working application from Day 1 (even if minimal)
- Each feature is self-contained and testable
- Can reference FormGPS.cs as you implement each feature
- Easy to understand context for each piece of business logic
- Low risk - always have a working version to fall back to
- Clear progress tracking (Phase 7 complete, Phase 8 next, etc.)

### How to Migrate a New Feature

When adding the next feature from FormGPS.cs to AgValoniaGPS, follow this process:

#### Step 1: Identify the Feature in FormGPS.cs
Find the button/panel/menu item in `SourceCode/GPS/Forms/FormGPS.cs`:
- Look for button click handlers (e.g., `btnFieldBoundary_Click`)
- Find related UI controls and their event handlers
- Trace data flow - what gets displayed, what gets saved
- Identify timers, update loops, render calls

**Example**: Field boundary feature
- Button: `btnFieldBoundary_Click`
- Rendering: Look for boundary drawing in OpenGL render loop
- Data: Find boundary point storage (lists, arrays)
- Save/Load: Find field file I/O code

#### Step 2: Extract Business Logic
Separate the business logic from UI code:
- **Business logic**: Calculations, algorithms, data transformations, validations
- **UI logic**: Button clicks, control updates, display formatting

Create a new service in AgValoniaGPS:
```csharp
// AgValoniaGPS.Services/Interfaces/IFieldBoundaryService.cs
public interface IFieldBoundaryService
{
    event EventHandler<BoundaryPointAddedEventArgs>? BoundaryPointAdded;
    void StartRecording();
    void StopRecording();
    void AddPoint(Position position);
    List<BoundaryPoint> GetCurrentBoundary();
}

// AgValoniaGPS.Services/FieldBoundaryService.cs
public class FieldBoundaryService : IFieldBoundaryService
{
    // Pure business logic - no UI dependencies
}
```

#### Step 3: Create Models
Add data models to `AgValoniaGPS.Models/`:
```csharp
public class BoundaryPoint
{
    public double Easting { get; set; }
    public double Northing { get; set; }
    public DateTime Timestamp { get; set; }
}
```

#### Step 4: Register Service in DI
Add to `AgValoniaGPS.Desktop/DependencyInjection/ServiceCollectionExtensions.cs`:
```csharp
services.AddSingleton<IFieldBoundaryService, FieldBoundaryService>();
```

#### Step 5: Update ViewModel
Add to existing ViewModel or create new one:
```csharp
// AgValoniaGPS.ViewModels/MainViewModel.cs
private readonly IFieldBoundaryService _fieldBoundaryService;

public MainViewModel(IFieldBoundaryService fieldBoundaryService, ...)
{
    _fieldBoundaryService = fieldBoundaryService;
    _fieldBoundaryService.BoundaryPointAdded += OnBoundaryPointAdded;
}

public ReactiveCommand<Unit, Unit> StartBoundaryRecordingCommand { get; }
```

#### Step 6: Create/Update UI
Add controls to existing view or create new dialog:
```xml
<!-- AgValoniaGPS.Desktop/Views/MainWindow.axaml -->
<Button Content="Record Boundary"
        Command="{Binding StartBoundaryRecordingCommand}"/>
```

#### Step 7: Add Rendering (if needed)
Update `OpenGLMapControl.cs` to render the feature:
```csharp
// OnOpenGLRender() in OpenGLMapControl.cs
private void RenderBoundary()
{
    // OpenGL rendering code for boundary lines
}
```

#### Step 8: Test Incrementally
- Build and run the application
- Test the new feature in isolation
- Verify it works with existing features
- Test with real/simulated GPS data

#### Step 9: Mark Phase Complete
- Update `MIGRATION_ACHIEVEMENTS.md` with what was accomplished
- Update `CONTINUATION_GUIDE.md` with current status
- Commit the working code

### Current Migration Progress

#### ✅ Completed Features (Phases 1-7)
| Phase | FormGPS.cs Feature | AgValoniaGPS Implementation | Status |
|-------|-------------------|---------------------------|--------|
| 1-2 | Foundation | Project structure, DI, OpenGL setup | ✅ Complete |
| 3 | Module status indicators | UdpCommunicationService + status panels | ✅ Complete |
| 4 | NTRIP connection button/dialog | NtripClientService + DataIODialog | ✅ Complete |
| 5 | GPS position display | GpsService + status panel | ✅ Complete |
| 6 | Map pan/zoom (mouse) | OpenGLMapControl mouse handlers | ✅ Complete |
| 7 | Vehicle rendering | Textured quad with GPS heading | ✅ Complete |

#### ❌ Remaining Features (Phases 8-12)
| Phase | FormGPS.cs Feature | What to Migrate | Complexity |
|-------|-------------------|----------------|------------|
| 8 | Field boundary tools | Recording, editing, saving boundaries | Medium |
| 9 | Guidance line buttons | AB line, curve, contour creation/editing | High |
| 10 | Section control panel | Section on/off, width config, mapping | Medium |
| 11 | AutoSteer engage button | Engage/disengage, steering parameters | Medium |
| 12 | Settings/config dialogs | Vehicle config, app settings, persistence | Low |

### Finding Code in FormGPS.cs

**FormGPS.cs is huge** (~47,000 lines). Here's how to find what you need:

#### Search for Event Handlers
```csharp
// Button clicks
private void btnABLine_Click(...)
private void btnContour_Click(...)
private void btnAutoSteer_Click(...)

// Menu items
private void menuFieldOpen_Click(...)
private void menuFieldBoundary_Click(...)

// Timers
private void tmrWatchdog_tick(...)  // Main update loop
```

#### Search for Rendering Code
Look in the OpenGL render method:
```csharp
private void OpenGL_Draw(...)
{
    // Search for GL.Begin, GL.End, GL.DrawArrays
    // Look for texture binding, vertex buffers
}
```

#### Search for Data Structures
```csharp
// Field boundaries
public List<CBoundary> boundaries;

// AB lines
public CABLine ABLine;

// Guidance paths
public CContour contour;
```

#### Search for File I/O
```csharp
// Saving
private void FileSaveBoundary(...)
private void FileSaveField(...)

// Loading
private void FileOpenField(...)
```

### Key Principles

1. **One feature at a time** - Don't try to migrate multiple major features simultaneously
2. **Keep it working** - Always have a runnable application after each feature
3. **Reference FormGPS.cs** - It's your specification - copy the algorithm, not the UI coupling
4. **Test incrementally** - Each feature should be testable in isolation
5. **Services are pure** - No UI dependencies in service layer
6. **Events for communication** - Services raise events, ViewModels subscribe
7. **Stub what's not ready** - Create interface + stub implementation for future features

### Anti-Patterns to Avoid

❌ Don't copy FormGPS.cs code directly (it's UI-coupled)
❌ Don't try to extract all business logic at once
❌ Don't skip testing intermediate steps
❌ Don't add multiple features before testing
❌ Don't put business logic in ViewModels or code-behind
❌ Don't create tight coupling between services

### Example: How GPS Service Was Migrated

**FormGPS.cs code** (UI-coupled):
```csharp
private void ParseNMEA(string sentence)
{
    // 200 lines of parsing mixed with:
    lblLatitude.Text = latitude.ToString();  // UI update
    CalculateUTM();  // Business logic
    UpdateDisplay();  // UI update
}
```

**AgValoniaGPS approach** (clean separation):
```csharp
// Service (pure business logic)
public class GpsService : IGpsService
{
    public void ParseNmeaSentence(string sentence)
    {
        // Pure parsing logic
        var data = new GpsData { Latitude = lat, Longitude = lon };
        GpsDataUpdated?.Invoke(this, new GpsDataEventArgs(data));
    }
}

// ViewModel (UI coordination)
public class MainViewModel
{
    private void OnGpsDataUpdated(object? sender, GpsDataEventArgs e)
    {
        Dispatcher.UIThread.Invoke(() => {
            Latitude = e.Data.Latitude;  // UI binding
        });
    }
}
```

## AgValoniaGPS Current Status (Phase 7 Complete)

### Working Features
- ✅ Cross-platform Avalonia UI with floating status panels
- ✅ OpenGL ES 3.0 map rendering with textured vehicle
- ✅ UDP communication on all required ports (9999/8888/7777/5544)
- ✅ NMEA GPS parsing with WGS84→UTM conversion
- ✅ NTRIP client for RTK corrections (RTCM data + GGA reporting)
- ✅ Module status tracking (AutoSteer, Machine, IMU, GPS)
- ✅ Mouse-based map pan/zoom controls
- ✅ Real-time vehicle position and heading visualization

### Not Yet Implemented (Future Phases)
- ❌ Field boundary management (Phase 8)
- ❌ Guidance lines (AB lines, curves, contour) (Phase 9)
- ❌ Section control (Phase 10)
- ❌ AutoSteer control panel (Phase 11)
- ❌ Settings persistence (Phase 12)

**Reference**: For detailed status, see `AgValoniaGPS/MIGRATION_ACHIEVEMENTS.md`

## Important Code Locations

### SourceCode Critical Files
- `GPS/Program.cs` - Main entry point with single-instance enforcement
- `GPS/Forms/FormGPS.cs` - Monolithic main form (~47,000 lines)
- `AgOpenGPS.Core/ApplicationCore.cs` - MVP composition root
- `AgLibrary/Settings/` - Configuration persistence

### AgValoniaGPS Critical Files
- `AgValoniaGPS.Desktop/Program.cs` - Application entry point
- `AgValoniaGPS.Desktop/App.axaml.cs` - DI configuration and startup
- `AgValoniaGPS.Desktop/Views/MainWindow.axaml` - Main UI window
- `AgValoniaGPS.Desktop/Controls/OpenGLMapControl.cs` - OpenGL renderer
- `AgValoniaGPS.Services/UdpCommunicationService.cs` - Network I/O hub
- `AgValoniaGPS.ViewModels/MainViewModel.cs` - Primary UI state

## Contributing

Per README.md:
- **Development branch**: `develop` (submit PRs here)
- **Stable branch**: `master`
- **Translation**: Via Weblate (https://hosted.weblate.org/engage/agopengps)

## Debugging Tips

### SourceCode Debugging
- Set GPS project as startup in Visual Studio
- AgIO logs show hardware communication issues
- Use ModSim for hardware simulation

### AgValoniaGPS Debugging
- Check console for OpenGL shader compilation errors
- Use Wireshark to monitor UDP traffic on ports 9999/8888/7777/5544
- Enable debug logging in services for packet analysis
- Check `MainViewModel.DebugLog` property for runtime diagnostics
- Module status updates every 100ms - check timeout logic if modules show offline

## Version Management

### SourceCode
- **GitVersion** handles semantic versioning automatically
- **Manual version file**: `./sys/version.h` for patch increments

### AgValoniaGPS
- **Assembly version**: `Program.cs` static property
- **Semantic versioning**: Parsed from `Application.ProductVersion`

## Common Commands

### SourceCode
```bash
# Build
dotnet build SourceCode/AgOpenGPS.sln

# Test
dotnet test SourceCode/AgOpenGPS.sln

# Publish all apps
dotnet publish SourceCode/AgOpenGPS.sln
```

### AgValoniaGPS
```bash
# Build
dotnet build AgValoniaGPS/AgValoniaGPS.sln

# Run
dotnet run --project AgValoniaGPS/AgValoniaGPS.Desktop/AgValoniaGPS.Desktop.csproj

# Clean build
cd AgValoniaGPS && dotnet clean && dotnet build
```

## Additional Documentation

- **SourceCode details**: `SourceCode/CLAUDE.md`
- **Avalonia migration proposal**: `SourceCode/AVALONIA_MIGRATION_PROPOSAL.md`
- **AgValoniaGPS guide**: `AgValoniaGPS/CONTINUATION_GUIDE.md`
- **Migration achievements**: `AgValoniaGPS/MIGRATION_ACHIEVEMENTS.md`
- **Project README**: `README.md`

## External Resources

- **AgOpenGPS Documentation**: https://docs.agopengps.com/
- **AgOpenGPS Forum**: https://discourse.agopengps.com/
- **PCB/Firmware Repository**: https://github.com/agopengps-official/Boards
- **Avalonia Documentation**: https://docs.avaloniaui.net/
- **ReactiveUI Documentation**: https://www.reactiveui.net/docs/

## License

GPLv3 - See LICENSE file for full terms
