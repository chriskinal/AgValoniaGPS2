# AB Line & Curve Line Format Analysis

## Executive Summary

AgOpenGPS supports **two types of guidance lines**:

1. **AB Lines** (ABLines.txt) - Straight guidance lines
2. **Curve Lines** (CurveLines.txt) - Curved/contour guidance lines

Both must be supported for full compatibility. Your simple AB line is actually an **A+ heading line** (simplest type).

---

## 1. ABLines.txt - Straight Guidance Lines

### Format from Your Test Field:
```
AB 80°,79.95161324,-208.652,90.491
```

### Structure:
```
Name,Heading(degrees),Point_Easting,Point_Northing
```

**Fields**:
- `Name`: Display name (e.g., "AB 80°")
- `Heading`: Direction in **degrees** (0-360)
- `Point_Easting`: Single point E coordinate (meters, local)
- `Point_Northing`: Single point N coordinate (meters, local)

###human Types of AB Lines

The codebase shows there are actually **multiple AB line types**:

#### Type 1: A+ Heading (Your Example)
- **One point + heading direction**
- Infinite line extending in heading direction
- Format: `Name,Heading,E,N`
- Example: `AB 80°,79.95161324,-208.652,90.491`

#### Type 2: Two-Point AB Line (A and B)
-  **Two points defining a line segment**
- Heading calculated from A to B
- Stored in `CTrk` class with `ptA` and `ptB`
- File format same, but uses both points

### Implementation Note:
The CABLine class (CABLine.cs) shows:
```csharp
public vec2 desPtA = new vec2(0.2, 0.15);  // Design point A
public vec2 desPtB = new vec2(0.3, 0.3);   // Design point B
public double desHeading = 0;              // Heading
public string desName = "";                // Name
```

The actual saved format appears to collapse two points into **one point + heading**, making storage simpler.

---

## 2. CurveLines.txt - Curved Guidance Lines

### Format from Your Test Field:
```
$CurveLines
Cu 275.4°
4.80700069562968
660
-17.708,63.64,1.7319
-16.721,63.48,1.7319
-15.734,63.319,1.7319
...
[660 points total]
```

### Structure:
```
$CurveLines                  # Header
CurveName                    # Name
??? (unknown field)          # Possibly nudge distance or metadata
PointCount                   # Number of points (integer)
E1,N1,Heading1              # Point 1: Easting, Northing, Heading (radians)
E2,N2,Heading2              # Point 2
...
[Next curve if exists]
```

**Critical Differences from AB Lines**:
- **Multiple points** (hundreds, even 660!)
- Each point has **individual heading** (in **radians**, not degrees!)
- Headings represent direction at each point along curve
- Used for curved guidance, contour farming, irregular paths

### Real Example Analysis:

Your test field has **3 curve lines**:
1. **Cu 275.4°** - 660 points
2. **Cu 49.9°** - 501 points
3. **Cu 183.7°** - 445 points

Each curve is a recorded path driven by the operator, saved with precise heading at each point for accurate following.

---

## 3. Track Mode System

From `CTrack.cs`, AgOpenGPS uses a track mode system:

```csharp
public enum TrackMode {
    None = 0,
    AB = 2,              // Straight AB line
    Curve = 4,           // Curved line
    bndTrackOuter = 8,   // Boundary-based track (outer)
    bndTrackInner = 16,  // Boundary-based track (inner)
    bndCurve = 32,       // Boundary curve
    waterPivot = 64      // Pivot irrigation
}
```

### CTrk Class (Track Object):
```csharp
public class CTrk
{
    public TrackMode mode;                  // Track type
    public string name;                     // Display name
    public double heading;                  // Heading for AB lines
    public vec2 ptA, ptB;                  // A and B points for AB lines
    public vec2 endPtA, endPtB;            // Extended endpoints
    public List<vec3> curvePts;            // Curve points (for curved lines)
    public double nudgeDistance;           // Lateral adjustment
    public bool isVisible;                 // Show on map
}
```

**Key Insight**: Both AB lines and curves are stored in a **single track list** (`List<CTrk> gArr`), differentiated by `mode`.

---

## 4. Updated Phase 8/9 Plan

### Original Plan Issues:
- ❌ Assumed AB lines were simple (just point + heading)
- ❌ Didn't account for curve lines (complex multi-point paths)
- ❌ Underestimated guidance line complexity

### Revised Plan:

#### Phase 8: Field Management (Week 1-2)
- ✅ Field.txt origin (StartFix)
- ✅ Boundary.txt (outer + holes)
- ✅ Field selection/creation UI
- ✅ Boundary rendering
- ✅ Background images (BackPic)

#### Phase 9: Guidance Lines (Week 3-4)
Now split into two sub-phases:

**Phase 9a: AB Lines (Week 3)**
- ✅ Read/write ABLines.txt format
- ✅ Data model:
  ```csharp
  public class AbLine
  {
      public string Name { get; set; }
      public double Heading { get; set; }  // Degrees
      public Position Point { get; set; }   // Single point
      public TrackMode Mode => TrackMode.AB;
  }
  ```
- ✅ Render AB line on map (infinite line in heading direction)
- ✅ Create AB line UI:
  - Click point A, click point B (calc heading)
  - OR click point + enter heading
- ✅ Parallel guidance lines (based on tool width)

**Phase 9b: Curve Lines (Week 4)**
- ✅ Read/write CurveLines.txt format
- ✅ Data model:
  ```csharp
  public class CurveLine
  {
      public string Name { get; set; }
      public List<CurvePoint> Points { get; set; }
      public TrackMode Mode => TrackMode.Curve;
  }

  public class CurvePoint
  {
      public double Easting { get; set; }
      public double Northing { get; set; }
      public double Heading { get; set; }  // Radians!
  }
  ```
- ✅ Render curve path on map (polyline with 100s of points)
- ✅ Record curve UI:
  - "Start Recording" button
  - Drive the path (collect GPS points + headings)
  - "Stop Recording" button
  - Save with name
- ✅ Curve smoothing (optional feature from CABCurve.cs)

---

## 5. File Format Specifications

### ABLines.txt Complete Format

**Multiple AB lines in one file**:
```
AB 80°,79.95161324,-208.652,90.491
AB 90°,80.00000000,0.000,0.000
North-South,0.00000000,100.000,200.000
```

**Structure**:
- No header (unlike Boundary.txt)
- One line per AB line
- Format: `Name,Heading,Easting,Northing`
- Heading in **degrees** (0-360)
- Coordinates in meters (local system)

**Parsing Requirements**:
- Split on comma
- `CultureInfo.InvariantCulture` for decimal parsing
- Handle empty file (no AB lines)
- Validate heading (0-360 range)

### CurveLines.txt Complete Format

**Multiple curves in one file**:
```
$CurveLines
FirstCurveName
MetadataField
PointCount1
E1,N1,H1
E2,N2,H2
...
SecondCurveName
MetadataField
PointCount2
E1,N1,H1
...
```

**Structure**:
- Header: `$CurveLines`
- For each curve:
  - Name (string)
  - Metadata field (unknown - possibly nudge distance?)
  - Point count (integer)
  - Points: `Easting,Northing,Heading` (one per line)
- Heading in **radians** (not degrees!)
- Coordinates in meters (local system)

**Parsing Requirements**:
- Read header, skip it
- Loop: read name, metadata, count, then count lines
- Headings are **radians** (convert for display if needed)
- Handle 100s of points efficiently
- `CultureInfo.InvariantCulture` for decimals

---

## 6. OpenGL Rendering Considerations

### AB Line Rendering:
```csharp
// Draw infinite line in both directions from point
double lineLength = 2000; // meters (extend far)
vec2 startPt = new vec2(
    point.Easting - Math.Sin(headingRad) * lineLength,
    point.Northing - Math.Cos(headingRad) * lineLength
);
vec2 endPt = new vec2(
    point.Easting + Math.Sin(headingRad) * lineLength,
    point.Northing + Math.Cos(headingRad) * lineLength
);

// Draw line
GL.Begin(PrimitiveType.Lines);
GL.Vertex3(startPt.Easting, startPt.Northing, 0);
GL.Vertex3(endPt.Easting, endPt.Northing, 0);
GL.End();
```

### Curve Line Rendering:
```csharp
// Draw polyline through all points
GL.LineWidth(3);
GL.Begin(PrimitiveType.LineStrip);
foreach (var point in curvePoints)
{
    GL.Vertex3(point.Easting, point.Northing, 0);
}
GL.End();
```

**Performance Note**: Curves with 660 points are manageable for modern OpenGL. Use VBO for efficiency.

### Parallel Lines (Side Guides):
Both AB and curve lines need **parallel offset lines** for guidance:
```csharp
// Calculate perpendicular offset
double toolWidth = 3.0; // meters (from vehicle config)
double perpHeading = heading + Math.PI / 2; // 90 degrees perpendicular

for (int i = 1; i <= numGuideLines; i++)
{
    double offset = toolWidth * i;
    // Draw offset line at +offset and -offset
}
```

---

## 7. Data Models for AgValoniaGPS

### Unified Track Model:
```csharp
public enum TrackMode
{
    None,
    AB,           // Straight AB line
    Curve,        // Curved recorded path
    BoundaryOuter,
    BoundaryInner,
    WaterPivot
}

public abstract class GuidanceLine
{
    public string Name { get; set; }
    public TrackMode Mode { get; }
    public double NudgeDistance { get; set; } // Lateral adjustment
    public bool IsVisible { get; set; } = true;
}

public class AbLine : GuidanceLine
{
    public AbLine() { Mode = TrackMode.AB; }
    public double Heading { get; set; }  // Degrees
    public Position Point { get; set; }  // Single point
}

public class CurveLine : GuidanceLine
{
    public CurveLine() { Mode = TrackMode.Curve; }
    public List<CurvePoint> Points { get; set; }
}

public class CurvePoint
{
    public double Easting { get; set; }
    public double Northing { get; set; }
    public double Heading { get; set; }  // Radians
}
```

### Service Interfaces:
```csharp
public interface IGuidanceLineService
{
    List<GuidanceLine> LoadGuidanceLines(string fieldDirectory);
    void SaveGuidanceLines(List<GuidanceLine> lines, string fieldDirectory);
}

public interface IAbLineService
{
    List<AbLine> LoadAbLines(string fieldDirectory);
    void SaveAbLines(List<AbLine> lines, string fieldDirectory);
}

public interface ICurveLineService
{
    List<CurveLine> LoadCurveLines(string fieldDirectory);
    void SaveCurveLines(List<CurveLine> lines, string fieldDirectory);
}
```

---

## 8. Migration Compatibility

### Critical Requirements:
1. ✅ **ABLines.txt**: Must read/write identical format
2. ✅ **CurveLines.txt**: Must preserve all points and headings
3. ✅ **Heading units**:
   - AB lines: **degrees**
   - Curve lines: **radians**
   - Easy mistake! Must convert correctly
4. ✅ **Decimal precision**: Use `CultureInfo.InvariantCulture`
5. ✅ **Point count**: Curves can have 600+ points

### Round-Trip Test:
```csharp
[Test]
public void AbLineRoundTrip()
{
    // Load from original AgOpenGPS file
    var original = AbLineService.Load("TestField/ABLines.txt");

    // Save in AgValoniaGPS
    AbLineService.Save(original, "TestField2/ABLines.txt");

    // Reload and compare
    var reloaded = AbLineService.Load("TestField2/ABLines.txt");

    Assert.That(reloaded[0].Name, Is.EqualTo(original[0].Name));
    Assert.That(reloaded[0].Heading, Is.EqualTo(original[0].Heading).Within(0.0001));
    Assert.That(reloaded[0].Point.Easting, Is.EqualTo(original[0].Point.Easting).Within(0.001));
}
```

---

## 9. UI/UX Considerations

### AB Line Creation:
**Method 1: Two Points**
1. User clicks "Create AB Line"
2. User clicks point A on map
3. User clicks point B on map
4. Calculate heading from A to B
5. Store single point (A) + heading
6. Prompt for name

**Method 2: Point + Heading**
1. User clicks "Create AB Line"
2. User clicks point on map
3. User enters or adjusts heading (0-360°)
4. Visual preview shows line rotating
5. Prompt for name

### Curve Line Recording:
1. User clicks "Record Curve"
2. User drives/moves along desired path
3. System records GPS position + heading every 1 meter (or time interval)
4. User clicks "Stop Recording"
5. System shows preview of recorded curve
6. Optional: Smooth curve (reduce points, average headings)
7. Prompt for name
8. Save to CurveLines.txt

### Guidance Display:
- **Current guidance line**: Bright color (magenta in original)
- **Reference line**: Dashed red
- **Parallel lines**: Semi-transparent green
- **Line labels**: Show name at top of screen
- **Distance indicator**: Show cross-track error

---

## 10. Performance Optimization

### Curve Lines with 600+ Points:
- ✅ Use OpenGL VBO (Vertex Buffer Object) for rendering
- ✅ Don't recreate buffers every frame
- ✅ Only update when curve changes
- ✅ Use LineStrip primitive (not individual lines)

### Example VBO Setup:
```csharp
// Create VBO once
private uint _curveVbo;
private int _curvePointCount;

private void InitializeCurveVbo(List<CurvePoint> points)
{
    _gl.GenBuffers(1, out _curveVbo);
    _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _curveVbo);

    // Pack points into float array
    float[] vertices = new float[points.Count * 2]; // x, y per point
    for (int i = 0; i < points.Count; i++)
    {
        vertices[i * 2] = (float)points[i].Easting;
        vertices[i * 2 + 1] = (float)points[i].Northing;
    }

    unsafe
    {
        fixed (float* ptr = vertices)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer,
                (nuint)(vertices.Length * sizeof(float)),
                ptr, BufferUsageARB.StaticDraw);
        }
    }

    _curvePointCount = points.Count;
}

// Render efficiently
private void RenderCurve()
{
    _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _curveVbo);
    _gl.DrawArrays(PrimitiveType.LineStrip, 0, (uint)_curvePointCount);
}
```

---

## Conclusion

**What Changed**: AgOpenGPS guidance is more complex than initially thought:
- ✅ AB lines are simple (point + heading)
- ❌ Curve lines are complex (100s of points with individual headings)
- ❌ Must support BOTH for full compatibility

**Updated Strategy**:
1. **Phase 8**: Field management (boundaries, background images)
2. **Phase 9a**: AB lines (simple straight guidance)
3. **Phase 9b**: Curve lines (recorded curved paths)
4. **Phase 10**: Section control
5. **Phase 11**: AutoSteer integration

**Critical Success Factors**:
- ✅ Support both ABLines.txt and CurveLines.txt
- ✅ Remember: AB headings in degrees, curve headings in radians
- ✅ Handle 600+ point curves efficiently
- ✅ Maintain perfect file format compatibility

**Ready to proceed with Phase 8** (fields + boundaries), knowing Phase 9 is split into 9a (AB) and 9b (Curves). 🎯
