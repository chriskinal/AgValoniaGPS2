using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using BruTile.Predefined;
using BruTile.Web;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling.Layers;
using Mapsui.UI.Avalonia;
using NetTopologySuite.Geometries;
using NtsPoint = NetTopologySuite.Geometries.Point;

namespace AgValoniaGPS.Desktop.Views;

/// <summary>
/// Result from the Mapsui boundary dialog.
/// </summary>
public class MapsuiBoundaryResult
{
    public List<(double Latitude, double Longitude)> BoundaryPoints { get; set; } = new();
    public bool HasBackgroundImage { get; set; }
    public string? BackgroundImagePath { get; set; }
    public double NorthWestLat { get; set; }
    public double NorthWestLon { get; set; }
    public double SouthEastLat { get; set; }
    public double SouthEastLon { get; set; }
}

public partial class MapsuiBoundaryDialog : Window
{
    private readonly double _initialLatitude;
    private readonly double _initialLongitude;
    private readonly List<(double Lat, double Lon)> _boundaryPoints = new();
    private WritableLayer? _pointsLayer;
    private WritableLayer? _polygonLayer;
    private bool _isDrawingMode;
    private string? _savedBackgroundPath;
    private double _savedNwLat, _savedNwLon, _savedSeLat, _savedSeLon;
    private bool _hasBackground;

    public MapsuiBoundaryResult? Result { get; private set; }

    public MapsuiBoundaryDialog() : this(0, 0)
    {
    }

    public MapsuiBoundaryDialog(double latitude, double longitude)
    {
        _initialLatitude = latitude;
        _initialLongitude = longitude;

        InitializeComponent();

        // Setup map after initialization
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        SetupMap();
    }

    private void SetupMap()
    {
        var map = new Mapsui.Map();

        // Add Esri World Imagery (satellite) as base layer - free, no API key required
        var esriSatelliteUrl = "https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}";
        var esriTileSource = new HttpTileSource(
            new GlobalSphericalMercator(),
            esriSatelliteUrl,
            name: "Esri World Imagery");
        map.Layers.Add(new TileLayer(esriTileSource) { Name = "Satellite" });

        // Create layer for polygon (drawn below points)
        _polygonLayer = new WritableLayer
        {
            Name = "Polygon",
            Style = new VectorStyle
            {
                Fill = new Mapsui.Styles.Brush(new Mapsui.Styles.Color(52, 152, 219, 50)), // Semi-transparent blue
                Line = new Mapsui.Styles.Pen(new Mapsui.Styles.Color(255, 255, 255, 255), 3) // White outline
            }
        };
        map.Layers.Add(_polygonLayer);

        // Create layer for boundary points
        _pointsLayer = new WritableLayer
        {
            Name = "Points",
            Style = new SymbolStyle
            {
                Fill = new Mapsui.Styles.Brush(new Mapsui.Styles.Color(231, 76, 60, 255)), // Red
                Outline = new Mapsui.Styles.Pen(new Mapsui.Styles.Color(192, 57, 43, 255), 2),
                SymbolScale = 0.5
            }
        };
        map.Layers.Add(_pointsLayer);

        // Set initial position
        double lat = _initialLatitude;
        double lon = _initialLongitude;

        // Default to US center if no GPS
        if (Math.Abs(lat) < 0.01 && Math.Abs(lon) < 0.01)
        {
            lat = 39.8283;
            lon = -98.5795;
        }

        // Convert to SphericalMercator
        var center = SphericalMercator.FromLonLat(lon, lat);
        map.Navigator.CenterOnAndZoomTo(new MPoint(center.x, center.y), map.Navigator.Resolutions[16]);

        MapControl.Map = map;

        // Disable all debug/performance overlays and widgets
        map.Widgets.Clear();

        // Handle map clicks via pointer events
        MapControl.PointerPressed += OnMapPointerPressed;

        // Handle pointer movement for coordinate display
        MapControl.PointerMoved += OnPointerMoved;
    }

    private void OnMapPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!_isDrawingMode)
            return;

        var point = e.GetCurrentPoint(MapControl);
        if (point.Properties.IsLeftButtonPressed)
        {
            var viewport = MapControl.Map.Navigator.Viewport;
            var worldPos = viewport.ScreenToWorldXY(point.Position.X, point.Position.Y);

            // Convert from SphericalMercator to WGS84
            var lonLat = SphericalMercator.ToLonLat(worldPos.worldX, worldPos.worldY);

            AddBoundaryPoint(lonLat.lat, lonLat.lon);

            e.Handled = true;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var position = e.GetPosition(MapControl);
        var viewport = MapControl.Map.Navigator.Viewport;
        var worldPos = viewport.ScreenToWorldXY(position.X, position.Y);

        // Convert from SphericalMercator to WGS84
        var lonLat = SphericalMercator.ToLonLat(worldPos.worldX, worldPos.worldY);

        CoordinateLabel.Text = $"Lat: {lonLat.lat:F6}, Lon: {lonLat.lon:F6}";
    }

    private void AddBoundaryPoint(double lat, double lon)
    {
        _boundaryPoints.Add((lat, lon));

        // Add point marker
        var mercator = SphericalMercator.FromLonLat(lon, lat);
        var point = new GeometryFeature(new NtsPoint(mercator.x, mercator.y));
        _pointsLayer?.Add(point);

        UpdatePolygon();
        UpdateUI();

        MapControl.Refresh();
    }

    private void UpdatePolygon()
    {
        _polygonLayer?.Clear();

        if (_boundaryPoints.Count >= 3)
        {
            // Create polygon from points
            var coordinates = new List<Coordinate>();
            foreach (var (lat, lon) in _boundaryPoints)
            {
                var mercator = SphericalMercator.FromLonLat(lon, lat);
                coordinates.Add(new Coordinate(mercator.x, mercator.y));
            }
            // Close the polygon
            var first = _boundaryPoints[0];
            var firstMercator = SphericalMercator.FromLonLat(first.Lon, first.Lat);
            coordinates.Add(new Coordinate(firstMercator.x, firstMercator.y));

            var ring = new LinearRing(coordinates.ToArray());
            var polygon = new Polygon(ring);
            var feature = new GeometryFeature(polygon);
            _polygonLayer?.Add(feature);
        }
        else if (_boundaryPoints.Count >= 2)
        {
            // Draw line between points
            var coordinates = new List<Coordinate>();
            foreach (var (lat, lon) in _boundaryPoints)
            {
                var mercator = SphericalMercator.FromLonLat(lon, lat);
                coordinates.Add(new Coordinate(mercator.x, mercator.y));
            }
            var line = new LineString(coordinates.ToArray());
            var feature = new GeometryFeature(line);
            _polygonLayer?.Add(feature);
        }
    }

    private void UpdateUI()
    {
        var count = _boundaryPoints.Count;
        PointCountLabel.Text = $"Points: {count}";
        BtnUndo.IsEnabled = count > 0;
        BtnClear.IsEnabled = count > 0;
        BtnSave.IsEnabled = count >= 3;
    }

    private void BtnDraw_Click(object? sender, RoutedEventArgs e)
    {
        _isDrawingMode = !_isDrawingMode;

        if (_isDrawingMode)
        {
            BtnDraw.Classes.Add("active");
            ((TextBlock)BtnDraw.Content!).Text = "Stop Drawing";
            MapControl.Cursor = new Cursor(StandardCursorType.Cross);
        }
        else
        {
            BtnDraw.Classes.Remove("active");
            ((TextBlock)BtnDraw.Content!).Text = "Draw";
            MapControl.Cursor = Cursor.Default;
        }
    }

    private void BtnUndo_Click(object? sender, RoutedEventArgs e)
    {
        if (_boundaryPoints.Count == 0)
            return;

        _boundaryPoints.RemoveAt(_boundaryPoints.Count - 1);

        // Rebuild points layer
        _pointsLayer?.Clear();
        foreach (var (lat, lon) in _boundaryPoints)
        {
            var mercator = SphericalMercator.FromLonLat(lon, lat);
            var point = new GeometryFeature(new NtsPoint(mercator.x, mercator.y));
            _pointsLayer?.Add(point);
        }

        UpdatePolygon();
        UpdateUI();
        MapControl.Refresh();
    }

    private void BtnClear_Click(object? sender, RoutedEventArgs e)
    {
        _boundaryPoints.Clear();
        _pointsLayer?.Clear();
        _polygonLayer?.Clear();
        UpdateUI();
        MapControl.Refresh();
    }

    private async Task<bool> CaptureBackgroundImageAsync()
    {
        try
        {
            // Get current viewport bounds
            var viewport = MapControl.Map.Navigator.Viewport;

            // Get the extent from viewport using extension method
            var worldMin = viewport.ScreenToWorldXY(0, viewport.Height);
            var worldMax = viewport.ScreenToWorldXY(viewport.Width, 0);

            // Convert extent corners to WGS84
            var nw = SphericalMercator.ToLonLat(worldMin.worldX, worldMax.worldY);
            var se = SphericalMercator.ToLonLat(worldMax.worldX, worldMin.worldY);

            _savedNwLat = nw.lat;
            _savedNwLon = nw.lon;
            _savedSeLat = se.lat;
            _savedSeLon = se.lon;

            // Export the map to a bitmap
            var tempDir = Path.Combine(Path.GetTempPath(), "AgValoniaGPS_Mapsui");
            Directory.CreateDirectory(tempDir);
            _savedBackgroundPath = Path.Combine(tempDir, "BackPic.png");

            // Hide drawing layers before capture
            if (_pointsLayer != null) _pointsLayer.Enabled = false;
            if (_polygonLayer != null) _polygonLayer.Enabled = false;
            MapControl.Refresh();

            // Small delay to ensure the map redraws without the layers
            await Task.Delay(100);

            // Get the size of the MapControl
            var bounds = MapControl.Bounds;
            var pixelSize = new PixelSize((int)bounds.Width, (int)bounds.Height);

            if (pixelSize.Width > 0 && pixelSize.Height > 0)
            {
                // Create a RenderTargetBitmap to capture the control
                var renderTarget = new RenderTargetBitmap(pixelSize);
                renderTarget.Render(MapControl);

                // Save the bitmap to a file
                renderTarget.Save(_savedBackgroundPath);

                Console.WriteLine($"Background image saved to: {_savedBackgroundPath}");
            }

            // Re-enable drawing layers
            if (_pointsLayer != null) _pointsLayer.Enabled = true;
            if (_polygonLayer != null) _polygonLayer.Enabled = true;
            MapControl.Refresh();

            // Create geo-reference file content
            var geoPath = Path.Combine(tempDir, "BackPic.txt");
            var geoContent = $"$BackPic\ntrue\n{_savedNwLat.ToString(CultureInfo.InvariantCulture)}\n{_savedNwLon.ToString(CultureInfo.InvariantCulture)}\n{_savedSeLat.ToString(CultureInfo.InvariantCulture)}\n{_savedSeLon.ToString(CultureInfo.InvariantCulture)}";
            File.WriteAllText(geoPath, geoContent);

            Console.WriteLine($"Background geo-reference saved: NW({_savedNwLat:F6}, {_savedNwLon:F6}) SE({_savedSeLat:F6}, {_savedSeLon:F6})");

            _hasBackground = true;
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error capturing background: {ex.Message}");

            // Re-enable drawing layers on error
            if (_pointsLayer != null) _pointsLayer.Enabled = true;
            if (_polygonLayer != null) _polygonLayer.Enabled = true;
            MapControl.Refresh();

            return false;
        }
    }

    private void BtnCancel_Click(object? sender, RoutedEventArgs e)
    {
        Result = null;
        Close(false);
    }

    private async void BtnSave_Click(object? sender, RoutedEventArgs e)
    {
        if (_boundaryPoints.Count < 3)
            return;

        var includeBackground = ChkSaveBackground.IsChecked == true;

        // Capture background automatically if checkbox is checked
        if (includeBackground)
        {
            BtnSave.IsEnabled = false;
            ((TextBlock)BtnSave.Content!).Text = "Saving...";

            var success = await CaptureBackgroundImageAsync();
            if (!success)
            {
                includeBackground = false;
            }

            BtnSave.IsEnabled = true;
            ((TextBlock)BtnSave.Content!).Text = "Save";
        }

        Result = new MapsuiBoundaryResult
        {
            BoundaryPoints = new List<(double, double)>(_boundaryPoints),
            HasBackgroundImage = includeBackground && _hasBackground,
            BackgroundImagePath = (includeBackground && _hasBackground) ? _savedBackgroundPath : null,
            NorthWestLat = _savedNwLat,
            NorthWestLon = _savedNwLon,
            SouthEastLat = _savedSeLat,
            SouthEastLon = _savedSeLon
        };

        Close(true);
    }
}
