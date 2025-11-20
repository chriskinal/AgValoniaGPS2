# AgOpenGPS Service Migration Checklist

This document tracks the migration of services from WinForms AgOpenGPS to AgOpenGPS.Core, enabling a shared backend for both the 6.x maintenance branch and future Avalonia UI.

## Migration Status Legend
- ✅ **Migrated** - Service migrated to AgOpenGPS.Core
- 🔄 **In Progress** - Currently being migrated
- 📋 **Planned** - Identified for migration
- ❌ **Not Suitable** - Should remain in WinForms

---

## Already Migrated to AgOpenGPS.Core ✅

| Service | Location | Migrated Files |
|---------|----------|----------------|
| **GPS Service** | Core/Services/GpsService.cs | ✅ IGpsService, GpsService |
| **Display Settings** | Core/Services/DisplaySettingsService.cs | ✅ IDisplaySettingsService, DisplaySettingsService |
| **GPS Data Models** | Core/Models/GPS/ | ✅ Position, GpsData |
| **Field Statistics** | Core/Services/FieldStatisticsService.cs | ✅ IFieldStatisticsService, FieldStatisticsService, FieldStatistics |
| **Headland Line Data** | Core/Models/Guidance/HeadlandLine.cs | ✅ HeadlandLine, HeadlandPath (data models only) |
| **Curve Processing** | Core/Models/Guidance/CurveProcessing.cs | ✅ CurveProcessing (WinForms delegates to Core) |
| **File I/O Utils** | Core/Utilities/FileIoUtils.cs | ✅ FileIoUtils (WinForms delegates to Core) |
| **LocalFieldModel** | Core/Models/AgShare/LocalFieldModel.cs | ✅ LocalFieldModel, LocalPoint, AbLineLocal (with implicit conversions) |

---

## Phase 1: Quick Wins (Low Complexity) 📋

These services have minimal dependencies, no UI coupling, and provide immediate value.

| Priority | Service | Location | Lines | Description | Dependencies | Status | WinForms Uses Core? |
|----------|---------|----------|-------|-------------|--------------|--------|---------------------|
| 1.1 | **CFieldData** | Classes/CFieldData.cs | 160 | Field statistics and area calculations | Tool (width), avgSpeed | ✅ | ✅ Via FieldStatisticsService |
| 1.2 | **CFlag** | Classes/CFlag.cs | 40 | Field flag/marker data model | GeoCoord | ✅ | ✅ WinForms wraps Core Flag |
| 1.3 | **vec3/vec2** | Classes/vec3.cs | 173 | Vector math structures and operations | GeoCoord | ✅ | ✅ Implicit conversions enable Core integration |
| 1.4 | **CGLM** (math only) | Classes/CGLM.cs | 421 | Distance and angle calculations | None | ✅ | ✅ WinForms delegates to Core |
| 1.5 | **GeoConverter** | AgShare/Helpers/GeoConverter.cs | 142 | Coordinate conversion utilities | Core models | ✅ | ✅ WinForms delegates to Core |
| 1.6 | **CurveCABTools** | Protocols/ISOBUS/CurveCABTools.cs | 150 | Curve preprocessing algorithms | None | ✅ | ✅ WinForms delegates to Core |
| 1.7 | **FileIoUtils** | IO/FileIOUtils.cs | 50 | File I/O utility functions | None | ✅ | ✅ WinForms delegates to Core |
| 1.8 | **LocalFieldModel** | AgShare/Helpers/LocalFieldModel.cs | 45 | Field representation data model | Vec3 | ✅ | ✅ WinForms uses Core directly |

**WinForms Integration Status:**
- ✅ = WinForms uses Core implementation (proven by real usage)
- ⚠️ = Migrated to Core but WinForms still uses its own version (needs refactoring)
- ❌ = Not yet integrated

**Notes:**
- vec3/vec2 are fundamental types used by many other services - migrate early
- CGLM has some OpenGL rendering - only migrate pure math functions
- CFieldData provides live field statistics - valuable for both UIs
- **Next refactoring targets**: CGLM, GeoConverter (make WinForms delegate to Core)

---

## Phase 2: Configuration Services (Medium Complexity) 📋

Configuration and state management services with clear data models.

| Priority | Service | Location | Lines | Description | Dependencies | Status | WinForms Uses Core? |
|----------|---------|----------|-------|-------------|--------------|--------|---------------------|
| 2.1 | **CHeadLine** (service) | Classes/CHeadLine.cs | 95 | Headland line state management | vec3 | ✅ | ✅ WinForms wraps Core with full delegation |
| 2.2 | **CAHRS** | Classes/CAHRS.cs | 53 | IMU/AHRS sensor configuration | Settings | ✅ | ✅ WinForms delegates to Core AhrsConfiguration |
| 2.3 | **CSection** | Classes/CSection.cs | 76 | Section state data holder | vec2 | ✅ | ✅ WinForms delegates to Core SectionControl |
| 2.4 | **CTool** (config) | Classes/CTool.cs | 323 | Tool width, offset, section positions | Settings, Section | ✅ | ✅ WinForms delegates to Core ToolConfiguration |
| 2.5 | **CVehicle** (config) | Classes/CVehicle.cs | 360 | Vehicle geometry, steering limits | VehicleConfig, Settings | ✅ | ✅ WinForms delegates to Core VehicleConfig |
| 2.6 | **FieldParser** | AgShare/Helpers/FieldParser.cs | ? | Parse field file formats | Core models | 📋 | - |

**Notes:**
- CHeadLine: FormGPS dependency removed ✅, provides ToCoreHeadlandLine()/FromCoreHeadlandLine() conversion methods for Core integration
- CTool and CVehicle have OpenGL rendering - extract config/calculation logic only
- VehicleConfiguration model already exists in Core - enhance/complete it
- CSection is a pure state holder - easy migration candidate
- Create ToolConfiguration model similar to VehicleConfiguration

---

## Phase 2.5: Protocol Services (Medium Complexity) 📋

ISO standard protocol support for field data interoperability.

| Priority | Service | Location | Lines | Description | Dependencies | Status |
|----------|---------|----------|-------|-------------|--------------|--------|
| 2.6 | **IsoXmlFieldImporter** | Protocols/ISOBUS/IsoXmlFieldImporter.cs | 35 | ISO XML field import service | Core models | 📋 |
| 2.7 | **IsoXmlParserHelpers** | Protocols/ISOBUS/IsoXmlParserHelpers.cs | 300 | ISO XML parsing algorithms | Core models | 📋 |
| 2.8 | **ISO11783_TaskFile** | Protocols/ISOBUS/ISO11783_TaskFile.cs | 200 | ISO ISOXML export service | Field data models | 📋 |

**Notes:**
- High-value services for interoperability with other agricultural software
- Already well-structured with minimal UI dependencies
- Critical for standards compliance

---

## Phase 3: Core Algorithms (Medium-High Complexity) 📋

Mathematical algorithms and generation logic with manageable complexity.

| Priority | Service | Location | Lines | Description | Dependencies | Status |
|----------|---------|----------|-------|-------------|--------------|--------|
| 3.1 | **CSim** | Classes/CSim.cs | 125 | GPS position simulation | NMEA, LocalPlane, Wgs84 | 📋 |
| 3.2 | **CDubins** | Classes/CDubins.cs | 637 | Dubins path planning algorithm | vec3/vec2, turn radius | 📋 |
| 3.3 | **CTram** (gen only) | Classes/CTram.cs | 240 | Tramline generation logic | Boundary, Tool, Settings | 📋 |
| 3.4 | **CModuleComm** | Classes/CModuleComm.cs | 124 | Hardware communication abstraction | UDP service, Events | 📋 |
| 3.5 | **CFenceLine** | Classes/CFenceLine.cs | 170 | Fence line geometry calculations | vec2, boundary | 📋 |
| 3.6 | **CTurnLines** | Classes/CTurnLines.cs | 105 | Turn line generation algorithm | vec2, boundary | 📋 |
| 3.7 | **CTurn** | Classes/CTurn.cs | 35 | Turn area polygon testing | vec2, boundary | 📋 |
| 3.8 | **CFence** (logic) | Classes/CFence.cs | 160 | Boundary fence management | vec2, polygon math | 📋 |
| 3.9 | **CPatches** (logic) | Classes/CPatches.cs | 160 | Worked area tracking | triangle patches | 📋 |

**Notes:**
- CDubins is pure path planning math - excellent migration candidate
- CSim useful for testing both UIs without GPS hardware
- CModuleComm needs event-based refactoring to remove FormGPS.PerformClick() calls
- CTram rendering stays in UI, generation logic moves to Core

---

## Phase 4: Guidance Engines (High Complexity) 📋

Core agricultural guidance algorithms - the heart of AgOpenGPS.

| Priority | Service | Location | Lines | Description | Dependencies | Status |
|----------|---------|----------|-------|-------------|--------------|--------|
| 4.1 | **CGuidance** | Classes/CGuidance.cs | 413 | Stanley & Pure Pursuit algorithms | Vehicle, ABLine, Curve, AHRS | 📋 |
| 4.2 | **CABLine** (logic) | Classes/CABLine.cs | 661 | AB line guidance calculations | Tool, Vehicle, Guidance, Tram | 📋 |
| 4.3 | **CABCurve** (logic) | Classes/CABCurve.cs | 1540 | Curve guidance calculations | Tool, Vehicle, Guidance | 📋 |
| 4.4 | **CContour** (logic) | Classes/CContour.cs | 1038 | Contour following guidance | Tool, Vehicle, Guidance, AHRS | 📋 |
| 4.5 | **CTrack** | Classes/CTrack.cs | 350 | Track management and nudging | ABLine, Curve, UI events | 📋 |

**Notes:**
- CGuidance implements the core steering algorithms - critical for both UIs
- These services have OpenGL rendering mixed with calculations
- Extract calculation methods, leave rendering in WinForms
- GuidanceService in AgValoniaGPS is incomplete - these would complete it
- Create clear DTOs for calculation inputs/outputs

---

## Phase 5: Complex State Machines (Very High Complexity) 📋

Advanced features requiring careful architectural planning.

| Priority | Service | Location | Lines | Description | Dependencies | Status |
|----------|---------|----------|-------|-------------|--------------|--------|
| 5.1 | **CRecordedPath** | Classes/CRecordedPath.cs | 665 | Recorded path playback | Vehicle, Sim, Dubins, Guidance | 📋 |
| 5.2 | **CYouTurn** | Classes/CYouTurn.cs | 2897 | U-turn generation and execution | Tool, Vehicle, ABLine, Curve, Boundary, Dubins | 📋 |
| 5.3 | **CBoundary** | Classes/CBoundary.cs | 1000+ | Boundary and headland management | File I/O, multiple dependencies | 📋 |
| 5.4 | **AgShareClient** | AgShare/AgShareClient.cs | ? | Cloud field sharing | HTTP client, authentication | 📋 |
| 5.5 | **AgShareUploader** | AgShare/AgShareUploader.cs | ? | Upload fields to cloud | AgShareClient | 📋 |
| 5.6 | **AgShareDownloader** | AgShare/AgShareDownloader.cs | ? | Download shared fields | AgShareClient | 📋 |

**Notes:**
- CYouTurn is 2,897 lines - largest and most complex service
- These require the foundation services (Phase 1-4) to be migrated first
- Defer until Core architecture is mature and proven
- Consider breaking into smaller services where possible

---

## Services NOT Suitable for Migration ❌

These services are UI-specific and should remain in WinForms.

| Service | Location | Reason |
|---------|----------|--------|
| **ScreenTextures** | Classes/ScreenTextures.cs | Pure OpenGL texture management |
| **VehicleTextures** | Classes/VehicleTextures.cs | Pure OpenGL texture management |
| **CSound** | Classes/CSound.cs | WinForms audio playback |
| **CBrightness** | Classes/CBrightness.cs | Windows display brightness control |
| **CFeatureSettings** | Classes/CFeatureSettings.cs | UI feature toggles |
| **CExtensionMethods** | Classes/CExtensionMethods.cs | WinForms extension methods |
| **Brands** | Classes/Brands.cs | UI branding resources |
| **BoundaryBuilder** | Classes/BoundaryBuilder.cs | UI-specific boundary editing |

---

## Migration Patterns and Best Practices

### Service Migration Checklist

For each service migration:

- [ ] Create interface in `AgOpenGPS.Core/Interfaces/Services/I{ServiceName}.cs`
- [ ] Create model/DTO classes in `AgOpenGPS.Core/Models/{Domain}/`
- [ ] Implement service in `AgOpenGPS.Core/Services/{ServiceName}.cs`
- [ ] Remove FormGPS/UI dependencies - use events/callbacks
- [ ] Abstract settings - use dependency injection instead of Properties.Settings
- [ ] Write unit tests in `AgOpenGPS.Core.Tests/Services/{ServiceName}Tests.cs`
- [ ] Update WinForms to use service via ApplicationCore
- [ ] Verify AgValoniaGPS can consume the same service
- [ ] Build and test both net48 and net8.0 targets

### Dependency Injection Pattern

```csharp
// In AgOpenGPS.Core/ApplicationCore.cs
public class ApplicationCore
{
    public IToolConfigurationService ToolConfig { get; }
    public ISectionControlService SectionControl { get; }

    public ApplicationCore(...)
    {
        ToolConfig = new ToolConfigurationService();
        SectionControl = new SectionControlService(ToolConfig);
        // Register in DI container
    }
}
```

### Event-Based Communication

Replace FormGPS property access with events:

```csharp
// Before (WinForms)
mf.someProperty = value;
mf.btnSomething.PerformClick();

// After (Core service)
public event EventHandler<SomeEventArgs> PropertyChanged;
PropertyChanged?.Invoke(this, new SomeEventArgs(value));
```

### Settings Abstraction

Replace direct Settings access with interfaces:

```csharp
// Before
var width = Properties.Settings.Default.setTool_Width;

// After
public interface IToolSettings
{
    double ToolWidth { get; set; }
}
```

---

## Progress Tracking

**Total Services Identified:** 38
**Migrated:** 16 (42%)
**Phase 1 Targets:** 8 services (8 complete - PHASE 1 COMPLETE ✅)
**Phase 2 Targets:** 6 services (5 complete - CHeadLine ✅, CAHRS ✅, CSection ✅, CTool ✅, CVehicle ✅)
**Phase 2.5 Targets:** 3 services (Protocol)
**Phase 3 Targets:** 9 services
**Phase 4 Targets:** 5 services
**Phase 5 Targets:** 6 services
**Not Suitable:** 8 services

---

## Key Dependencies to Address

### Cross-cutting Concerns
- **Settings Management**: Abstract Properties.Settings.Default → ISettingsService
- **Coordinate Systems**: Ensure LocalPlane/GeoCoord/Wgs84 fully in Core
- **Event System**: Create event bus for UI notifications
- **Dependency Injection**: Constructor injection instead of FormGPS references

### Foundation Data Models Needed
- ✅ VehicleConfiguration (exists, may need enhancement)
- 📋 ToolConfiguration
- 📋 SectionConfiguration
- 📋 GuidanceConfiguration
- 📋 BoundaryData
- 📋 FieldData

---

## Next Steps

1. ✅ Migrate DisplaySettingsService and GpsService (COMPLETE)
2. **Choose Phase 1 service** (recommend: vec3/vec2 or CFieldData)
3. **Establish migration pattern** with first Phase 1 service
4. **Document pattern** for 6.x team to follow
5. **Iterate** through Phase 1, then Phase 2, etc.

---

## Notes for 6.x Maintenance Team

This migration strategy allows:
- **Incremental progress** - One service at a time
- **Shared effort** - Both WinForms and Avalonia benefit
- **Risk mitigation** - WinForms keeps working throughout
- **Clear priorities** - Start with easy wins, build momentum
- **Flexibility** - Pick services based on current work priorities

**No service migration is wasted effort.** Any service moved to Core will be:
- Usable by future Avalonia UI (or any other UI framework)
- More testable (unit tests in Core)
- Better architected (clean dependencies)
- Shared between all AgOpenGPS versions

---

*Last Updated: 2025-11-17*
*Audit Complete: 38 total services identified (13 added after comprehensive review)*
*Document maintained by: AgOpenGPS 6.x Core Migration Team*
