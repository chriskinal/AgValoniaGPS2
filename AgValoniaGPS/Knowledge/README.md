# Knowledge Base

This folder contains documentation and reference materials for AgValoniaGPS development.

## Contents

### AB_LINE_FORMATS.md
Comprehensive analysis of AgOpenGPS guidance line file formats:
- **ABLines.txt** - Straight guidance lines (point + heading)
- **CurveLines.txt** - Curved/contour paths (hundreds of points)
- File format specifications with examples
- Data models and OpenGL rendering guidance
- Critical compatibility notes (AB: degrees, Curves: radians!)

### AgOpenGPS_File_Struct/
Sample field data from AgOpenGPS showing all file types:
- **Fields/** - Multiple example fields with complete file sets
- **Vehicles/** - Vehicle configuration XML examples
- **AgIO/** - AgIO configuration examples
- **Logs/** - Event log examples

Use these as reference when implementing file I/O compatibility.

## Key Insights

1. **Guidance Lines**: Two distinct types requiring different parsers
2. **File Compatibility**: Must maintain exact format compatibility with original AgOpenGPS
3. **Decimal Culture**: Always use `CultureInfo.InvariantCulture` for parsing
4. **Heading Units**: AB lines use degrees, curve lines use radians
5. **Performance**: Curve lines can have 600+ points - use VBO for rendering

## Platform Compatibility

See platform compatibility analysis in session notes:
- ✅ Windows, Linux, macOS - Ready (4 minor project changes needed)
- ✅ Android - Viable (needs platform-specific GPS/serial implementations)
- ❌ iOS - Waiting for Avalonia.iOS maturity
