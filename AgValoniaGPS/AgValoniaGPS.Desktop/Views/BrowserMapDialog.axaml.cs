using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace AgValoniaGPS.Desktop.Views;

/// <summary>
/// Result from the Browser Map boundary dialog.
/// </summary>
public class BrowserMapBoundaryResult
{
    public List<(double Latitude, double Longitude)> BoundaryPoints { get; set; } = new();

    // Background image data
    public bool HasBackgroundImage { get; set; }
    public string? BackgroundImagePath { get; set; }
    public double NorthWestLat { get; set; }
    public double NorthWestLon { get; set; }
    public double SouthEastLat { get; set; }
    public double SouthEastLon { get; set; }
}

public partial class BrowserMapDialog : Window
{
    private readonly double _initialLatitude;
    private readonly double _initialLongitude;
    private readonly string _tempDirectory;
    private readonly string _boundaryFilePath;
    private readonly string _htmlFilePath;
    private List<(double Latitude, double Longitude)> _boundaryPoints = new();
    private DispatcherTimer? _checkTimer;

    // Background image data
    private string? _backgroundImagePath;
    private double _nwLat, _nwLon, _seLat, _seLon;
    private bool _hasBackgroundImage;

    public BrowserMapBoundaryResult? Result { get; private set; }

    public BrowserMapDialog() : this(0, 0)
    {
    }

    public BrowserMapDialog(double latitude, double longitude)
    {
        _initialLatitude = latitude;
        _initialLongitude = longitude;

        // Create temp directory for our files
        _tempDirectory = Path.Combine(Path.GetTempPath(), "AgValoniaGPS_BingMap");
        Directory.CreateDirectory(_tempDirectory);

        _boundaryFilePath = Path.Combine(_tempDirectory, "boundary_points.txt");
        _htmlFilePath = Path.Combine(_tempDirectory, "bing_map.html");

        InitializeComponent();

        // Clean up any old boundary file
        if (File.Exists(_boundaryFilePath))
        {
            try { File.Delete(_boundaryFilePath); } catch { }
        }

        // Create the HTML file
        CreateHtmlFile();
    }

    private void CreateHtmlFile()
    {
        // Use Leaflet.js with ESRI satellite tiles and html2canvas for map capture
        var html = $@"<!DOCTYPE html>
<html>
<head>
    <title>AgValoniaGPS - Draw Boundary</title>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <link rel=""stylesheet"" href=""https://unpkg.com/leaflet@1.9.4/dist/leaflet.css"" />
    <script src=""https://unpkg.com/leaflet@1.9.4/dist/leaflet.js""></script>
    <script src=""https://cdnjs.cloudflare.com/ajax/libs/html2canvas/1.4.1/html2canvas.min.js""></script>
    <style>
        body {{ margin: 0; padding: 0; font-family: Arial, sans-serif; }}
        #map {{ position: absolute; top: 50px; bottom: 60px; left: 0; right: 0; }}
        #header {{
            position: absolute; top: 0; left: 0; right: 0; height: 50px;
            background: #2C3E50; color: white; display: flex; align-items: center;
            padding: 0 15px; box-sizing: border-box;
        }}
        #header h1 {{ margin: 0; font-size: 18px; flex: 1; }}
        #header .info {{ font-size: 14px; color: #1ABC9C; }}
        #controls {{
            position: absolute; bottom: 0; left: 0; right: 0; height: 60px;
            background: #34495E; display: flex; align-items: center;
            padding: 0 15px; box-sizing: border-box; gap: 10px;
        }}
        .btn {{
            padding: 10px 20px; border: none; border-radius: 4px; cursor: pointer;
            font-size: 14px; font-weight: bold;
        }}
        .btn-draw {{ background: #3498DB; color: white; }}
        .btn-draw.active {{ background: #1ABC9C; }}
        .btn-undo {{ background: #E67E22; color: white; }}
        .btn-clear {{ background: #C0392B; color: white; }}
        .btn-save {{ background: #27AE60; color: white; }}
        .btn-bg {{ background: #9B59B6; color: white; }}
        .btn:hover {{ opacity: 0.9; }}
        .btn:disabled {{ opacity: 0.5; cursor: not-allowed; }}
        #pointCount {{ color: #1ABC9C; font-size: 14px; margin-left: auto; }}
        #instructions {{
            position: absolute; top: 60px; left: 10px; background: rgba(44,62,80,0.9);
            color: white; padding: 10px; border-radius: 4px; font-size: 12px; z-index: 1000;
            max-width: 280px;
        }}
        #instructions.hidden {{ display: none; }}
        #savedMsg {{
            position: fixed; top: 50%; left: 50%; transform: translate(-50%, -50%);
            background: #27AE60; color: white; padding: 20px 40px; border-radius: 8px;
            font-size: 18px; font-weight: bold; display: none; z-index: 2000;
        }}
        #loadingMsg {{
            position: fixed; top: 50%; left: 50%; transform: translate(-50%, -50%);
            background: #9B59B6; color: white; padding: 20px 40px; border-radius: 8px;
            font-size: 18px; font-weight: bold; display: none; z-index: 2000;
        }}
    </style>
</head>
<body>
    <div id=""header"">
        <h1>AgValoniaGPS - Draw Field Boundary</h1>
        <span class=""info"">Click map to add points</span>
    </div>

    <div id=""instructions"">
        <b>Instructions:</b><br>
        1. Click ""Enable Drawing"" to start<br>
        2. Click on the map to add boundary points<br>
        3. Click ""Save Boundary"" when done<br>
        4. Click ""Save Background"" to capture satellite image<br>
        5. Return to AgValoniaGPS and click ""Refresh""
    </div>

    <div id=""map""></div>

    <div id=""controls"">
        <button class=""btn btn-draw"" id=""btnDraw"" onclick=""toggleDrawing()"">Enable Drawing</button>
        <button class=""btn btn-undo"" id=""btnUndo"" onclick=""undoPoint()"" disabled>Undo</button>
        <button class=""btn btn-clear"" id=""btnClear"" onclick=""clearAll()"" disabled>Clear All</button>
        <button class=""btn btn-save"" id=""btnSave"" onclick=""saveBoundary()"" disabled>Save Boundary</button>
        <button class=""btn btn-bg"" id=""btnBg"" onclick=""saveBackground()"">Save Background</button>
        <span id=""pointCount"">Points: 0</span>
    </div>

    <div id=""savedMsg"">Saved! Return to AgValoniaGPS</div>
    <div id=""loadingMsg"">Capturing map...</div>

    <script>
        // Initialize map centered on GPS position or default
        var lat = {_initialLatitude.ToString(CultureInfo.InvariantCulture)};
        var lon = {_initialLongitude.ToString(CultureInfo.InvariantCulture)};

        // Default to US center if no GPS
        if (lat === 0 && lon === 0) {{
            lat = 39.8283;
            lon = -98.5795;
        }}

        var map = L.map('map', {{ preferCanvas: true }}).setView([lat, lon], 16);

        // Use Google satellite tiles (supports CORS for canvas export)
        L.tileLayer('https://mt1.google.com/vt/lyrs=s&x={{x}}&y={{y}}&z={{z}}', {{
            attribution: 'Map data &copy; Google',
            maxZoom: 20,
            crossOrigin: 'anonymous'
        }}).addTo(map);

        var boundaryPoints = [];
        var markers = [];
        var polygon = null;
        var polyline = null;
        var isDrawing = false;

        function toggleDrawing() {{
            isDrawing = !isDrawing;
            var btn = document.getElementById('btnDraw');
            if (isDrawing) {{
                btn.textContent = 'Stop Drawing';
                btn.classList.add('active');
                map.getContainer().style.cursor = 'crosshair';
                document.getElementById('instructions').classList.add('hidden');
            }} else {{
                btn.textContent = 'Enable Drawing';
                btn.classList.remove('active');
                map.getContainer().style.cursor = '';
            }}
        }}

        map.on('click', function(e) {{
            if (!isDrawing) return;

            var point = [e.latlng.lat, e.latlng.lng];
            boundaryPoints.push(point);

            // Add numbered marker
            var marker = L.circleMarker(e.latlng, {{
                radius: 8,
                fillColor: '#e74c3c',
                color: '#c0392b',
                weight: 2,
                fillOpacity: 1
            }}).addTo(map);

            // Add number label
            var label = L.divIcon({{
                className: 'point-label',
                html: '<div style=""background:white;border-radius:50%;width:20px;height:20px;text-align:center;line-height:20px;font-size:11px;font-weight:bold;border:1px solid #333;"">' + boundaryPoints.length + '</div>',
                iconSize: [20, 20],
                iconAnchor: [-5, -5]
            }});
            var labelMarker = L.marker(e.latlng, {{ icon: label }}).addTo(map);
            markers.push({{ circle: marker, label: labelMarker }});

            updatePolygon();
            updateUI();
        }});

        function updatePolygon() {{
            if (polyline) {{
                map.removeLayer(polyline);
                polyline = null;
            }}
            if (polygon) {{
                map.removeLayer(polygon);
                polygon = null;
            }}

            if (boundaryPoints.length >= 2) {{
                polyline = L.polyline(boundaryPoints, {{
                    color: 'white',
                    weight: 4,
                    opacity: 1
                }}).addTo(map);
            }}

            if (boundaryPoints.length >= 3) {{
                polygon = L.polygon(boundaryPoints, {{
                    color: 'white',
                    weight: 4,
                    fillColor: '#3498db',
                    fillOpacity: 0.2
                }}).addTo(map);
            }}
        }}

        function updateUI() {{
            var count = boundaryPoints.length;
            document.getElementById('pointCount').textContent = 'Points: ' + count;
            document.getElementById('btnUndo').disabled = count === 0;
            document.getElementById('btnClear').disabled = count === 0;
            document.getElementById('btnSave').disabled = count < 3;
        }}

        function undoPoint() {{
            if (boundaryPoints.length === 0) return;

            boundaryPoints.pop();
            var m = markers.pop();
            if (m) {{
                map.removeLayer(m.circle);
                map.removeLayer(m.label);
            }}
            updatePolygon();
            updateUI();
        }}

        function clearAll() {{
            boundaryPoints = [];
            markers.forEach(function(m) {{
                map.removeLayer(m.circle);
                map.removeLayer(m.label);
            }});
            markers = [];
            updatePolygon();
            updateUI();
        }}

        function saveBoundary() {{
            if (boundaryPoints.length < 3) {{
                alert('Need at least 3 points to create a boundary');
                return;
            }}

            // Create downloadable file content
            var content = boundaryPoints.map(function(p) {{
                return p[0].toFixed(8) + ',' + p[1].toFixed(8);
            }}).join('\n');

            downloadFile(content, 'boundary_points.txt', 'text/plain');
            showMessage('savedMsg');
        }}

        function saveBackground() {{
            document.getElementById('loadingMsg').style.display = 'block';

            // Hide markers and polygons for clean background
            markers.forEach(function(m) {{
                m.circle.setStyle({{ opacity: 0, fillOpacity: 0 }});
                m.label.setOpacity(0);
            }});
            if (polygon) polygon.setStyle({{ opacity: 0, fillOpacity: 0 }});
            if (polyline) polyline.setStyle({{ opacity: 0 }});

            // Get current map bounds
            var bounds = map.getBounds();
            var nw = bounds.getNorthWest();
            var se = bounds.getSouthEast();

            // Wait a moment for tiles to fully load, then capture
            setTimeout(function() {{
                // Use html2canvas to capture the map container
                var mapContainer = document.getElementById('map');
                html2canvas(mapContainer, {{
                    useCORS: true,
                    allowTaint: false,
                    logging: true,
                    imageTimeout: 15000,
                    proxy: null
                }}).then(function(canvas) {{
                // Restore markers and polygons
                markers.forEach(function(m) {{
                    m.circle.setStyle({{ opacity: 1, fillOpacity: 1 }});
                    m.label.setOpacity(1);
                }});
                if (polygon) polygon.setStyle({{ opacity: 1, fillOpacity: 0.2 }});
                if (polyline) polyline.setStyle({{ opacity: 1 }});

                document.getElementById('loadingMsg').style.display = 'none';

                // Convert canvas to blob and download
                canvas.toBlob(function(blob) {{
                    var url = URL.createObjectURL(blob);
                    var a = document.createElement('a');
                    a.href = url;
                    a.download = 'BackPic.png';
                    document.body.appendChild(a);
                    a.click();
                    document.body.removeChild(a);
                    URL.revokeObjectURL(url);

                    // Also download the geo-reference file
                    var geoContent = '$BackPic\n' +
                        'true\n' +
                        nw.lat.toFixed(8) + '\n' +
                        nw.lng.toFixed(8) + '\n' +
                        se.lat.toFixed(8) + '\n' +
                        se.lng.toFixed(8);
                    downloadFile(geoContent, 'BackPic.txt', 'text/plain');

                    showMessage('savedMsg');
                }}, 'image/png');
            }}).catch(function(err) {{
                // Restore markers and polygons on error
                markers.forEach(function(m) {{
                    m.circle.setStyle({{ opacity: 1, fillOpacity: 1 }});
                    m.label.setOpacity(1);
                }});
                if (polygon) polygon.setStyle({{ opacity: 1, fillOpacity: 0.2 }});
                if (polyline) polyline.setStyle({{ opacity: 1 }});

                document.getElementById('loadingMsg').style.display = 'none';
                alert('Error capturing map: ' + err);
            }});
            }}, 1000);  // Wait 1 second for tiles to load
        }}

        function downloadFile(content, filename, type) {{
            var blob = new Blob([content], {{ type: type }});
            var url = URL.createObjectURL(blob);
            var a = document.createElement('a');
            a.href = url;
            a.download = filename;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            URL.revokeObjectURL(url);
        }}

        function showMessage(id) {{
            document.getElementById(id).style.display = 'block';
            setTimeout(function() {{
                document.getElementById(id).style.display = 'none';
            }}, 3000);
        }}

        // Show current position marker if we have GPS
        if (lat !== 39.8283) {{
            L.marker([lat, lon]).addTo(map)
                .bindPopup('Current GPS Position')
                .openPopup();
        }}
    </script>
</body>
</html>";

        File.WriteAllText(_htmlFilePath, html);
    }

    private void BtnOpenMap_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            // Open HTML file in default browser
            OpenUrl(_htmlFilePath);
            StatusLabel.Text = "Map opened in browser - draw your boundary";

            // Start timer to check for saved boundary file
            StartCheckTimer();
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
        }
    }

    private void StartCheckTimer()
    {
        _checkTimer?.Stop();
        _checkTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _checkTimer.Tick += CheckForBoundaryFile;
        _checkTimer.Start();
    }

    private void CheckForBoundaryFile(object? sender, EventArgs e)
    {
        CheckAndLoadBoundaryFile();
    }

    private void CheckAndLoadBoundaryFile()
    {
        var downloadsFolder = GetDownloadsFolder();

        // Check for boundary_points.txt
        var boundaryFile = Path.Combine(downloadsFolder, "boundary_points.txt");
        if (File.Exists(boundaryFile))
        {
            try
            {
                LoadBoundaryPoints(boundaryFile);
                // Move file to temp to avoid re-loading
                var destPath = Path.Combine(_tempDirectory, "boundary_points_imported.txt");
                File.Move(boundaryFile, destPath, overwrite: true);
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"Error reading boundary file: {ex.Message}";
            }
        }

        // Check for background image files (BackPic.png and BackPic.txt)
        var backPicFile = Path.Combine(downloadsFolder, "BackPic.png");
        var backPicGeo = Path.Combine(downloadsFolder, "BackPic.txt");
        if (File.Exists(backPicFile) && File.Exists(backPicGeo))
        {
            try
            {
                LoadBackgroundImage(backPicFile, backPicGeo);
                // Move files to temp to avoid re-loading
                var destPng = Path.Combine(_tempDirectory, "BackPic_imported.png");
                var destTxt = Path.Combine(_tempDirectory, "BackPic_imported.txt");
                File.Move(backPicFile, destPng, overwrite: true);
                File.Move(backPicGeo, destTxt, overwrite: true);
                _backgroundImagePath = destPng;
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"Error reading background: {ex.Message}";
            }
        }
    }

    private void LoadBackgroundImage(string pngPath, string geoPath)
    {
        var lines = File.ReadAllLines(geoPath);
        if (lines.Length >= 6)
        {
            // Format: $BackPic, true, nwLat, nwLon, seLat, seLon
            if (double.TryParse(lines[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var nwLat) &&
                double.TryParse(lines[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var nwLon) &&
                double.TryParse(lines[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var seLat) &&
                double.TryParse(lines[5], NumberStyles.Float, CultureInfo.InvariantCulture, out var seLon))
            {
                _nwLat = nwLat;
                _nwLon = nwLon;
                _seLat = seLat;
                _seLon = seLon;
                _hasBackgroundImage = true;

                BtnImport.IsEnabled = true;
                UpdateStatusLabel();
            }
        }
    }

    private void LoadBoundaryPoints(string filePath)
    {
        _boundaryPoints.Clear();
        var lines = File.ReadAllLines(filePath);

        foreach (var line in lines)
        {
            var parts = line.Split(',');
            if (parts.Length >= 2 &&
                double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) &&
                double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
            {
                _boundaryPoints.Add((lat, lon));
            }
        }

        if (_boundaryPoints.Count >= 3)
        {
            BtnImport.IsEnabled = true;
            UpdateStatusLabel();
        }
        else if (_boundaryPoints.Count > 0)
        {
            PointCountLabel.Text = $"Only {_boundaryPoints.Count} points - need at least 3";
        }
    }

    private void UpdateStatusLabel()
    {
        var parts = new List<string>();
        if (_boundaryPoints.Count >= 3)
            parts.Add($"Boundary ({_boundaryPoints.Count} pts)");
        if (_hasBackgroundImage)
            parts.Add("Background image");

        if (parts.Count > 0)
        {
            StatusLabel.Text = $"Ready to import: {string.Join(" + ", parts)}";
            PointCountLabel.Text = "";
        }
    }

    private void BtnRefresh_Click(object? sender, RoutedEventArgs e)
    {
        CheckAndLoadBoundaryFile();

        if (_boundaryPoints.Count == 0 && !_hasBackgroundImage)
        {
            StatusLabel.Text = "No files found in Downloads folder";
        }
    }

    private void BtnCancel_Click(object? sender, RoutedEventArgs e)
    {
        _checkTimer?.Stop();
        Result = null;
        Close(false);
    }

    private void BtnImport_Click(object? sender, RoutedEventArgs e)
    {
        if (_boundaryPoints.Count < 3 && !_hasBackgroundImage)
        {
            StatusLabel.Text = "Need boundary points or background image";
            return;
        }

        _checkTimer?.Stop();
        Result = new BrowserMapBoundaryResult
        {
            BoundaryPoints = new List<(double, double)>(_boundaryPoints),
            HasBackgroundImage = _hasBackgroundImage,
            BackgroundImagePath = _backgroundImagePath,
            NorthWestLat = _nwLat,
            NorthWestLon = _nwLon,
            SouthEastLat = _seLat,
            SouthEastLon = _seLon
        };
        Close(true);
    }

    private static string GetDownloadsFolder()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        }
        else // Linux
        {
            var xdgDownload = Environment.GetEnvironmentVariable("XDG_DOWNLOAD_DIR");
            if (!string.IsNullOrEmpty(xdgDownload) && Directory.Exists(xdgDownload))
            {
                return xdgDownload;
            }
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        }
    }

    private static void OpenUrl(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", url);
        }
        else // Linux
        {
            Process.Start("xdg-open", url);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _checkTimer?.Stop();
        base.OnClosed(e);
    }
}
