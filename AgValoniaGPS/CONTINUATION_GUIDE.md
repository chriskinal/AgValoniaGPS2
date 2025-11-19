# AgValoniaGPS Continuation Guide

## Quick Context Prompt

When starting a new session, use this prompt to quickly restore context:

```
I'm continuing work on the AgValoniaGPS project - a complete architectural
reimagining of AgOpenGPS using Avalonia UI and modern .NET 8.

We've completed Phase 7 (vehicle texture rendering). The application now has:
- Cross-platform Avalonia UI with floating status panels
- OpenGL ES 3.0 map rendering with textured tractor vehicle
- UDP communication with module status tracking
- NMEA GPS parsing with real-time position updates
- NTRIP client for RTK corrections
- Mouse-based map pan/zoom controls

Please read:
1. C:\Users\chrisk\source\AgOpenGPS\AgValoniaGPS\MIGRATION_ACHIEVEMENTS.md
2. C:\Users\chrisk\source\AgOpenGPS\AgValoniaGPS\CONTINUATION_GUIDE.md

The codebase is at: C:\Users\chrisk\source\AgOpenGPS\AgValoniaGPS\

What should we work on next?
```

## Project Overview

**Location**: `C:\Users\chrisk\source\AgOpenGPS\AgValoniaGPS\`

**Purpose**: Cross-platform agricultural guidance application built with Avalonia UI, replacing the Windows Forms-based AgOpenGPS with a modern, maintainable architecture.

**Current Status**: Phase 7 Complete - Foundation ready for feature expansion

## Architecture

### Solution Structure
```
AgValoniaGPS/
├── AgValoniaGPS.Core/          # Core business logic
├── AgValoniaGPS.Models/        # Data models and DTOs
├── AgValoniaGPS.Services/      # Service implementations
├── AgValoniaGPS.ViewModels/    # MVVM view models
└── AgValoniaGPS.Desktop/       # Avalonia desktop app
    ├── Controls/               # Custom controls (OpenGLMapControl)
    ├── Views/                  # XAML views and code-behind
    ├── Converters/            # Value converters
    └── DependencyInjection/   # DI setup
```

### Key Design Patterns
- **MVVM**: Model-View-ViewModel with ReactiveUI
- **Dependency Injection**: Constructor injection throughout
- **Service Layer**: Business logic in dedicated services
- **Event-Driven**: Loosely coupled communication via events

### Technology Stack
- **Avalonia 11.3.6** - Cross-platform UI framework
- **ReactiveUI 20.1.1** - MVVM with reactive extensions
- **Silk.NET.OpenGL 2.22.0** - OpenGL bindings
- **StbImageSharp 2.30.15** - Image loading
- **.NET 8.0** - Modern runtime

## Key Files & Their Purpose

### Core Services
- `AgValoniaGPS.Services/UdpCommunicationService.cs` - UDP networking (ports 9999, 8888, 7777, 5544)
- `AgValoniaGPS.Services/GpsService.cs` - NMEA parsing and coordinate conversion
- `AgValoniaGPS.Services/NtripClientService.cs` - NTRIP RTK corrections
- `AgValoniaGPS.Services/NmeaParserService.cs` - NMEA sentence parsing
- `AgValoniaGPS.Services/FieldService.cs` - Field management (stub)
- `AgValoniaGPS.Services/GuidanceService.cs` - Guidance logic (stub)

### View Models
- `AgValoniaGPS.ViewModels/MainViewModel.cs` - Main window VM with GPS/module/NTRIP state

### Views & Controls
- `AgValoniaGPS.Desktop/Views/MainWindow.axaml(.cs)` - Main window with floating panels
- `AgValoniaGPS.Desktop/Views/DataIODialog.axaml(.cs)` - NTRIP configuration dialog
- `AgValoniaGPS.Desktop/Controls/OpenGLMapControl.cs` - Custom OpenGL rendering control

### Models
- `AgValoniaGPS.Models/GpsData.cs` - GPS position and quality data
- `AgValoniaGPS.Models/Position.cs` - Geographic position with UTM conversion
- `AgValoniaGPS.Models/NtripConfiguration.cs` - NTRIP settings
- `AgValoniaGPS.Models/ModuleType.cs` - Module enumeration
- `AgValoniaGPS.Models/PgnNumbers.cs` - PGN message constants

### Configuration
- `AgValoniaGPS.Desktop/DependencyInjection/ServiceCollectionExtensions.cs` - DI setup
- `AgValoniaGPS.Desktop/App.axaml(.cs)` - Application startup

## How to Build & Run

```bash
# Navigate to solution directory
cd C:\Users\chrisk\source\AgOpenGPS\AgValoniaGPS

# Build
dotnet build AgValoniaGPS.Desktop/AgValoniaGPS.Desktop.csproj

# Run
dotnet run --project AgValoniaGPS.Desktop/AgValoniaGPS.Desktop.csproj
```

## What's Working

### GPS & Navigation
- ✅ NMEA sentence parsing (GGA, VTG, RMC)
- ✅ WGS84 to UTM coordinate conversion
- ✅ Real-time position updates
- ✅ Speed, heading, satellite count tracking
- ✅ Fix quality detection (GPS/DGPS/RTK)

### Module Communication
- ✅ UDP send/receive on all required ports
- ✅ PGN message parsing
- ✅ Module status tracking (AutoSteer, Machine, IMU, GPS)
- ✅ 3-second timeout detection
- ✅ Hello packet system

### NTRIP
- ✅ TCP connection to NTRIP casters
- ✅ RTCM data reception
- ✅ Automatic GGA position reporting
- ✅ UDP forwarding to GPS modules (port 2233)
- ✅ Connection status tracking
- ✅ Configuration dialog

### OpenGL Rendering
- ✅ Grid rendering with orthographic projection
- ✅ Textured vehicle rendering (TractorAoG.png)
- ✅ Vehicle rotation based on GPS heading
- ✅ Mouse pan (middle button drag)
- ✅ Mouse zoom (wheel, 2x-100x)
- ✅ Real-time vehicle position updates

### UI
- ✅ Modern floating panel design
- ✅ GPS status panel
- ✅ Module status indicators
- ✅ NTRIP status panel
- ✅ Data I/O dialog for NTRIP config

## What Needs Work (Future Phases)

### Phase 8: Field Management
- Field boundary creation and editing
- Field data persistence (SQLite)
- Field selection UI
- Area calculation and display
- Import/export field data

### Phase 9: Guidance Lines
- AB line creation and management
- Curve guidance implementation
- Contour guidance
- Guidance line visualization on map
- Cross-track error calculation

### Phase 10: Section Control
- Section configuration UI
- Section on/off control
- Section status visualization
- Machine settings integration
- Section mapping to equipment

### Phase 11: AutoSteer Integration
- AutoSteer control panel
- Steering parameters UI
- AutoSteer engage/disengage
- Real-time steering visualization
- PGN 253 (steer data) sending

### Phase 12: Settings & Configuration
- Vehicle configuration UI
- Implement settings persistence
- User preferences management
- Import/export settings

## Common Development Tasks

### Adding a New Service

1. Create interface in `AgValoniaGPS.Services/Interfaces/IYourService.cs`
2. Implement service in `AgValoniaGPS.Services/YourService.cs`
3. Register in DI: `ServiceCollectionExtensions.cs`
4. Inject into ViewModel constructor
5. Subscribe to events in ViewModel

### Adding OpenGL Rendering Features

Edit `AgValoniaGPS.Desktop/Controls/OpenGLMapControl.cs`:
- `OnOpenGLInit()` - Initialization (shaders, buffers, textures)
- `OnOpenGlRender()` - Render loop
- `OnOpenGlDeinit()` - Cleanup

OpenGL ES 3.0 shader requirements:
- Must start with `#version 300 es` (no leading whitespace!)
- Use `precision highp float;` for fragment shaders
- Use `layout (location = N)` for vertex attributes
- Use `in`/`out` instead of `attribute`/`varying`

### Adding UI Components

1. Create XAML view in `Views/YourView.axaml`
2. Create code-behind `Views/YourView.axaml.cs`
3. Create ViewModel in `AgValoniaGPS.ViewModels/YourViewModel.cs`
4. Register ViewModel in DI if needed
5. Add to MainWindow or create new window

### Working with UDP Communication

All UDP communication goes through `UdpCommunicationService`:
- Listen on: 9999, 8888, 7777, 5544
- Send from: 9999
- Subscribe to `DataReceived` event for incoming packets
- Use `SendData()` or specific helpers like `SendHelloPacket()`

PGN numbers are in `Models/PgnNumbers.cs`

## Debugging Tips

### OpenGL Issues
- Check console for shader compilation errors
- OpenGL Version info printed on startup
- Use `GL_ERRORS` define for error checking
- ANGLE renderer requires OpenGL ES 3.0 shaders

### UDP Communication Issues
- Check firewall settings
- Use Wireshark to monitor UDP traffic
- Enable debug logging in `UdpCommunicationService`
- Check `DebugLog` property in MainViewModel

### Module Status Issues
- Module status polling is every 100ms in `StartHelloTimer()`
- AutoSteer/GPS use data flow monitoring (3s timeout)
- Machine/IMU use hello packet monitoring (3s timeout)
- Check `IsModuleDataOk()` vs `IsModuleHelloOk()` usage

### NTRIP Issues
- Check caster address and port
- Verify mount point exists on caster
- Check username/password if required
- Monitor `NtripBytesReceived` for data flow
- Enable TCP logging in `NtripClientService`

## Important Code Patterns

### Event Subscriptions in ViewModel
```csharp
public MainViewModel(IUdpCommunicationService udpService)
{
    _udpService = udpService;
    _udpService.DataReceived += OnUdpDataReceived;
}

private void OnUdpDataReceived(object? sender, UdpDataReceivedEventArgs e)
{
    // Always marshal to UI thread!
    Dispatcher.UIThread.Invoke(() => UpdateUI(e));
}
```

### Coordinate Conversion
```csharp
// WGS84 (lat/lon) -> UTM in Position.cs constructor
var position = new Position(latitude, longitude, altitude);
double easting = position.Easting;
double northing = position.Northing;
```

### OpenGL Transforms
```csharp
// Camera position -> View matrix
Matrix4x4 view = Matrix4x4.CreateTranslation(-_cameraX, -_cameraY, 0);

// Orthographic projection
Matrix4x4 projection = Matrix4x4.CreateOrthographic(
    viewWidth, viewHeight, -1, 1);

// Model transform (vehicle position + rotation)
Matrix4x4 model = Matrix4x4.CreateRotationZ(headingRadians) *
                  Matrix4x4.CreateTranslation(vehicleX, vehicleY, 0);

// Combined MVP
Matrix4x4 mvp = model * view * projection;
```

## Testing with Real Hardware

### Required Hardware Setup
1. **GPS Module** - Sends NMEA via UDP to port 9999
2. **AutoSteer Module** - Arduino-based, communicates via PGN messages
3. **Machine Module** - Receives section control commands
4. **IMU Module** - BNO085 or similar, sends orientation data

### GPS Simulator Option
Can simulate GPS by sending NMEA sentences to UDP port 9999:
```
$GPGGA,123519,4807.038,N,01131.000,E,1,08,0.9,545.4,M,46.9,M,,*47
$GPVTG,054.7,T,034.4,M,005.5,N,010.2,K*48
```

### NTRIP Testing
Free NTRIP casters for testing:
- rtk2go.com (port 2101)
- Various mount points available
- May require free registration

## Known Issues & Limitations

### Current Limitations
- No field boundaries yet (Phase 8)
- No guidance lines yet (Phase 9)
- No section control yet (Phase 10)
- No AutoSteer control panel yet (Phase 11)
- Settings not persisted yet (Phase 12)
- Vehicle texture doesn't change based on vehicle type
- No compass rose on map
- No scale bar on map

### Planned Improvements
- Add vehicle selection (tractor, harvester, sprayer)
- Add compass and scale indicators
- Add lat/lon grid overlay
- Add track history/breadcrumbs
- Add field boundary rendering
- Add guidance line rendering
- Implement settings persistence

## Performance Notes

- OpenGL rendering is 60 FPS (vsync)
- UDP packet processing handles all available packets per receive
- Module status polling is 100ms (responsive but not excessive)
- NTRIP GGA reporting is 10s intervals (configurable)
- No performance issues observed with current features

## Useful Commands

```bash
# Clean build
dotnet clean && dotnet build

# Kill running instances
taskkill //F //IM AgValoniaGPS.Desktop.exe

# Build and run
dotnet build && dotnet run --project AgValoniaGPS.Desktop/AgValoniaGPS.Desktop.csproj

# View UDP traffic (PowerShell)
# Test-NetConnection -ComputerName localhost -Port 9999

# Check if app is running
tasklist | findstr AgValoniaGPS
```

## Reference Documentation

### Original AgOpenGPS
Location: `C:\Users\chrisk\source\AgOpenGPS\SourceCode\`
Main form: `GPS\FormGPS.cs` (~15,000 lines - good reference for features)

### Avalonia Documentation
- https://docs.avaloniaui.net/
- https://github.com/AvaloniaUI/Avalonia

### ReactiveUI Documentation
- https://www.reactiveui.net/docs/

### OpenGL ES 3.0 Reference
- https://registry.khronos.org/OpenGL-Refpages/es3.0/

## Contact & Resources

**Project**: AgOpenGPS Avalonia Migration
**Original Project**: https://github.com/farmerbriantee/AgOpenGPS
**Framework**: Avalonia UI
**Language**: C# / .NET 8.0
**Graphics**: OpenGL ES 3.0 via Silk.NET + ANGLE

---

## Quick Start Checklist for Next Session

1. ✅ Read this file
2. ✅ Read MIGRATION_ACHIEVEMENTS.md
3. ✅ Navigate to: `C:\Users\chrisk\source\AgOpenGPS\AgValoniaGPS\`
4. ✅ Build and run to verify everything works
5. ✅ Decide on next phase (8, 9, 10, 11, or 12)
6. ✅ Review relevant code sections
7. ✅ Start implementing!

**Remember**: The architecture is clean and modular. Add features incrementally, test frequently, and maintain the separation of concerns. The codebase is designed to be maintainable and extensible.

Good luck! 🚜
