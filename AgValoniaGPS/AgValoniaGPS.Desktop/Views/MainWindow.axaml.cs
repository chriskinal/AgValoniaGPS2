using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Layout;
using Microsoft.Extensions.DependencyInjection;
using AgValoniaGPS.ViewModels;
using AgValoniaGPS.Services;
using AgValoniaGPS.Services.Interfaces;
using AgValoniaGPS.Models;
using AgOpenGPS.Core.Models;
using AgOpenGPS.Core.Models.Base;

namespace AgValoniaGPS.Desktop.Views;

public partial class MainWindow : Window
{
    private MainViewModel? ViewModel => DataContext as MainViewModel;
    private bool _isDraggingSection = false;
    private bool _isDraggingLeftPanel = false;
    private bool _isDraggingFileMenu = false;
    private bool _isDraggingViewSettings = false;
    private bool _isDraggingTools = false;
    private bool _isDraggingConfiguration = false;
    private bool _isDraggingJobMenu = false;
    private bool _isDraggingFieldTools = false;
    private bool _isDraggingSimulator = false;
    private bool _isDraggingBoundary = false;
    private bool _isDraggingBoundaryPlayer = false;
    private Avalonia.Point _dragStartPoint;
    // Boundary Player state
    private bool _isDrawRightSide = true;
    private bool _isDrawAtPivot = false;
    private bool _isRecording = false;
    private bool _isBoundarySectionControlOn = false;
    private double _boundaryOffset = 0; // Offset in centimeters
    private DateTime _leftPanelPressTime;
    private const int TapTimeThresholdMs = 300;
    private const double TapDistanceThreshold = 5.0;

    public MainWindow()
    {
        InitializeComponent();

        // Set DataContext from DI
        if (App.Services != null)
        {
            DataContext = App.Services.GetRequiredService<MainViewModel>();
        }

        // Handle window resize to keep section control in bounds
        this.PropertyChanged += MainWindow_PropertyChanged;

        // Load window settings AFTER window is opened to avoid Avalonia overriding them
        this.Opened += MainWindow_Opened;

        // Subscribe to GPS position changes
        if (ViewModel != null)
        {
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        // Add keyboard shortcut for 3D mode toggle (F3)
        this.KeyDown += MainWindow_KeyDown;

        // Save window settings on close
        this.Closing += MainWindow_Closing;
    }

    private void MainWindow_Opened(object? sender, EventArgs e)
    {
        // Load settings after window is opened
        LoadWindowSettings();
    }

    private void LoadWindowSettings()
    {
        if (App.Services == null) return;

        var settingsService = App.Services.GetRequiredService<ISettingsService>();
        var settings = settingsService.Settings;

        // Apply window size and position
        if (settings.WindowWidth > 0 && settings.WindowHeight > 0)
        {
            Width = settings.WindowWidth;
            Height = settings.WindowHeight;
        }

        if (settings.WindowX >= 0 && settings.WindowY >= 0)
        {
            Position = new PixelPoint((int)settings.WindowX, (int)settings.WindowY);
        }

        if (settings.WindowMaximized)
        {
            WindowState = WindowState.Maximized;
        }

        // Apply simulator panel position if saved
        if (SimulatorPanel != null && !double.IsNaN(settings.SimulatorPanelX) && !double.IsNaN(settings.SimulatorPanelY))
        {
            Canvas.SetLeft(SimulatorPanel, settings.SimulatorPanelX);
            Canvas.SetTop(SimulatorPanel, settings.SimulatorPanelY);
        }
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (App.Services == null) return;

        var settingsService = App.Services.GetRequiredService<ISettingsService>();
        var settings = settingsService.Settings;

        // Save window state
        settings.WindowMaximized = WindowState == WindowState.Maximized;

        if (WindowState == WindowState.Normal)
        {
            settings.WindowWidth = Width;
            settings.WindowHeight = Height;
            settings.WindowX = Position.X;
            settings.WindowY = Position.Y;
        }

        // Save simulator panel position
        if (SimulatorPanel != null)
        {
            settings.SimulatorPanelX = Canvas.GetLeft(SimulatorPanel);
            settings.SimulatorPanelY = Canvas.GetTop(SimulatorPanel);
            settings.SimulatorPanelVisible = SimulatorPanel.IsVisible;
        }

        // Save UI state
        if (ViewModel != null)
        {
            settings.SimulatorEnabled = ViewModel.IsSimulatorEnabled;
            settings.GridVisible = ViewModel.IsGridOn;
        }

        // Settings will be saved automatically by App.Exit handler
    }

    private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F3 && MapControl != null)
        {
            MapControl.Toggle3DMode();
            e.Handled = true;
        }
        else if (e.Key == Key.PageUp && MapControl != null)
        {
            // Increase pitch (tilt camera up)
            MapControl.SetPitch(0.05);
            e.Handled = true;
        }
        else if (e.Key == Key.PageDown && MapControl != null)
        {
            // Decrease pitch (tilt camera down)
            MapControl.SetPitch(-0.05);
            e.Handled = true;
        }
    }

    private void MainWindow_PropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == nameof(Bounds))
        {
            // Constrain section control to new window bounds
            if (SectionControlPanel != null)
            {
                double currentLeft = Canvas.GetLeft(SectionControlPanel);
                double currentTop = Canvas.GetTop(SectionControlPanel);

                if (double.IsNaN(currentLeft)) currentLeft = 900; // Default initial position
                if (double.IsNaN(currentTop)) currentTop = 600;

                double maxLeft = Bounds.Width - SectionControlPanel.Bounds.Width;
                double maxTop = Bounds.Height - SectionControlPanel.Bounds.Height;

                double newLeft = Math.Clamp(currentLeft, 0, Math.Max(0, maxLeft));
                double newTop = Math.Clamp(currentTop, 0, Math.Max(0, maxTop));

                Canvas.SetLeft(SectionControlPanel, newLeft);
                Canvas.SetTop(SectionControlPanel, newTop);
            }

            // Constrain left panel to new window bounds
            if (LeftNavigationPanel != null)
            {
                double currentLeft = Canvas.GetLeft(LeftNavigationPanel);
                double currentTop = Canvas.GetTop(LeftNavigationPanel);

                if (double.IsNaN(currentLeft)) currentLeft = 20; // Default initial position
                if (double.IsNaN(currentTop)) currentTop = 100;

                double maxLeft = Bounds.Width - LeftNavigationPanel.Bounds.Width;
                double maxTop = Bounds.Height - LeftNavigationPanel.Bounds.Height;

                double newLeft = Math.Clamp(currentLeft, 0, Math.Max(0, maxLeft));
                double newTop = Math.Clamp(currentTop, 0, Math.Max(0, maxTop));

                Canvas.SetLeft(LeftNavigationPanel, newLeft);
                Canvas.SetTop(LeftNavigationPanel, newTop);
            }
        }
    }

    private async void BtnNtripConnect_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            await ViewModel.ConnectToNtripAsync();
        }
    }

    private async void BtnNtripDisconnect_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            await ViewModel.DisconnectFromNtripAsync();
        }
    }

    private void BtnDataIO_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new DataIODialog
        {
            DataContext = ViewModel
        };
        dialog.ShowDialog(this);
    }

    private async void BtnEnterSimCoords_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;

        // Check if simulator is enabled
        if (!ViewModel.IsSimulatorEnabled)
        {
            // Show message that simulator must be enabled first
            var messageBox = new Window
            {
                Title = "Simulator Not Enabled",
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30))
            };

            var stack = new StackPanel
            {
                Margin = new Thickness(24),
                Spacing = 16
            };

            stack.Children.Add(new TextBlock
            {
                Text = "Please enable the simulator first.",
                FontSize = 16,
                Foreground = Brushes.White,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            });

            var okButton = new Button
            {
                Content = "OK",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Padding = new Thickness(24, 8),
                Background = new SolidColorBrush(Color.FromRgb(0, 120, 212)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };
            okButton.Click += (s, args) => messageBox.Close();
            stack.Children.Add(okButton);

            messageBox.Content = stack;
            await messageBox.ShowDialog(this);
            return;
        }

        // Get current simulator position
        var currentPos = ViewModel.GetSimulatorPosition();

        // Open the dialog
        var dialog = new SimCoordsDialog(currentPos.Latitude, currentPos.Longitude);
        await dialog.ShowDialog(this);

        // If OK was clicked, update simulator coordinates
        if (dialog.DialogResult)
        {
            ViewModel.SetSimulatorCoordinates(dialog.Latitude, dialog.Longitude);
        }
    }

    private void Btn3DToggle_Click(object? sender, RoutedEventArgs e)
    {
        if (MapControl != null)
        {
            MapControl.Toggle3DMode();
        }
    }

    private async void BtnFields_Click(object? sender, RoutedEventArgs e)
    {
        if (App.Services == null) return;

        var fieldService = App.Services.GetRequiredService<IFieldService>();

        // Get or set default fields directory
        string fieldsDir = ViewModel?.FieldsRootDirectory ?? string.Empty;
        if (string.IsNullOrWhiteSpace(fieldsDir))
        {
            // Default to Documents/AgOpenGPS/Fields
            fieldsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "AgOpenGPS",
                "Fields");
        }

        var dialog = new FieldSelectionDialog(fieldService, fieldsDir);
        var result = await dialog.ShowDialog<bool>(this);

        if (result && dialog.SelectedField != null && ViewModel != null)
        {
            // Update ViewModel with selected field directory
            ViewModel.FieldsRootDirectory = fieldsDir;

            // Pass boundary to MapControl for rendering and center camera on it
            if (MapControl != null && dialog.SelectedField.Boundary != null)
            {
                MapControl.SetBoundary(dialog.SelectedField.Boundary);

                // Center camera on boundary
                var boundary = dialog.SelectedField.Boundary;
                if (boundary.OuterBoundary != null && boundary.OuterBoundary.Points.Count > 0)
                {
                    // Calculate boundary center
                    double sumE = 0, sumN = 0;
                    foreach (var point in boundary.OuterBoundary.Points)
                    {
                        sumE += point.Easting;
                        sumN += point.Northing;
                    }
                    double centerE = sumE / boundary.OuterBoundary.Points.Count;
                    double centerN = sumN / boundary.OuterBoundary.Points.Count;

                    // Pan camera to boundary center
                    MapControl.Pan(centerE, centerN);
                }
            }
        }
    }

    private async void BtnNewField_Click(object? sender, RoutedEventArgs e)
    {
        if (App.Services == null || ViewModel == null) return;

        var settingsService = App.Services.GetRequiredService<ISettingsService>();
        var fieldPlaneFileService = App.Services.GetRequiredService<FieldPlaneFileService>();

        // Get current GPS position or use default
        double latitude = ViewModel.Latitude;
        double longitude = ViewModel.Longitude;

        // If no GPS fix, use default coordinates (New York)
        if (latitude == 0 && longitude == 0)
        {
            latitude = 40.7128;
            longitude = -74.0060;
        }

        var currentPosition = new Position
        {
            Latitude = latitude,
            Longitude = longitude,
            Altitude = 0
        };

        // Pass current GPS position to the dialog
        var dialog = new NewFieldDialog(currentPosition);
        var result = await dialog.ShowDialog<(bool Success, string FieldName, Position Origin)>(this);

        if (result.Success && !string.IsNullOrWhiteSpace(result.FieldName))
        {
            try
            {
                // Create field directory
                var fieldDirectory = Path.Combine(settingsService.Settings.FieldsDirectory, result.FieldName);

                if (Directory.Exists(fieldDirectory))
                {
                    ViewModel.StatusMessage = $"Field '{result.FieldName}' already exists!";
                    return;
                }

                // Create directory and Field.txt
                fieldPlaneFileService.CreateField(fieldDirectory, result.Origin);

                // Update view model
                ViewModel.CurrentFieldName = result.FieldName;
                ViewModel.IsFieldOpen = true;

                // Save to settings
                settingsService.Settings.CurrentFieldName = result.FieldName;
                settingsService.Settings.LastOpenedField = result.FieldName;

                ViewModel.StatusMessage = $"Created field: {result.FieldName}";
            }
            catch (Exception ex)
            {
                ViewModel.StatusMessage = $"Error creating field: {ex.Message}";
            }
        }
    }

    private async void BtnOpenField_Click(object? sender, RoutedEventArgs e)
    {
        if (App.Services == null || ViewModel == null) return;

        var settingsService = App.Services.GetRequiredService<ISettingsService>();
        var fieldService = App.Services.GetRequiredService<IFieldService>();
        var fieldPlaneFileService = App.Services.GetRequiredService<FieldPlaneFileService>();
        var boundaryFileService = App.Services.GetRequiredService<BoundaryFileService>();

        // Open field selection dialog
        var dialog = new FieldSelectionDialog(fieldService, settingsService.Settings.FieldsDirectory);
        var dialogResult = await dialog.ShowDialog<bool>(this);

        if (dialogResult && dialog.SelectedField != null)
        {
            try
            {
                // Load field data
                var fieldDirectory = dialog.SelectedField.DirectoryPath;
                var fieldName = Path.GetFileName(fieldDirectory);

                // Load boundary if it exists
                var boundary = boundaryFileService.LoadBoundary(fieldDirectory);

                // Update view model
                ViewModel.CurrentFieldName = fieldName;
                ViewModel.IsFieldOpen = true;

                // Save to settings
                settingsService.Settings.CurrentFieldName = fieldName;
                settingsService.Settings.LastOpenedField = fieldName;

                // Render boundary on map
                if (MapControl != null && boundary != null)
                {
                    MapControl.SetBoundary(boundary);

                    // Center camera on boundary
                    if (boundary.OuterBoundary != null && boundary.OuterBoundary.Points.Count > 0)
                    {
                        double sumE = 0, sumN = 0;
                        foreach (var point in boundary.OuterBoundary.Points)
                        {
                            sumE += point.Easting;
                            sumN += point.Northing;
                        }
                        double centerE = sumE / boundary.OuterBoundary.Points.Count;
                        double centerN = sumN / boundary.OuterBoundary.Points.Count;

                        MapControl.Pan(centerE, centerN);
                    }
                }

                ViewModel.StatusMessage = $"Opened field: {fieldName}";
            }
            catch (Exception ex)
            {
                ViewModel.StatusMessage = $"Error opening field: {ex.Message}";
            }
        }
    }

    private void BtnCloseField_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;

        // Clear current field
        ViewModel.CurrentFieldName = string.Empty;
        ViewModel.IsFieldOpen = false;

        // Clear boundary from map
        if (MapControl != null)
        {
            MapControl.SetBoundary(null);
        }

        ViewModel.StatusMessage = "Field closed";
    }

    private async void BtnFromExisting_Click(object? sender, RoutedEventArgs e)
    {
        if (App.Services == null || ViewModel == null) return;

        var settingsService = App.Services.GetRequiredService<ISettingsService>();
        var fieldService = App.Services.GetRequiredService<IFieldService>();

        // Get vehicle name from settings (for appending to field name)
        // TODO: Add VehicleName to AppSettings when vehicle config is implemented
        var vehicleName = "Tractor";

        var dialog = new FromExistingFieldDialog(
            fieldService,
            settingsService.Settings.FieldsDirectory,
            vehicleName);

        var dialogResult = await dialog.ShowDialog<bool?>(this);

        if (dialogResult == true && dialog.Result != null)
        {
            try
            {
                // Create the new field by copying from existing
                var result = dialog.Result;
                var newFieldPath = Path.Combine(settingsService.Settings.FieldsDirectory, result.NewFieldName);

                // Create directory
                Directory.CreateDirectory(newFieldPath);

                // Copy boundary file (always copy)
                var sourceBoundaryFile = Path.Combine(result.SourceFieldPath, "Boundary.txt");
                var destBoundaryFile = Path.Combine(newFieldPath, "Boundary.txt");
                if (File.Exists(sourceBoundaryFile))
                {
                    File.Copy(sourceBoundaryFile, destBoundaryFile, true);
                }

                // Copy optional data based on selections
                if (result.CopyOptions.IncludeFlags)
                {
                    CopyFileIfExists(result.SourceFieldPath, newFieldPath, "Flags.txt");
                }

                if (result.CopyOptions.IncludeMapping)
                {
                    CopyFileIfExists(result.SourceFieldPath, newFieldPath, "Mapping.txt");
                    CopyFileIfExists(result.SourceFieldPath, newFieldPath, "Coverage.txt");
                }

                if (result.CopyOptions.IncludeHeadland)
                {
                    CopyFileIfExists(result.SourceFieldPath, newFieldPath, "Headland.txt");
                }

                if (result.CopyOptions.IncludeLines)
                {
                    CopyFileIfExists(result.SourceFieldPath, newFieldPath, "ABLines.txt");
                    CopyFileIfExists(result.SourceFieldPath, newFieldPath, "CurveLines.txt");
                }

                // Open the new field
                var boundaryFileService = App.Services.GetRequiredService<BoundaryFileService>();
                var boundary = boundaryFileService.LoadBoundary(newFieldPath);

                ViewModel.CurrentFieldName = result.NewFieldName;
                ViewModel.IsFieldOpen = true;

                // Save to settings
                settingsService.Settings.LastOpenedField = result.NewFieldName;
                settingsService.Settings.CurrentFieldName = result.NewFieldName;
                settingsService.Save();

                // Update map
                if (MapControl != null && boundary != null)
                {
                    MapControl.SetBoundary(boundary);

                    // Center on boundary
                    if (boundary.OuterBoundary?.Points != null && boundary.OuterBoundary.Points.Count > 0)
                    {
                        double sumE = 0, sumN = 0;
                        foreach (var pt in boundary.OuterBoundary.Points)
                        {
                            sumE += pt.Easting;
                            sumN += pt.Northing;
                        }
                        MapControl.Pan(sumE / boundary.OuterBoundary.Points.Count,
                                       sumN / boundary.OuterBoundary.Points.Count);
                    }
                }

                ViewModel.StatusMessage = $"Created new field: {result.NewFieldName}";
            }
            catch (Exception ex)
            {
                ViewModel.StatusMessage = $"Error creating field: {ex.Message}";
            }
        }
    }

    private void CopyFileIfExists(string sourceDir, string destDir, string fileName)
    {
        var sourceFile = Path.Combine(sourceDir, fileName);
        var destFile = Path.Combine(destDir, fileName);
        if (File.Exists(sourceFile))
        {
            File.Copy(sourceFile, destFile, true);
        }
    }

    private async void BtnIsoXml_Click(object? sender, RoutedEventArgs e)
    {
        if (App.Services == null || ViewModel == null) return;

        var settingsService = App.Services.GetRequiredService<ISettingsService>();
        var boundaryFileService = App.Services.GetRequiredService<BoundaryFileService>();

        // Open file picker for XML files
        var storageProvider = GetTopLevel(this)?.StorageProvider;
        if (storageProvider == null) return;

        var files = await storageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "Select ISO-XML File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new Avalonia.Platform.Storage.FilePickerFileType("XML Files") { Patterns = new[] { "*.xml", "*.XML" } }
            }
        });

        if (files.Count == 0) return;

        var xmlFilePath = files[0].Path.LocalPath;

        // Show the import dialog
        var dialog = new IsoXmlImportDialog(settingsService.Settings.FieldsDirectory);
        dialog.LoadXmlFile(xmlFilePath);

        var dialogResult = await dialog.ShowDialog<bool?>(this);

        if (dialogResult == true && dialog.Result != null)
        {
            try
            {
                var result = dialog.Result;

                // Create the field directory
                Directory.CreateDirectory(result.NewFieldPath);

                // Create Field.txt with origin coordinates
                var fieldFilePath = Path.Combine(result.NewFieldPath, "Field.txt");
                using (var writer = new StreamWriter(fieldFilePath))
                {
                    writer.WriteLine(DateTime.Now.ToString("yyyy-MMMM-dd hh:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture));
                    writer.WriteLine("$FieldDir");
                    writer.WriteLine("XML Derived");
                    writer.WriteLine("$Offsets");
                    writer.WriteLine("0,0");
                    writer.WriteLine("Convergence");
                    writer.WriteLine("0");
                    writer.WriteLine("StartFix");
                    writer.WriteLine($"{result.Origin.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{result.Origin.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                }

                // Convert Core boundaries to our Boundary model and save
                var boundary = new Models.Boundary();
                foreach (var coreBoundary in result.Boundaries)
                {
                    var polygon = new Models.BoundaryPolygon
                    {
                        IsDriveThrough = coreBoundary.IsDriveThru
                    };

                    foreach (var pt in coreBoundary.FenceLine)
                    {
                        polygon.Points.Add(new Models.BoundaryPoint(pt.Easting, pt.Northing, pt.Heading));
                    }

                    if (boundary.OuterBoundary == null)
                    {
                        boundary.OuterBoundary = polygon;
                    }
                    else
                    {
                        boundary.InnerBoundaries.Add(polygon);
                    }
                }

                // Save boundary
                if (boundary.OuterBoundary != null)
                {
                    boundaryFileService.SaveBoundary(boundary, result.NewFieldPath);
                }
                else
                {
                    boundaryFileService.CreateEmptyBoundary(result.NewFieldPath);
                }

                // Save headland if available
                if (result.Headland.Count > 0)
                {
                    var headlandFilePath = Path.Combine(result.NewFieldPath, "Headland.txt");
                    using (var writer = new StreamWriter(headlandFilePath))
                    {
                        writer.WriteLine("$Headland");
                        writer.WriteLine("False"); // isDriveThru
                        writer.WriteLine(result.Headland.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        foreach (var pt in result.Headland)
                        {
                            writer.WriteLine($"{pt.Easting:F3},{pt.Northing:F3},{pt.Heading:F5}");
                        }
                    }
                }

                // TODO: Save guidance lines when AB line support is implemented

                // Open the new field
                ViewModel.CurrentFieldName = result.NewFieldName;
                ViewModel.IsFieldOpen = true;

                // Save to settings
                settingsService.Settings.LastOpenedField = result.NewFieldName;
                settingsService.Settings.CurrentFieldName = result.NewFieldName;
                settingsService.Save();

                // Update map
                if (MapControl != null && boundary.OuterBoundary != null)
                {
                    MapControl.SetBoundary(boundary);

                    // Center on boundary
                    if (boundary.OuterBoundary.Points.Count > 0)
                    {
                        double sumE = 0, sumN = 0;
                        foreach (var pt in boundary.OuterBoundary.Points)
                        {
                            sumE += pt.Easting;
                            sumN += pt.Northing;
                        }
                        MapControl.Pan(sumE / boundary.OuterBoundary.Points.Count,
                                       sumN / boundary.OuterBoundary.Points.Count);
                    }
                }

                ViewModel.StatusMessage = $"Imported field from ISO-XML: {result.NewFieldName}";
            }
            catch (Exception ex)
            {
                ViewModel.StatusMessage = $"Error importing ISO-XML: {ex.Message}";
            }
        }
    }

    private async void BtnKml_Click(object? sender, RoutedEventArgs e)
    {
        if (App.Services == null || ViewModel == null) return;

        var settingsService = App.Services.GetRequiredService<ISettingsService>();
        var boundaryFileService = App.Services.GetRequiredService<BoundaryFileService>();

        // Open file picker for KML files
        var storageProvider = GetTopLevel(this)?.StorageProvider;
        if (storageProvider == null) return;

        var files = await storageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "Select KML File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new Avalonia.Platform.Storage.FilePickerFileType("KML Files") { Patterns = new[] { "*.kml", "*.KML" } }
            }
        });

        if (files.Count == 0) return;

        var kmlFilePath = files[0].Path.LocalPath;

        // Show the import dialog
        var dialog = new KmlImportDialog(settingsService.Settings.FieldsDirectory);
        dialog.LoadKmlFile(kmlFilePath);

        var dialogResult = await dialog.ShowDialog<bool?>(this);

        if (dialogResult == true && dialog.Result != null)
        {
            try
            {
                var result = dialog.Result;

                // Create the field directory
                Directory.CreateDirectory(result.NewFieldPath);

                // Create Field.txt with origin coordinates
                var fieldFilePath = Path.Combine(result.NewFieldPath, "Field.txt");
                using (var writer = new StreamWriter(fieldFilePath))
                {
                    writer.WriteLine(DateTime.Now.ToString("yyyy-MMMM-dd hh:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture));
                    writer.WriteLine("$FieldDir");
                    writer.WriteLine("KML Derived");
                    writer.WriteLine("$Offsets");
                    writer.WriteLine("0,0");
                    writer.WriteLine("Convergence");
                    writer.WriteLine("0");
                    writer.WriteLine("StartFix");
                    writer.WriteLine($"{result.CenterLatitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{result.CenterLongitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                }

                // Convert WGS84 boundary points to local coordinates
                var origin = new AgOpenGPS.Core.Models.Wgs84(result.CenterLatitude, result.CenterLongitude);
                var sharedProps = new AgOpenGPS.Core.Models.SharedFieldProperties();
                var localPlane = new AgOpenGPS.Core.Models.LocalPlane(origin, sharedProps);

                var boundary = new Models.Boundary();
                var outerPolygon = new Models.BoundaryPolygon();

                foreach (var (lat, lon) in result.BoundaryPoints)
                {
                    var wgs84 = new AgOpenGPS.Core.Models.Wgs84(lat, lon);
                    var geoCoord = localPlane.ConvertWgs84ToGeoCoord(wgs84);
                    outerPolygon.Points.Add(new Models.BoundaryPoint(geoCoord.Easting, geoCoord.Northing, 0));
                }

                boundary.OuterBoundary = outerPolygon;

                // Save boundary
                boundaryFileService.SaveBoundary(boundary, result.NewFieldPath);

                // Open the new field
                ViewModel.CurrentFieldName = result.NewFieldName;
                ViewModel.IsFieldOpen = true;

                // Save to settings
                settingsService.Settings.LastOpenedField = result.NewFieldName;
                settingsService.Settings.CurrentFieldName = result.NewFieldName;
                settingsService.Save();

                // Update map
                if (MapControl != null && boundary.OuterBoundary != null)
                {
                    MapControl.SetBoundary(boundary);

                    // Center on boundary
                    if (boundary.OuterBoundary.Points.Count > 0)
                    {
                        double sumE = 0, sumN = 0;
                        foreach (var pt in boundary.OuterBoundary.Points)
                        {
                            sumE += pt.Easting;
                            sumN += pt.Northing;
                        }
                        MapControl.Pan(sumE / boundary.OuterBoundary.Points.Count,
                                       sumN / boundary.OuterBoundary.Points.Count);
                    }
                }

                ViewModel.StatusMessage = $"Imported field from KML: {result.NewFieldName}";
            }
            catch (Exception ex)
            {
                ViewModel.StatusMessage = $"Error importing KML: {ex.Message}";
            }
        }
    }

    private void BtnDriveIn_Click(object? sender, RoutedEventArgs e)
    {
        if (App.Services == null || ViewModel == null) return;

        var settingsService = App.Services.GetRequiredService<ISettingsService>();
        var boundaryFileService = App.Services.GetRequiredService<BoundaryFileService>();

        // Get current GPS position
        double currentLat = ViewModel.Latitude;
        double currentLon = ViewModel.Longitude;

        // Check if we have a valid GPS position
        if (currentLat == 0 && currentLon == 0)
        {
            ViewModel.StatusMessage = "No GPS position available - cannot find nearby field";
            return;
        }

        var fieldsDir = settingsService.Settings.FieldsDirectory;
        if (!Directory.Exists(fieldsDir))
        {
            ViewModel.StatusMessage = "Fields directory not found";
            return;
        }

        // Find nearest field
        string? nearestFieldName = null;
        double nearestDistance = double.MaxValue;

        foreach (var fieldDir in Directory.GetDirectories(fieldsDir))
        {
            var fieldFilePath = Path.Combine(fieldDir, "Field.txt");
            if (!File.Exists(fieldFilePath)) continue;

            // Read Field.txt and find StartFix line
            try
            {
                var lines = File.ReadAllLines(fieldFilePath);
                for (int i = 0; i < lines.Length - 1; i++)
                {
                    if (lines[i].Trim() == "StartFix")
                    {
                        var coords = lines[i + 1].Split(',');
                        if (coords.Length >= 2 &&
                            double.TryParse(coords[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double fieldLat) &&
                            double.TryParse(coords[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double fieldLon))
                        {
                            // Calculate distance using simple Euclidean approximation (good enough for nearby fields)
                            double dLat = (currentLat - fieldLat) * 111132.92; // meters per degree lat
                            double dLon = (currentLon - fieldLon) * 111412.84 * Math.Cos(currentLat * Math.PI / 180); // meters per degree lon
                            double distance = Math.Sqrt(dLat * dLat + dLon * dLon);

                            if (distance < nearestDistance)
                            {
                                nearestDistance = distance;
                                nearestFieldName = Path.GetFileName(fieldDir);
                            }
                        }
                        break;
                    }
                }
            }
            catch
            {
                // Skip fields with invalid Field.txt
            }
        }

        if (nearestFieldName == null)
        {
            ViewModel.StatusMessage = "No fields found with valid coordinates";
            return;
        }

        // Check if field is within reasonable distance (5km)
        if (nearestDistance > 5000)
        {
            ViewModel.StatusMessage = $"Nearest field '{nearestFieldName}' is {nearestDistance / 1000:F1} km away - too far";
            return;
        }

        // Open the nearest field
        var fieldPath = Path.Combine(fieldsDir, nearestFieldName);
        var boundary = boundaryFileService.LoadBoundary(fieldPath);

        ViewModel.CurrentFieldName = nearestFieldName;
        ViewModel.IsFieldOpen = true;

        // Save to settings
        settingsService.Settings.LastOpenedField = nearestFieldName;
        settingsService.Settings.CurrentFieldName = nearestFieldName;
        settingsService.Save();

        // Update map
        if (MapControl != null)
        {
            if (boundary != null)
            {
                MapControl.SetBoundary(boundary);

                // Center on boundary
                if (boundary.OuterBoundary?.Points != null && boundary.OuterBoundary.Points.Count > 0)
                {
                    double sumE = 0, sumN = 0;
                    foreach (var pt in boundary.OuterBoundary.Points)
                    {
                        sumE += pt.Easting;
                        sumN += pt.Northing;
                    }
                    MapControl.Pan(sumE / boundary.OuterBoundary.Points.Count,
                                   sumN / boundary.OuterBoundary.Points.Count);
                }
            }
            else
            {
                MapControl.SetBoundary(null);
            }
        }

        ViewModel.StatusMessage = $"Opened nearest field: {nearestFieldName} ({nearestDistance:F0}m away)";
    }

    private void BtnResumeField_Click(object? sender, RoutedEventArgs e)
    {
        if (App.Services == null || ViewModel == null) return;

        var settingsService = App.Services.GetRequiredService<ISettingsService>();
        var boundaryFileService = App.Services.GetRequiredService<BoundaryFileService>();

        // Get the last opened field name from settings
        var lastFieldName = settingsService.Settings.LastOpenedField;

        if (string.IsNullOrWhiteSpace(lastFieldName))
        {
            ViewModel.StatusMessage = "No previous field to resume";
            return;
        }

        // Build field directory path
        var fieldDirectory = Path.Combine(settingsService.Settings.FieldsDirectory, lastFieldName);

        if (!Directory.Exists(fieldDirectory))
        {
            ViewModel.StatusMessage = $"Field '{lastFieldName}' not found";
            return;
        }

        try
        {
            // Load boundary if it exists
            var boundary = boundaryFileService.LoadBoundary(fieldDirectory);

            // Update view model
            ViewModel.CurrentFieldName = lastFieldName;
            ViewModel.IsFieldOpen = true;

            // Save to settings
            settingsService.Settings.CurrentFieldName = lastFieldName;

            // Render boundary on map
            if (MapControl != null && boundary != null)
            {
                MapControl.SetBoundary(boundary);

                // Center camera on boundary
                if (boundary.OuterBoundary != null && boundary.OuterBoundary.Points.Count > 0)
                {
                    double sumE = 0, sumN = 0;
                    foreach (var point in boundary.OuterBoundary.Points)
                    {
                        sumE += point.Easting;
                        sumN += point.Northing;
                    }
                    double centerE = sumE / boundary.OuterBoundary.Points.Count;
                    double centerN = sumN / boundary.OuterBoundary.Points.Count;

                    MapControl.Pan(centerE, centerN);
                }
            }

            ViewModel.StatusMessage = $"Resumed field: {lastFieldName}";
        }
        catch (Exception ex)
        {
            ViewModel.StatusMessage = $"Error resuming field: {ex.Message}";
        }
    }

    private async void BtnTestOSK_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;

        // Test alphanumeric keyboard (for field names, etc.)
        var textResult = await AlphanumericKeyboard.ShowAsync(
            this,
            description: "Enter field name:",
            initialValue: "My Field",
            maxLength: 50);

        if (textResult != null)
        {
            ViewModel.StatusMessage = $"Text entered: {textResult}";
        }
        else
        {
            ViewModel.StatusMessage = "Keyboard cancelled";
        }
    }

    private async void BtnTestNumericOSK_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;

        // Test numeric keyboard (for values)
        var numResult = await OnScreenKeyboard.ShowAsync(
            this,
            description: "Enter vehicle width (meters):",
            initialValue: 6.5,
            minValue: 0.5,
            maxValue: 50.0,
            maxDecimalPlaces: 2,
            allowNegative: false);

        if (numResult.HasValue)
        {
            ViewModel.StatusMessage = $"Value entered: {numResult.Value:F2} meters";
        }
        else
        {
            ViewModel.StatusMessage = "Keyboard cancelled";
        }
    }

    // Drag functionality for Section Control
    private void SectionControl_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border)
        {
            _isDraggingSection = true;
            _dragStartPoint = e.GetPosition(this);
            e.Pointer.Capture(border);
        }
    }

    private void SectionControl_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDraggingSection && sender is Border border)
        {
            var currentPoint = e.GetPosition(this);
            var delta = currentPoint - _dragStartPoint;

            // Calculate new position
            double newLeft = Canvas.GetLeft(border) + delta.X;
            double newTop = Canvas.GetTop(border) + delta.Y;

            // Constrain to window bounds
            double maxLeft = Bounds.Width - border.Bounds.Width;
            double maxTop = Bounds.Height - border.Bounds.Height;

            newLeft = Math.Clamp(newLeft, 0, maxLeft);
            newTop = Math.Clamp(newTop, 0, maxTop);

            // Update position
            Canvas.SetLeft(border, newLeft);
            Canvas.SetTop(border, newTop);

            _dragStartPoint = currentPoint;
        }
    }

    private void SectionControl_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDraggingSection)
        {
            _isDraggingSection = false;
            if (sender is Border border)
            {
                e.Pointer.Capture(null);
            }
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.Easting) ||
            e.PropertyName == nameof(MainViewModel.Northing) ||
            e.PropertyName == nameof(MainViewModel.Heading))
        {
            if (ViewModel != null && MapControl != null)
            {
                // Convert heading from degrees to radians
                double headingRadians = ViewModel.Heading * Math.PI / 180.0;
                MapControl.SetVehiclePosition(ViewModel.Easting, ViewModel.Northing, headingRadians);

                // Add boundary point if recording
                if (BoundaryRecordingService?.IsRecording == true)
                {
                    // Apply boundary offset
                    var (offsetEasting, offsetNorthing) = CalculateOffsetPosition(
                        ViewModel.Easting, ViewModel.Northing, headingRadians);
                    BoundaryRecordingService.AddPoint(offsetEasting, offsetNorthing, headingRadians);
                    UpdateBoundaryStatusDisplay();
                }
            }
        }
        else if (e.PropertyName == nameof(MainViewModel.IsGridOn))
        {
            if (ViewModel != null && MapControl != null)
            {
                MapControl.SetGridVisible(ViewModel.IsGridOn);
            }
        }
        else if (e.PropertyName == nameof(MainViewModel.CameraPitch))
        {
            if (ViewModel != null && MapControl != null)
            {
                // Camera pitch from service is negative degrees (-90 to -10)
                // OpenGL expects positive radians (0 = overhead, PI/2 = horizontal)
                // So we negate the degrees and convert: -90° -> 0 rad, -10° -> ~1.4 rad
                double pitchRadians = -ViewModel.CameraPitch * Math.PI / 180.0;
                MapControl.SetPitchAbsolute(pitchRadians);
            }
        }
        else if (e.PropertyName == nameof(MainViewModel.Is2DMode))
        {
            if (ViewModel != null && MapControl != null)
            {
                // Is2DMode = true means 3D is off, so invert the value
                MapControl.Set3DMode(!ViewModel.Is2DMode);
            }
        }
        else if (e.PropertyName == nameof(MainViewModel.IsDayMode))
        {
            // Day/Night mode visual implementation not yet added to OpenGLMapControl
            // TODO: Implement theme switching (background color, grid color, etc.)
        }
        else if (e.PropertyName == nameof(MainViewModel.IsNorthUp))
        {
            // North-up rotation mode not yet implemented in OpenGLMapControl
            // TODO: Implement camera rotation locking to north
        }
        else if (e.PropertyName == nameof(MainViewModel.Brightness))
        {
            // Brightness control depends on platform-specific implementation
            // Currently marked as not supported in DisplaySettingsService
        }
    }

    // Map overlay event handlers that forward to MapControl
    private void MapOverlay_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Check if pointer is over any UI panel - if so, don't handle the event
        if (IsPointerOverUIPanel(e))
        {
            return; // Let the UI panel handle it
        }

        if (MapControl != null)
        {
            // Forward event to MapControl's internal handler
            var point = e.GetCurrentPoint(this);

            if (point.Properties.IsLeftButtonPressed)
            {
                MapControl.StartPan(point.Position);
                e.Handled = true;
            }
            else if (point.Properties.IsRightButtonPressed)
            {
                MapControl.StartRotate(point.Position);
                e.Handled = true;
            }
        }
    }

    private void MapOverlay_PointerMoved(object? sender, PointerEventArgs e)
    {
        // Check if pointer is over any UI panel - if so, don't handle the event
        if (IsPointerOverUIPanel(e))
        {
            return; // Let the UI panel handle it
        }

        if (MapControl != null)
        {
            var point = e.GetCurrentPoint(this);
            MapControl.UpdateMouse(point.Position);
            e.Handled = true;
        }
    }

    private void MapOverlay_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        // Check if pointer is over any UI panel - if so, don't handle the event
        if (IsPointerOverUIPanel(e))
        {
            return; // Let the UI panel handle it
        }

        if (MapControl != null)
        {
            MapControl.EndPanRotate();
            e.Handled = true;
        }
    }

    private void MapOverlay_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // Check if pointer is over any UI panel - if so, don't handle the event
        if (IsPointerOverUIPanel(e))
        {
            return; // Let the UI panel handle it
        }

        if (MapControl != null)
        {
            double zoomFactor = e.Delta.Y > 0 ? 1.1 : 0.9;
            MapControl.Zoom(zoomFactor);
            e.Handled = true;
        }
    }

    // Combined tap-to-rotate and hold-to-drag for Left Navigation Panel
    private void LeftPanel_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Sender is the touch handle Grid
        if (LeftNavigationPanel != null && sender is Grid touchHandle)
        {
            _leftPanelPressTime = DateTime.Now;
            _dragStartPoint = e.GetPosition(this);
            e.Pointer.Capture(touchHandle);

            // Close any open tooltips to prevent position issues during drag
            ToolTip.SetIsOpen(touchHandle, false);

            e.Handled = true; // Prevent map from handling this event
        }
    }

    private void LeftPanel_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (LeftNavigationPanel != null && e.Pointer.Captured == sender && sender is Grid touchHandle)
        {
            var currentPoint = e.GetPosition(this);
            var distance = Math.Sqrt(Math.Pow(currentPoint.X - _dragStartPoint.X, 2) +
                                    Math.Pow(currentPoint.Y - _dragStartPoint.Y, 2));

            // Start dragging if moved beyond threshold
            if (!_isDraggingLeftPanel && distance > TapDistanceThreshold)
            {
                _isDraggingLeftPanel = true;
                // Ensure tooltip stays closed while dragging
                ToolTip.SetIsOpen(touchHandle, false);
            }

            if (_isDraggingLeftPanel)
            {
                var delta = currentPoint - _dragStartPoint;

                // Calculate new position
                double newLeft = Canvas.GetLeft(LeftNavigationPanel) + delta.X;
                double newTop = Canvas.GetTop(LeftNavigationPanel) + delta.Y;

                // Constrain to window bounds
                double maxLeft = Bounds.Width - LeftNavigationPanel.Bounds.Width;
                double maxTop = Bounds.Height - LeftNavigationPanel.Bounds.Height;

                newLeft = Math.Clamp(newLeft, 0, Math.Max(0, maxLeft));
                newTop = Math.Clamp(newTop, 0, Math.Max(0, maxTop));

                // Update position
                Canvas.SetLeft(LeftNavigationPanel, newLeft);
                Canvas.SetTop(LeftNavigationPanel, newTop);

                _dragStartPoint = currentPoint;
            }

            e.Handled = true;
        }
    }

    private void LeftPanel_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (LeftNavigationPanel != null && e.Pointer.Captured == sender)
        {
            var currentPoint = e.GetPosition(this);
            var distance = Math.Sqrt(Math.Pow(currentPoint.X - _dragStartPoint.X, 2) +
                                    Math.Pow(currentPoint.Y - _dragStartPoint.Y, 2));
            var elapsed = (DateTime.Now - _leftPanelPressTime).TotalMilliseconds;

            // Detect tap: quick release with minimal movement
            bool isTap = !_isDraggingLeftPanel &&
                        elapsed < TapTimeThresholdMs &&
                        distance < TapDistanceThreshold;

            if (isTap)
            {
                // Tap detected - rotate the panel
                if (LeftPanelStack != null)
                {
                    if (LeftPanelStack.Orientation == Avalonia.Layout.Orientation.Vertical)
                    {
                        LeftPanelStack.Orientation = Avalonia.Layout.Orientation.Horizontal;
                    }
                    else
                    {
                        LeftPanelStack.Orientation = Avalonia.Layout.Orientation.Vertical;
                    }
                }
            }

            // Reset state
            _isDraggingLeftPanel = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }


    // Event blockers for Left Panel Border - prevent events from reaching map overlay
    // But DON'T block if the event comes from interactive children (buttons, drag handle, etc.)
    private void LeftPanelBorder_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Only block if event originated from the Border itself (not a child control)
        if (e.Source == sender)
        {
            e.Handled = true;
        }
    }

    private void LeftPanelBorder_PointerMoved(object? sender, PointerEventArgs e)
    {
        // Don't block - let children handle their own moves
    }

    private void LeftPanelBorder_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        // Only block if event originated from the Border itself (not a child control)
        if (e.Source == sender)
        {
            e.Handled = true;
        }
    }

    // File Menu Panel drag handlers
    private void FileMenuPanel_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Sender is the header Grid
        if (FileMenuPanel != null && sender is Grid header)
        {
            _dragStartPoint = e.GetPosition(this);
            e.Pointer.Capture(header);

            // Close any open tooltips to prevent position issues during drag
            ToolTip.SetIsOpen(header, false);

            e.Handled = true;
        }
    }

    private void FileMenuPanel_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (FileMenuPanel != null && e.Pointer.Captured == sender && sender is Grid header)
        {
            var currentPoint = e.GetPosition(this);
            var distance = Math.Sqrt(Math.Pow(currentPoint.X - _dragStartPoint.X, 2) +
                                    Math.Pow(currentPoint.Y - _dragStartPoint.Y, 2));

            // Start dragging if moved beyond threshold
            if (!_isDraggingFileMenu && distance > TapDistanceThreshold)
            {
                _isDraggingFileMenu = true;
                // Ensure tooltip stays closed while dragging
                ToolTip.SetIsOpen(header, false);
            }

            if (_isDraggingFileMenu)
            {
                var delta = currentPoint - _dragStartPoint;

                // Calculate new position
                double newLeft = Canvas.GetLeft(FileMenuPanel) + delta.X;
                double newTop = Canvas.GetTop(FileMenuPanel) + delta.Y;

                // Constrain to window bounds
                double maxLeft = Bounds.Width - FileMenuPanel.Bounds.Width;
                double maxTop = Bounds.Height - FileMenuPanel.Bounds.Height;

                newLeft = Math.Clamp(newLeft, 0, Math.Max(0, maxLeft));
                newTop = Math.Clamp(newTop, 0, Math.Max(0, maxTop));

                // Update position
                Canvas.SetLeft(FileMenuPanel, newLeft);
                Canvas.SetTop(FileMenuPanel, newTop);

                _dragStartPoint = currentPoint;
            }

            e.Handled = true;
        }
    }

    private void FileMenuPanel_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (FileMenuPanel != null && e.Pointer.Captured == sender)
        {
            // Reset state
            _isDraggingFileMenu = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    // View Settings Panel drag handlers
    private void ViewSettingsPanel_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Sender is the header Grid
        if (ViewSettingsPanel != null && sender is Grid header)
        {
            _dragStartPoint = e.GetPosition(this);
            e.Pointer.Capture(header);

            // Close any open tooltips to prevent position issues during drag
            ToolTip.SetIsOpen(header, false);

            e.Handled = true;
        }
    }

    private void ViewSettingsPanel_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (ViewSettingsPanel != null && e.Pointer.Captured == sender && sender is Grid header)
        {
            var currentPoint = e.GetPosition(this);
            var distance = Math.Sqrt(Math.Pow(currentPoint.X - _dragStartPoint.X, 2) +
                                    Math.Pow(currentPoint.Y - _dragStartPoint.Y, 2));

            // Start dragging if moved beyond threshold
            if (!_isDraggingViewSettings && distance > TapDistanceThreshold)
            {
                _isDraggingViewSettings = true;
                // Ensure tooltip stays closed while dragging
                ToolTip.SetIsOpen(header, false);
            }

            if (_isDraggingViewSettings)
            {
                var delta = currentPoint - _dragStartPoint;

                // Calculate new position
                double newLeft = Canvas.GetLeft(ViewSettingsPanel) + delta.X;
                double newTop = Canvas.GetTop(ViewSettingsPanel) + delta.Y;

                // Constrain to window bounds
                double maxLeft = Bounds.Width - ViewSettingsPanel.Bounds.Width;
                double maxTop = Bounds.Height - ViewSettingsPanel.Bounds.Height;

                newLeft = Math.Clamp(newLeft, 0, Math.Max(0, maxLeft));
                newTop = Math.Clamp(newTop, 0, Math.Max(0, maxTop));

                // Update position
                Canvas.SetLeft(ViewSettingsPanel, newLeft);
                Canvas.SetTop(ViewSettingsPanel, newTop);

                _dragStartPoint = currentPoint;
            }

            e.Handled = true;
        }
    }

    private void ViewSettingsPanel_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (ViewSettingsPanel != null && e.Pointer.Captured == sender)
        {
            // Reset state
            _isDraggingViewSettings = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    // Tools Panel drag handlers
    private void ToolsPanel_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Sender is the header Grid
        if (ToolsPanel != null && sender is Grid header)
        {
            _dragStartPoint = e.GetPosition(this);
            e.Pointer.Capture(header);

            // Close any open tooltips to prevent position issues during drag
            ToolTip.SetIsOpen(header, false);

            e.Handled = true;
        }
    }

    private void ToolsPanel_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (ToolsPanel != null && e.Pointer.Captured == sender && sender is Grid header)
        {
            var currentPoint = e.GetPosition(this);
            var distance = Math.Sqrt(Math.Pow(currentPoint.X - _dragStartPoint.X, 2) +
                                    Math.Pow(currentPoint.Y - _dragStartPoint.Y, 2));

            // Start dragging if moved beyond threshold
            if (!_isDraggingTools && distance > TapDistanceThreshold)
            {
                _isDraggingTools = true;
                // Ensure tooltip stays closed while dragging
                ToolTip.SetIsOpen(header, false);
            }

            if (_isDraggingTools)
            {
                var delta = currentPoint - _dragStartPoint;

                // Calculate new position
                double newLeft = Canvas.GetLeft(ToolsPanel) + delta.X;
                double newTop = Canvas.GetTop(ToolsPanel) + delta.Y;

                // Constrain to window bounds
                double maxLeft = Bounds.Width - ToolsPanel.Bounds.Width;
                double maxTop = Bounds.Height - ToolsPanel.Bounds.Height;

                newLeft = Math.Clamp(newLeft, 0, Math.Max(0, maxLeft));
                newTop = Math.Clamp(newTop, 0, Math.Max(0, maxTop));

                // Update position
                Canvas.SetLeft(ToolsPanel, newLeft);
                Canvas.SetTop(ToolsPanel, newTop);

                _dragStartPoint = currentPoint;
            }

            e.Handled = true;
        }
    }

    private void ToolsPanel_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (ToolsPanel != null && e.Pointer.Captured == sender)
        {
            // Reset state
            _isDraggingTools = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    // Configuration Panel drag handlers
    private void ConfigurationPanel_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Sender is the header Grid
        if (ConfigurationPanel != null && sender is Grid header)
        {
            _dragStartPoint = e.GetPosition(this);
            e.Pointer.Capture(header);

            // Close any open tooltips to prevent position issues during drag
            ToolTip.SetIsOpen(header, false);

            e.Handled = true;
        }
    }

    private void ConfigurationPanel_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (ConfigurationPanel != null && e.Pointer.Captured == sender && sender is Grid header)
        {
            var currentPoint = e.GetPosition(this);
            var distance = Math.Sqrt(Math.Pow(currentPoint.X - _dragStartPoint.X, 2) +
                                    Math.Pow(currentPoint.Y - _dragStartPoint.Y, 2));

            // Start dragging if moved beyond threshold
            if (!_isDraggingConfiguration && distance > TapDistanceThreshold)
            {
                _isDraggingConfiguration = true;
                // Ensure tooltip stays closed while dragging
                ToolTip.SetIsOpen(header, false);
            }

            if (_isDraggingConfiguration)
            {
                var delta = currentPoint - _dragStartPoint;

                // Calculate new position
                double newLeft = Canvas.GetLeft(ConfigurationPanel) + delta.X;
                double newTop = Canvas.GetTop(ConfigurationPanel) + delta.Y;

                // Constrain to window bounds
                double maxLeft = Bounds.Width - ConfigurationPanel.Bounds.Width;
                double maxTop = Bounds.Height - ConfigurationPanel.Bounds.Height;

                newLeft = Math.Clamp(newLeft, 0, Math.Max(0, maxLeft));
                newTop = Math.Clamp(newTop, 0, Math.Max(0, maxTop));

                // Update position
                Canvas.SetLeft(ConfigurationPanel, newLeft);
                Canvas.SetTop(ConfigurationPanel, newTop);

                _dragStartPoint = currentPoint;
            }

            e.Handled = true;
        }
    }

    private void ConfigurationPanel_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (ConfigurationPanel != null && e.Pointer.Captured == sender)
        {
            // Reset state
            _isDraggingConfiguration = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    // Job Menu Panel drag handlers
    private void JobMenuPanel_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (JobMenuPanel != null && sender is Grid header)
        {
            _dragStartPoint = e.GetPosition(this);
            e.Pointer.Capture(header);
            // Suppress tooltip to prevent it from following during drag
            ToolTip.SetIsOpen(header, false);
            e.Handled = true;
        }
    }

    private void JobMenuPanel_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (JobMenuPanel != null && e.Pointer.Captured == sender && sender is Grid header)
        {
            var currentPoint = e.GetPosition(this);
            var distance = Math.Sqrt(Math.Pow(currentPoint.X - _dragStartPoint.X, 2) +
                                    Math.Pow(currentPoint.Y - _dragStartPoint.Y, 2));

            // Only start dragging if moved beyond threshold
            if (!_isDraggingJobMenu && distance > TapDistanceThreshold)
            {
                _isDraggingJobMenu = true;
                // Suppress tooltip when dragging starts
                ToolTip.SetIsOpen(header, false);
            }

            if (_isDraggingJobMenu)
            {
                var delta = currentPoint - _dragStartPoint;

                double newLeft = Canvas.GetLeft(JobMenuPanel) + delta.X;
                double newTop = Canvas.GetTop(JobMenuPanel) + delta.Y;

                // Constrain to window bounds
                double maxLeft = Bounds.Width - JobMenuPanel.Bounds.Width;
                double maxTop = Bounds.Height - JobMenuPanel.Bounds.Height;

                newLeft = Math.Clamp(newLeft, 0, Math.Max(0, maxLeft));
                newTop = Math.Clamp(newTop, 0, Math.Max(0, maxTop));

                Canvas.SetLeft(JobMenuPanel, newLeft);
                Canvas.SetTop(JobMenuPanel, newTop);

                _dragStartPoint = currentPoint;
            }

            e.Handled = true;
        }
    }

    private void JobMenuPanel_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (JobMenuPanel != null && e.Pointer.Captured == sender)
        {
            // Reset state
            _isDraggingJobMenu = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    // Field Tools Panel drag handlers
    private void FieldToolsPanel_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (FieldToolsPanel != null && sender is Grid header)
        {
            _dragStartPoint = e.GetPosition(this);
            e.Pointer.Capture(header);
            // Suppress tooltip to prevent it from following during drag
            ToolTip.SetIsOpen(header, false);
            e.Handled = true;
        }
    }

    private void FieldToolsPanel_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (FieldToolsPanel != null && e.Pointer.Captured == sender && sender is Grid header)
        {
            var currentPoint = e.GetPosition(this);
            var distance = Math.Sqrt(Math.Pow(currentPoint.X - _dragStartPoint.X, 2) +
                                    Math.Pow(currentPoint.Y - _dragStartPoint.Y, 2));

            // Only start dragging if moved beyond threshold
            if (!_isDraggingFieldTools && distance > TapDistanceThreshold)
            {
                _isDraggingFieldTools = true;
                // Suppress tooltip when dragging starts
                ToolTip.SetIsOpen(header, false);
            }

            if (_isDraggingFieldTools)
            {
                var delta = currentPoint - _dragStartPoint;

                double newLeft = Canvas.GetLeft(FieldToolsPanel) + delta.X;
                double newTop = Canvas.GetTop(FieldToolsPanel) + delta.Y;

                // Constrain to window bounds
                double maxLeft = Bounds.Width - FieldToolsPanel.Bounds.Width;
                double maxTop = Bounds.Height - FieldToolsPanel.Bounds.Height;

                newLeft = Math.Clamp(newLeft, 0, Math.Max(0, maxLeft));
                newTop = Math.Clamp(newTop, 0, Math.Max(0, maxTop));

                Canvas.SetLeft(FieldToolsPanel, newLeft);
                Canvas.SetTop(FieldToolsPanel, newTop);

                _dragStartPoint = currentPoint;
            }

            e.Handled = true;
        }
    }

    private void FieldToolsPanel_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (FieldToolsPanel != null && e.Pointer.Captured == sender)
        {
            // Reset state
            _isDraggingFieldTools = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    // Simulator Panel drag handlers
    private void SimulatorPanel_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (SimulatorPanel != null && sender is Grid header)
        {
            _dragStartPoint = e.GetPosition(this);
            e.Pointer.Capture(header);
            // Suppress tooltip to prevent it from following during drag
            ToolTip.SetIsOpen(header, false);
            e.Handled = true;
        }
    }

    private void SimulatorPanel_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (SimulatorPanel != null && e.Pointer.Captured == sender && sender is Grid header)
        {
            var currentPoint = e.GetPosition(this);
            var distance = Math.Sqrt(Math.Pow(currentPoint.X - _dragStartPoint.X, 2) +
                                    Math.Pow(currentPoint.Y - _dragStartPoint.Y, 2));

            // Only start dragging if moved beyond threshold
            if (!_isDraggingSimulator && distance > TapDistanceThreshold)
            {
                _isDraggingSimulator = true;
                // Suppress tooltip when dragging starts
                ToolTip.SetIsOpen(header, false);
            }

            if (_isDraggingSimulator)
            {
                var delta = currentPoint - _dragStartPoint;

                double newLeft = Canvas.GetLeft(SimulatorPanel) + delta.X;
                double newTop = Canvas.GetTop(SimulatorPanel) + delta.Y;

                // Constrain to window bounds
                double maxLeft = Bounds.Width - SimulatorPanel.Bounds.Width;
                double maxTop = Bounds.Height - SimulatorPanel.Bounds.Height;

                newLeft = Math.Clamp(newLeft, 0, Math.Max(0, maxLeft));
                newTop = Math.Clamp(newTop, 0, Math.Max(0, maxTop));

                Canvas.SetLeft(SimulatorPanel, newLeft);
                Canvas.SetTop(SimulatorPanel, newTop);

                _dragStartPoint = currentPoint;
                e.Handled = true;
            }
        }
    }

    private void SimulatorPanel_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (SimulatorPanel != null)
        {
            // Always release capture and reset state, regardless of whether we were dragging
            _isDraggingSimulator = false;
            if (e.Pointer.Captured == sender)
            {
                e.Pointer.Capture(null);
            }
            e.Handled = true;
        }
    }

    // Helper method to check if pointer is over any UI panel
    private bool IsPointerOverUIPanel(PointerEventArgs e)
    {
        var position = e.GetPosition(this);

        // Check left navigation panel
        if (LeftNavigationPanel != null && LeftNavigationPanel.IsVisible && LeftNavigationPanel.Bounds.Width > 0 && LeftNavigationPanel.Bounds.Height > 0)
        {
            double left = Canvas.GetLeft(LeftNavigationPanel);
            double top = Canvas.GetTop(LeftNavigationPanel);

            if (double.IsNaN(left)) left = 20;
            if (double.IsNaN(top)) top = 100;

            var panelBounds = new Rect(left, top, LeftNavigationPanel.Bounds.Width, LeftNavigationPanel.Bounds.Height);

            if (panelBounds.Contains(position))
            {
                return true;
            }
        }

        // Check section control panel
        if (SectionControlPanel != null && SectionControlPanel.IsVisible && SectionControlPanel.Bounds.Width > 0 && SectionControlPanel.Bounds.Height > 0)
        {
            double left = Canvas.GetLeft(SectionControlPanel);
            double top = Canvas.GetTop(SectionControlPanel);

            if (double.IsNaN(left)) left = 900;
            if (double.IsNaN(top)) top = 600;

            var panelBounds = new Rect(left, top, SectionControlPanel.Bounds.Width, SectionControlPanel.Bounds.Height);

            if (panelBounds.Contains(position))
            {
                return true;
            }
        }

        // Check file menu panel
        if (FileMenuPanel != null && FileMenuPanel.IsVisible && FileMenuPanel.Bounds.Width > 0 && FileMenuPanel.Bounds.Height > 0)
        {
            double left = Canvas.GetLeft(FileMenuPanel);
            double top = Canvas.GetTop(FileMenuPanel);

            if (double.IsNaN(left)) left = 90;
            if (double.IsNaN(top)) top = 100;

            var panelBounds = new Rect(left, top, FileMenuPanel.Bounds.Width, FileMenuPanel.Bounds.Height);

            if (panelBounds.Contains(position))
            {
                return true;
            }
        }

        // Check view settings panel
        if (ViewSettingsPanel != null && ViewSettingsPanel.IsVisible && ViewSettingsPanel.Bounds.Width > 0 && ViewSettingsPanel.Bounds.Height > 0)
        {
            double left = Canvas.GetLeft(ViewSettingsPanel);
            double top = Canvas.GetTop(ViewSettingsPanel);

            if (double.IsNaN(left)) left = 90;
            if (double.IsNaN(top)) top = 200;

            var panelBounds = new Rect(left, top, ViewSettingsPanel.Bounds.Width, ViewSettingsPanel.Bounds.Height);

            if (panelBounds.Contains(position))
            {
                return true;
            }
        }

        // Check tools panel
        if (ToolsPanel != null && ToolsPanel.IsVisible && ToolsPanel.Bounds.Width > 0 && ToolsPanel.Bounds.Height > 0)
        {
            double left = Canvas.GetLeft(ToolsPanel);
            double top = Canvas.GetTop(ToolsPanel);

            if (double.IsNaN(left)) left = 90;
            if (double.IsNaN(top)) top = 100;

            var panelBounds = new Rect(left, top, ToolsPanel.Bounds.Width, ToolsPanel.Bounds.Height);

            if (panelBounds.Contains(position))
            {
                return true;
            }
        }

        // Check configuration panel
        if (ConfigurationPanel != null && ConfigurationPanel.IsVisible && ConfigurationPanel.Bounds.Width > 0 && ConfigurationPanel.Bounds.Height > 0)
        {
            double left = Canvas.GetLeft(ConfigurationPanel);
            double top = Canvas.GetTop(ConfigurationPanel);

            if (double.IsNaN(left)) left = 90;
            if (double.IsNaN(top)) top = 100;

            var panelBounds = new Rect(left, top, ConfigurationPanel.Bounds.Width, ConfigurationPanel.Bounds.Height);

            if (panelBounds.Contains(position))
            {
                return true;
            }
        }

        // Check job menu panel
        if (JobMenuPanel != null && JobMenuPanel.IsVisible && JobMenuPanel.Bounds.Width > 0 && JobMenuPanel.Bounds.Height > 0)
        {
            double left = Canvas.GetLeft(JobMenuPanel);
            double top = Canvas.GetTop(JobMenuPanel);

            if (double.IsNaN(left)) left = 90;
            if (double.IsNaN(top)) top = 100;

            var panelBounds = new Rect(left, top, JobMenuPanel.Bounds.Width, JobMenuPanel.Bounds.Height);

            if (panelBounds.Contains(position))
            {
                return true;
            }
        }

        // Check field tools panel
        if (FieldToolsPanel != null && FieldToolsPanel.IsVisible && FieldToolsPanel.Bounds.Width > 0 && FieldToolsPanel.Bounds.Height > 0)
        {
            double left = Canvas.GetLeft(FieldToolsPanel);
            double top = Canvas.GetTop(FieldToolsPanel);

            if (double.IsNaN(left)) left = 90;
            if (double.IsNaN(top)) top = 100;

            var panelBounds = new Rect(left, top, FieldToolsPanel.Bounds.Width, FieldToolsPanel.Bounds.Height);

            if (panelBounds.Contains(position))
            {
                return true;
            }
        }

        // Check simulator panel
        if (SimulatorPanel != null && SimulatorPanel.IsVisible && SimulatorPanel.Bounds.Width > 0 && SimulatorPanel.Bounds.Height > 0)
        {
            double left = Canvas.GetLeft(SimulatorPanel);
            double top = Canvas.GetTop(SimulatorPanel);

            if (double.IsNaN(left)) left = 400;
            if (double.IsNaN(top)) top = 100;

            var panelBounds = new Rect(left, top, SimulatorPanel.Bounds.Width, SimulatorPanel.Bounds.Height);

            if (panelBounds.Contains(position))
            {
                return true;
            }
        }

        // Check boundary recording panel
        if (BoundaryRecordingPanel != null && BoundaryRecordingPanel.IsVisible && BoundaryRecordingPanel.Bounds.Width > 0 && BoundaryRecordingPanel.Bounds.Height > 0)
        {
            double left = Canvas.GetLeft(BoundaryRecordingPanel);
            double top = Canvas.GetTop(BoundaryRecordingPanel);

            if (double.IsNaN(left)) left = 200;
            if (double.IsNaN(top)) top = 150;

            var panelBounds = new Rect(left, top, BoundaryRecordingPanel.Bounds.Width, BoundaryRecordingPanel.Bounds.Height);

            if (panelBounds.Contains(position))
            {
                return true;
            }
        }

        return false;
    }

    // AgShare Settings button click
    private async void BtnAgShareSettings_Click(object? sender, RoutedEventArgs e)
    {
        if (App.Services == null) return;

        var settingsService = App.Services.GetRequiredService<ISettingsService>();
        var settings = settingsService.Settings;

        var dialog = new AgShareSettingsDialog();
        dialog.LoadSettings(
            settings.AgShareServer,
            settings.AgShareApiKey,
            settings.AgShareEnabled);

        var result = await dialog.ShowDialog<bool>(this);

        if (result && dialog.Result != null)
        {
            // Save the new settings
            settings.AgShareServer = dialog.Result.ServerUrl;
            settings.AgShareApiKey = dialog.Result.ApiKey;
            settings.AgShareEnabled = dialog.Result.Enabled;
            settingsService.Save();

            if (ViewModel != null)
            {
                ViewModel.StatusMessage = "AgShare settings saved";
            }
        }
    }

    // AgShare Download button click
    private async void BtnAgShareDownload_Click(object? sender, RoutedEventArgs e)
    {
        if (App.Services == null || ViewModel == null) return;

        var settingsService = App.Services.GetRequiredService<ISettingsService>();
        var settings = settingsService.Settings;

        // Check if AgShare is configured
        if (string.IsNullOrEmpty(settings.AgShareApiKey))
        {
            ViewModel.StatusMessage = "Please configure AgShare API key first (hamburger menu > AgShare API)";
            return;
        }

        var dialog = new AgShareDownloadDialog(
            settings.FieldsDirectory,
            settings.AgShareServer,
            settings.AgShareApiKey);

        var result = await dialog.ShowDialog<bool>(this);

        if (result && dialog.Result != null)
        {
            // Field was downloaded - open it
            var boundaryFileService = App.Services.GetRequiredService<BoundaryFileService>();
            var boundary = boundaryFileService.LoadBoundary(dialog.Result.FieldPath);

            ViewModel.CurrentFieldName = dialog.Result.FieldName;
            ViewModel.IsFieldOpen = true;

            // Save to settings
            settings.CurrentFieldName = dialog.Result.FieldName;
            settings.LastOpenedField = dialog.Result.FieldName;
            settingsService.Save();

            // Update map
            if (MapControl != null && boundary != null)
            {
                MapControl.SetBoundary(boundary);

                // Center on boundary
                if (boundary.OuterBoundary?.Points != null && boundary.OuterBoundary.Points.Count > 0)
                {
                    double sumE = 0, sumN = 0;
                    foreach (var pt in boundary.OuterBoundary.Points)
                    {
                        sumE += pt.Easting;
                        sumN += pt.Northing;
                    }
                    MapControl.Pan(sumE / boundary.OuterBoundary.Points.Count,
                                   sumN / boundary.OuterBoundary.Points.Count);
                }
            }

            ViewModel.StatusMessage = $"Downloaded and opened field: {dialog.Result.FieldName}";
        }
    }

    // AgShare Upload button click
    private async void BtnAgShareUpload_Click(object? sender, RoutedEventArgs e)
    {
        if (App.Services == null || ViewModel == null) return;

        var settingsService = App.Services.GetRequiredService<ISettingsService>();
        var settings = settingsService.Settings;

        // Check if AgShare is configured
        if (string.IsNullOrEmpty(settings.AgShareApiKey))
        {
            ViewModel.StatusMessage = "Please configure AgShare API key first (hamburger menu > AgShare API)";
            return;
        }

        var dialog = new AgShareUploadDialog(
            settings.FieldsDirectory,
            settings.AgShareServer,
            settings.AgShareApiKey);

        var result = await dialog.ShowDialog<bool>(this);

        if (result && dialog.Result != null)
        {
            if (dialog.Result.UploadedCount > 0)
            {
                ViewModel.StatusMessage = $"Uploaded {dialog.Result.UploadedCount} field(s) to AgShare";
            }
        }
    }

    // ========== Boundary Recording Panel Handlers ==========

    private IBoundaryRecordingService? _boundaryRecordingService;
    private int _selectedBoundaryIndex = -1;
    private BoundaryType _currentBoundaryType = BoundaryType.Outer;

    private IBoundaryRecordingService BoundaryRecordingService
    {
        get
        {
            if (_boundaryRecordingService == null && App.Services != null)
            {
                _boundaryRecordingService = App.Services.GetRequiredService<IBoundaryRecordingService>();
                // Subscribe to events for live boundary display
                _boundaryRecordingService.PointAdded += BoundaryRecordingService_PointAdded;
                _boundaryRecordingService.StateChanged += BoundaryRecordingService_StateChanged;
            }
            return _boundaryRecordingService!;
        }
    }

    private void BoundaryRecordingService_PointAdded(object? sender, BoundaryPointAddedEventArgs e)
    {
        // Update the map display with the current recording points
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            UpdateRecordingDisplay();
            UpdateBoundaryPlayerDisplay();
        });
    }

    private void BoundaryRecordingService_StateChanged(object? sender, BoundaryRecordingStateChangedEventArgs e)
    {
        // Clear the recording display when recording is stopped or cancelled
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (e.State == BoundaryRecordingState.Idle)
            {
                MapControl?.ClearRecordingPoints();
            }
        });
    }

    private void UpdateRecordingDisplay()
    {
        if (MapControl == null || BoundaryRecordingService == null) return;

        var points = BoundaryRecordingService.RecordedPoints;
        if (points.Count > 0)
        {
            var pointsList = points.Select(p => (p.Easting, p.Northing)).ToList();
            MapControl.SetRecordingPoints(pointsList);
        }
        else
        {
            MapControl.ClearRecordingPoints();
        }
    }

    // Boundary button click (opens the boundary recording panel)
    private void BtnBoundary_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.IsBoundaryPanelVisible = !ViewModel.IsBoundaryPanelVisible;
            if (ViewModel.IsBoundaryPanelVisible)
            {
                RefreshBoundaryList();
            }
        }
    }

    // Refresh the boundary list display
    private void RefreshBoundaryList()
    {
        if (App.Services == null || ViewModel == null) return;

        var boundaryListPanel = this.FindControl<StackPanel>("BoundaryListPanel");
        if (boundaryListPanel == null) return;

        boundaryListPanel.Children.Clear();
        _selectedBoundaryIndex = -1;

        // Load boundary from current field
        if (string.IsNullOrEmpty(ViewModel.CurrentFieldName)) return;

        var settingsService = App.Services.GetRequiredService<ISettingsService>();
        var boundaryFileService = App.Services.GetRequiredService<BoundaryFileService>();
        var fieldPath = Path.Combine(settingsService.Settings.FieldsDirectory, ViewModel.CurrentFieldName);
        var boundary = boundaryFileService.LoadBoundary(fieldPath);

        if (boundary == null) return;

        int index = 0;

        // Add outer boundary if exists
        if (boundary.OuterBoundary != null && boundary.OuterBoundary.IsValid)
        {
            AddBoundaryListItem(boundaryListPanel, "Outer", boundary.OuterBoundary.AreaAcres, boundary.OuterBoundary.IsDriveThrough, index++);
        }

        // Add inner boundaries
        for (int i = 0; i < boundary.InnerBoundaries.Count; i++)
        {
            var inner = boundary.InnerBoundaries[i];
            if (inner.IsValid)
            {
                AddBoundaryListItem(boundaryListPanel, $"Inner {i + 1}", inner.AreaAcres, inner.IsDriveThrough, index++);
            }
        }
    }

    private void AddBoundaryListItem(StackPanel panel, string boundaryType, double areaAcres, bool isDriveThrough, int index)
    {
        var grid = new Grid
        {
            ColumnDefinitions = ColumnDefinitions.Parse("*,*,*"),
            Background = new SolidColorBrush(Color.Parse("#334C566A")),
            Tag = index
        };

        grid.PointerPressed += BoundaryListItem_PointerPressed;

        var typeText = new TextBlock
        {
            Text = boundaryType,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Colors.White),
            Padding = new Thickness(8, 6)
        };
        Grid.SetColumn(typeText, 0);

        var areaText = new TextBlock
        {
            Text = $"{areaAcres:F2} Ac",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Colors.White),
            Padding = new Thickness(8, 6)
        };
        Grid.SetColumn(areaText, 1);

        var driveThruText = new TextBlock
        {
            Text = isDriveThrough ? "Yes" : "--",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Foreground = new SolidColorBrush(isDriveThrough ? Colors.LimeGreen : Colors.Gray),
            Padding = new Thickness(8, 6)
        };
        Grid.SetColumn(driveThruText, 2);

        grid.Children.Add(typeText);
        grid.Children.Add(areaText);
        grid.Children.Add(driveThruText);

        panel.Children.Add(grid);
    }

    private void BoundaryListItem_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Grid grid && grid.Tag is int index)
        {
            _selectedBoundaryIndex = index;

            // Update selection visual
            var boundaryListPanel = this.FindControl<StackPanel>("BoundaryListPanel");
            if (boundaryListPanel != null)
            {
                foreach (var child in boundaryListPanel.Children)
                {
                    if (child is Grid g)
                    {
                        g.Background = g.Tag is int idx && idx == index
                            ? new SolidColorBrush(Color.Parse("#3498DB"))
                            : new SolidColorBrush(Color.Parse("#334C566A"));
                    }
                }
            }
        }
    }

    // Delete selected boundary
    private void BtnDeleteBoundary_Click(object? sender, RoutedEventArgs e)
    {
        if (App.Services == null || ViewModel == null || _selectedBoundaryIndex < 0)
        {
            if (ViewModel != null && _selectedBoundaryIndex < 0)
            {
                ViewModel.StatusMessage = "Select a boundary to delete";
            }
            return;
        }

        if (string.IsNullOrEmpty(ViewModel.CurrentFieldName))
        {
            ViewModel.StatusMessage = "No field open";
            return;
        }

        var settingsService = App.Services.GetRequiredService<ISettingsService>();
        var boundaryFileService = App.Services.GetRequiredService<BoundaryFileService>();
        var fieldPath = Path.Combine(settingsService.Settings.FieldsDirectory, ViewModel.CurrentFieldName);
        var boundary = boundaryFileService.LoadBoundary(fieldPath);

        if (boundary == null) return;

        int currentIndex = 0;
        bool deleted = false;

        // Check if outer boundary is selected
        if (boundary.OuterBoundary != null && boundary.OuterBoundary.IsValid)
        {
            if (currentIndex == _selectedBoundaryIndex)
            {
                boundary.OuterBoundary = null;
                deleted = true;
            }
            currentIndex++;
        }

        // Check inner boundaries
        if (!deleted)
        {
            for (int i = 0; i < boundary.InnerBoundaries.Count; i++)
            {
                if (boundary.InnerBoundaries[i].IsValid)
                {
                    if (currentIndex == _selectedBoundaryIndex)
                    {
                        boundary.InnerBoundaries.RemoveAt(i);
                        deleted = true;
                        break;
                    }
                    currentIndex++;
                }
            }
        }

        if (deleted)
        {
            boundaryFileService.SaveBoundary(boundary, fieldPath);
            RefreshBoundaryList();

            // Update map display
            if (MapControl != null)
            {
                MapControl.SetBoundary(boundary);
            }

            ViewModel.StatusMessage = "Boundary deleted";
        }
    }

    // KML Import button - imports boundary from KML file to current field
    private async void BtnKmlImportBoundary_Click(object? sender, RoutedEventArgs e)
    {
        if (App.Services == null || ViewModel == null) return;

        // Must have a field open
        if (!ViewModel.IsFieldOpen || string.IsNullOrEmpty(ViewModel.CurrentFieldName))
        {
            ViewModel.StatusMessage = "Open a field first before importing a boundary";
            return;
        }

        var settingsService = App.Services.GetRequiredService<ISettingsService>();
        var boundaryFileService = App.Services.GetRequiredService<BoundaryFileService>();

        // Open file picker for KML files
        var storageProvider = GetTopLevel(this)?.StorageProvider;
        if (storageProvider == null) return;

        var files = await storageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "Select KML File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new Avalonia.Platform.Storage.FilePickerFileType("KML Files") { Patterns = new[] { "*.kml", "*.KML" } }
            }
        });

        if (files.Count == 0) return;

        var kmlFilePath = files[0].Path.LocalPath;

        // Show the import dialog with the KML file
        var dialog = new KmlImportDialog(settingsService.Settings.FieldsDirectory);
        dialog.LoadKmlFile(kmlFilePath);

        var dialogResult = await dialog.ShowDialog<bool?>(this);

        if (dialogResult == true && dialog.Result != null)
        {
            try
            {
                var result = dialog.Result;

                // Get the field path (use current field, not a new one)
                var fieldPath = Path.Combine(settingsService.Settings.FieldsDirectory, ViewModel.CurrentFieldName);

                // Load existing boundary or create new one
                var boundary = boundaryFileService.LoadBoundary(fieldPath) ?? new Models.Boundary();

                // Convert WGS84 boundary points to local coordinates
                var origin = new AgOpenGPS.Core.Models.Wgs84(result.CenterLatitude, result.CenterLongitude);
                var sharedProps = new AgOpenGPS.Core.Models.SharedFieldProperties();
                var localPlane = new AgOpenGPS.Core.Models.LocalPlane(origin, sharedProps);

                var outerPolygon = new Models.BoundaryPolygon();

                foreach (var (lat, lon) in result.BoundaryPoints)
                {
                    var wgs84 = new AgOpenGPS.Core.Models.Wgs84(lat, lon);
                    var geoCoord = localPlane.ConvertWgs84ToGeoCoord(wgs84);
                    outerPolygon.Points.Add(new Models.BoundaryPoint(geoCoord.Easting, geoCoord.Northing, 0));
                }

                boundary.OuterBoundary = outerPolygon;

                // Save boundary
                boundaryFileService.SaveBoundary(boundary, fieldPath);

                // Update map
                if (MapControl != null)
                {
                    MapControl.SetBoundary(boundary);
                }

                // Refresh the boundary list
                RefreshBoundaryList();

                ViewModel.StatusMessage = $"Boundary imported from KML ({outerPolygon.Points.Count} points)";
            }
            catch (Exception ex)
            {
                ViewModel.StatusMessage = $"Error importing KML boundary: {ex.Message}";
            }
        }
    }

    // Bing Map button - opens browser-based map boundary drawing
    private async void BtnBingMap_Click(object? sender, RoutedEventArgs e)
    {
        if (App.Services == null || ViewModel == null) return;

        var settingsService = App.Services.GetRequiredService<ISettingsService>();
        var boundaryFileService = App.Services.GetRequiredService<BoundaryFileService>();

        // Must have a field open
        if (!ViewModel.IsFieldOpen)
        {
            ViewModel.StatusMessage = "Open a field first to add boundary";
            return;
        }

        // Get current GPS position for map centering
        double currentLat = ViewModel.Latitude;
        double currentLon = ViewModel.Longitude;

        // Show the map dialog
        var dialog = new MapsuiBoundaryDialog(currentLat, currentLon);
        var dialogResult = await dialog.ShowDialog<bool?>(this);

        if (dialogResult == true && dialog.Result != null &&
            (dialog.Result.BoundaryPoints.Count >= 3 || dialog.Result.HasBackgroundImage))
        {
            try
            {
                var result = dialog.Result;

                // Get field path
                var fieldPath = Path.Combine(settingsService.Settings.FieldsDirectory, ViewModel.CurrentFieldName);

                // Get LocalPlane - use boundary center or background center
                AgOpenGPS.Core.Models.LocalPlane? localPlane = null;

                if (result.BoundaryPoints.Count >= 3)
                {
                    // Calculate center of boundary points
                    double sumLat = 0, sumLon = 0;
                    foreach (var point in result.BoundaryPoints)
                    {
                        sumLat += point.Latitude;
                        sumLon += point.Longitude;
                    }
                    double centerLat = sumLat / result.BoundaryPoints.Count;
                    double centerLon = sumLon / result.BoundaryPoints.Count;

                    var origin = new AgOpenGPS.Core.Models.Wgs84(centerLat, centerLon);
                    var sharedProps = new AgOpenGPS.Core.Models.SharedFieldProperties();
                    localPlane = new AgOpenGPS.Core.Models.LocalPlane(origin, sharedProps);
                }
                else if (result.HasBackgroundImage)
                {
                    // Use background image center as origin
                    double centerLat = (result.NorthWestLat + result.SouthEastLat) / 2;
                    double centerLon = (result.NorthWestLon + result.SouthEastLon) / 2;

                    var origin = new AgOpenGPS.Core.Models.Wgs84(centerLat, centerLon);
                    var sharedProps = new AgOpenGPS.Core.Models.SharedFieldProperties();
                    localPlane = new AgOpenGPS.Core.Models.LocalPlane(origin, sharedProps);
                }

                // Process boundary points if present
                if (result.BoundaryPoints.Count >= 3 && localPlane != null)
                {
                    var boundary = new Models.Boundary();
                    var outerPolygon = new Models.BoundaryPolygon();

                    foreach (var point in result.BoundaryPoints)
                    {
                        var wgs84 = new AgOpenGPS.Core.Models.Wgs84(point.Latitude, point.Longitude);
                        var geoCoord = localPlane.ConvertWgs84ToGeoCoord(wgs84);
                        outerPolygon.Points.Add(new Models.BoundaryPoint(geoCoord.Easting, geoCoord.Northing, 0));
                    }

                    boundary.OuterBoundary = outerPolygon;

                    // Save boundary
                    boundaryFileService.SaveBoundary(boundary, fieldPath);

                    // Update map
                    if (MapControl != null)
                    {
                        MapControl.SetBoundary(boundary);

                        // Center on boundary
                        if (boundary.OuterBoundary.Points.Count > 0)
                        {
                            double sumE = 0, sumN = 0;
                            foreach (var pt in boundary.OuterBoundary.Points)
                            {
                                sumE += pt.Easting;
                                sumN += pt.Northing;
                            }
                            MapControl.Pan(sumE / boundary.OuterBoundary.Points.Count,
                                           sumN / boundary.OuterBoundary.Points.Count);
                        }
                    }

                    // Refresh the boundary list
                    RefreshBoundaryList();
                }

                // Process background image if present
                if (result.HasBackgroundImage && !string.IsNullOrEmpty(result.BackgroundImagePath) && localPlane != null)
                {
                    // Convert WGS84 corners to local coordinates
                    var nwWgs84 = new AgOpenGPS.Core.Models.Wgs84(result.NorthWestLat, result.NorthWestLon);
                    var seWgs84 = new AgOpenGPS.Core.Models.Wgs84(result.SouthEastLat, result.SouthEastLon);

                    var nwLocal = localPlane.ConvertWgs84ToGeoCoord(nwWgs84);
                    var seLocal = localPlane.ConvertWgs84ToGeoCoord(seWgs84);

                    // Copy background image to field directory
                    var destPngPath = Path.Combine(fieldPath, "BackPic.png");
                    File.Copy(result.BackgroundImagePath, destPngPath, overwrite: true);

                    // Save geo-reference file in WinForms format for compatibility
                    var geoFilePath = Path.Combine(fieldPath, "BackPic.txt");
                    using (var writer = new StreamWriter(geoFilePath))
                    {
                        writer.WriteLine("$BackPic");
                        writer.WriteLine("true");
                        writer.WriteLine(seLocal.Easting.ToString(CultureInfo.InvariantCulture));  // eastingMax
                        writer.WriteLine(nwLocal.Easting.ToString(CultureInfo.InvariantCulture));  // eastingMin
                        writer.WriteLine(nwLocal.Northing.ToString(CultureInfo.InvariantCulture)); // northingMax
                        writer.WriteLine(seLocal.Northing.ToString(CultureInfo.InvariantCulture)); // northingMin
                    }

                    // Set the background image in the map control
                    if (MapControl != null)
                    {
                        MapControl.SetBackgroundImage(destPngPath,
                            nwLocal.Easting, nwLocal.Northing,  // NW corner (minX, maxY)
                            seLocal.Easting, seLocal.Northing); // SE corner (maxX, minY)
                    }
                }

                // Build status message
                var msgParts = new List<string>();
                if (result.BoundaryPoints.Count >= 3)
                    msgParts.Add($"boundary ({result.BoundaryPoints.Count} pts)");
                if (result.HasBackgroundImage)
                    msgParts.Add("background image");

                ViewModel.StatusMessage = $"Imported from satellite map: {string.Join(" + ", msgParts)}";
            }
            catch (Exception ex)
            {
                ViewModel.StatusMessage = $"Error importing: {ex.Message}";
            }
        }
    }

    // Build boundary from driven tracks (placeholder)
    private void BtnBuildFromTracks_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.StatusMessage = "Build boundary from tracks not yet implemented";
        }
    }

    // Drive Around Field button - directly opens BoundaryPlayerPanel to record boundary
    private void BtnDriveAroundField_Click(object? sender, RoutedEventArgs e)
    {
        // Must have a field open
        if (ViewModel == null || !ViewModel.IsFieldOpen || string.IsNullOrEmpty(ViewModel.CurrentFieldName))
        {
            if (ViewModel != null)
                ViewModel.StatusMessage = "Open a field first before recording a boundary";
            return;
        }

        // Hide the main boundary panel
        ViewModel.IsBoundaryPanelVisible = false;

        // Show the BoundaryPlayerPanel
        var playerPanel = this.FindControl<Border>("BoundaryPlayerPanel");
        if (playerPanel != null)
        {
            playerPanel.IsVisible = true;
        }

        // Show boundary offset indicator on map
        UpdateBoundaryOffsetIndicator();

        // Initialize recording service for a new boundary
        if (BoundaryRecordingService != null)
        {
            BoundaryRecordingService.StartRecording(BoundaryType.Outer);
            _isRecording = false; // Start in paused state (user must click Record to start)
            BoundaryRecordingService.PauseRecording();
            UpdateBoundaryPlayerDisplay();
        }

        if (ViewModel != null)
        {
            ViewModel.StatusMessage = "Drive around the field boundary. Click Record to start.";
        }
    }

    // Add new boundary button - shows choice panel (kept for AddBoundaryChoicePanel)
    private void BtnAddBoundary_Click(object? sender, RoutedEventArgs e)
    {
        // Show the Add Boundary Choice panel
        var choicePanel = this.FindControl<Border>("AddBoundaryChoicePanel");
        if (choicePanel != null)
        {
            choicePanel.IsVisible = true;
        }
    }

    // Import KML button - open file dialog to import KML boundary
    private void BtnImportKml_Click(object? sender, RoutedEventArgs e)
    {
        // Hide the choice panel
        var choicePanel = this.FindControl<Border>("AddBoundaryChoicePanel");
        if (choicePanel != null)
        {
            choicePanel.IsVisible = false;
        }

        if (ViewModel != null)
        {
            ViewModel.StatusMessage = "KML import not yet implemented";
        }
        // TODO: Implement KML file import
    }

    // Drive/Record button - show BoundaryPlayerPanel
    private void BtnDriveRecord_Click(object? sender, RoutedEventArgs e)
    {
        // Hide the choice panel
        var choicePanel = this.FindControl<Border>("AddBoundaryChoicePanel");
        if (choicePanel != null)
        {
            choicePanel.IsVisible = false;
        }

        // Hide the main boundary panel
        if (ViewModel != null)
        {
            ViewModel.IsBoundaryPanelVisible = false;
        }

        // Show the BoundaryPlayerPanel
        var playerPanel = this.FindControl<Border>("BoundaryPlayerPanel");
        if (playerPanel != null)
        {
            playerPanel.IsVisible = true;
        }

        // Show boundary offset indicator on map
        UpdateBoundaryOffsetIndicator();

        // Initialize recording service for a new boundary
        if (BoundaryRecordingService != null)
        {
            BoundaryRecordingService.StartRecording(BoundaryType.Outer);
            _isRecording = false; // Start in paused state (user must click Record to start)
            BoundaryRecordingService.PauseRecording();
            UpdateBoundaryPlayerDisplay();
        }

        if (ViewModel != null)
        {
            ViewModel.StatusMessage = "Boundary recording ready - Click Record (R) to start";
        }
    }

    // Cancel add boundary choice
    private void BtnCancelAddBoundary_Click(object? sender, RoutedEventArgs e)
    {
        // Hide the choice panel
        var choicePanel = this.FindControl<Border>("AddBoundaryChoicePanel");
        if (choicePanel != null)
        {
            choicePanel.IsVisible = false;
        }
    }

    // Close boundary panel
    private void BtnCloseBoundaryPanel_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.IsBoundaryPanelVisible = false;
        }
    }

    // Select outer boundary type
    private void BtnOuterBoundary_Click(object? sender, RoutedEventArgs e)
    {
        _currentBoundaryType = BoundaryType.Outer;
        if (ViewModel != null)
        {
            ViewModel.StatusMessage = "Outer boundary selected";
        }
    }

    // Select inner boundary type
    private void BtnInnerBoundary_Click(object? sender, RoutedEventArgs e)
    {
        _currentBoundaryType = BoundaryType.Inner;
        if (ViewModel != null)
        {
            ViewModel.StatusMessage = "Inner boundary selected";
        }
    }

    // Determine which boundary type is selected
    private BoundaryType GetSelectedBoundaryType()
    {
        return _currentBoundaryType;
    }

    // Start/Resume recording boundary
    private void BtnRecordBoundary_Click(object? sender, RoutedEventArgs e)
    {
        if (BoundaryRecordingService == null) return;

        var state = BoundaryRecordingService.State;
        if (state == BoundaryRecordingState.Idle)
        {
            // Start new recording
            var boundaryType = GetSelectedBoundaryType();
            BoundaryRecordingService.StartRecording(boundaryType);
            UpdateBoundaryStatusDisplay();

            // Show recording status panel
            var recordingStatusPanel = this.FindControl<Border>("RecordingStatusPanel");
            var recordingControlsPanel = this.FindControl<Grid>("RecordingControlsPanel");
            if (recordingStatusPanel != null) recordingStatusPanel.IsVisible = true;
            if (recordingControlsPanel != null) recordingControlsPanel.IsVisible = true;

            // Update accept button
            var acceptBtn = this.FindControl<Button>("BtnAcceptBoundary");
            if (acceptBtn != null) acceptBtn.IsEnabled = true;

            if (ViewModel != null)
            {
                ViewModel.StatusMessage = $"Recording {boundaryType} boundary - drive around the perimeter";
            }
        }
        else if (state == BoundaryRecordingState.Paused)
        {
            // Resume recording
            BoundaryRecordingService.ResumeRecording();
            UpdateBoundaryStatusDisplay();
            if (ViewModel != null)
            {
                ViewModel.StatusMessage = "Resumed boundary recording";
            }
        }
    }

    // Pause recording
    private void BtnPauseBoundary_Click(object? sender, RoutedEventArgs e)
    {
        if (BoundaryRecordingService == null) return;

        if (BoundaryRecordingService.State == BoundaryRecordingState.Recording)
        {
            BoundaryRecordingService.PauseRecording();
            UpdateBoundaryStatusDisplay();
            if (ViewModel != null)
            {
                ViewModel.StatusMessage = "Boundary recording paused";
            }
        }
    }

    // Stop recording and save boundary
    private void BtnStopBoundary_Click(object? sender, RoutedEventArgs e)
    {
        if (BoundaryRecordingService == null || App.Services == null || ViewModel == null) return;

        if (BoundaryRecordingService.State != BoundaryRecordingState.Idle)
        {
            // Get the current boundary type before stopping
            var isOuter = BoundaryRecordingService.CurrentBoundaryType == BoundaryType.Outer;
            var polygon = BoundaryRecordingService.StopRecording();

            if (polygon != null && polygon.Points.Count >= 3)
            {
                // Save the boundary to the current field
                var settingsService = App.Services.GetRequiredService<ISettingsService>();
                var boundaryFileService = App.Services.GetRequiredService<BoundaryFileService>();

                if (!string.IsNullOrEmpty(ViewModel.CurrentFieldName))
                {
                    var fieldPath = Path.Combine(settingsService.Settings.FieldsDirectory, ViewModel.CurrentFieldName);

                    // Load existing boundary or create new one
                    var boundary = boundaryFileService.LoadBoundary(fieldPath) ?? new Models.Boundary();

                    if (isOuter)
                    {
                        boundary.OuterBoundary = polygon;
                    }
                    else
                    {
                        boundary.InnerBoundaries.Add(polygon);
                    }

                    // Save boundary
                    boundaryFileService.SaveBoundary(boundary, fieldPath);

                    // Update map display
                    if (MapControl != null)
                    {
                        MapControl.SetBoundary(boundary);
                    }

                    ViewModel.StatusMessage = $"Saved {(isOuter ? "outer" : "inner")} boundary ({polygon.Points.Count} points, {polygon.AreaHectares:F2} ha)";

                    // Refresh the boundary list
                    RefreshBoundaryList();
                }
                else
                {
                    ViewModel.StatusMessage = "No field open - boundary not saved";
                }
            }
            else
            {
                ViewModel.StatusMessage = "Boundary cancelled (not enough points)";
            }

            // Hide recording panels
            var recordingStatusPanel = this.FindControl<Border>("RecordingStatusPanel");
            var recordingControlsPanel = this.FindControl<Grid>("RecordingControlsPanel");
            if (recordingStatusPanel != null) recordingStatusPanel.IsVisible = false;
            if (recordingControlsPanel != null) recordingControlsPanel.IsVisible = false;

            // Update accept button
            var acceptBtn = this.FindControl<Button>("BtnAcceptBoundary");
            if (acceptBtn != null) acceptBtn.IsEnabled = false;

            UpdateBoundaryStatusDisplay();
        }
    }

    // Undo last boundary point
    private void BtnUndoBoundaryPoint_Click(object? sender, RoutedEventArgs e)
    {
        if (BoundaryRecordingService == null) return;

        if (BoundaryRecordingService.RemoveLastPoint())
        {
            UpdateBoundaryStatusDisplay();
            UpdateRecordingDisplay(); // Update map display
            if (ViewModel != null)
            {
                ViewModel.StatusMessage = $"Removed point ({BoundaryRecordingService.PointCount} remaining)";
            }
        }
    }

    // Clear all boundary points
    private void BtnClearBoundary_Click(object? sender, RoutedEventArgs e)
    {
        if (BoundaryRecordingService == null) return;

        BoundaryRecordingService.ClearPoints();
        UpdateBoundaryStatusDisplay();
        if (ViewModel != null)
        {
            ViewModel.StatusMessage = "Boundary points cleared";
        }
    }

    // Update the boundary status display text blocks
    private void UpdateBoundaryStatusDisplay()
    {
        if (BoundaryRecordingService == null) return;

        var statusText = this.FindControl<TextBlock>("BoundaryStatusLabel");
        var pointsText = this.FindControl<TextBlock>("BoundaryPointsLabel");
        var areaText = this.FindControl<TextBlock>("BoundaryAreaLabel");

        if (statusText != null)
        {
            statusText.Text = BoundaryRecordingService.State.ToString();
        }

        if (pointsText != null)
        {
            pointsText.Text = BoundaryRecordingService.PointCount.ToString();
        }

        if (areaText != null)
        {
            areaText.Text = $"{BoundaryRecordingService.AreaHectares:F2} Ha";
        }

        // Update button enabled states
        var recordBtn = this.FindControl<Button>("BtnRecordBoundary");
        var pauseBtn = this.FindControl<Button>("BtnPauseBoundary");
        var stopBtn = this.FindControl<Button>("BtnStopBoundary");

        var state = BoundaryRecordingService.State;
        if (recordBtn != null)
        {
            recordBtn.IsEnabled = state == BoundaryRecordingState.Idle || state == BoundaryRecordingState.Paused;
        }
        if (pauseBtn != null)
        {
            pauseBtn.IsEnabled = state == BoundaryRecordingState.Recording;
        }
        if (stopBtn != null)
        {
            stopBtn.IsEnabled = state != BoundaryRecordingState.Idle;
        }
    }

    // Boundary Panel drag handlers
    private void BoundaryPanel_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (BoundaryRecordingPanel != null && sender is Grid header)
        {
            _dragStartPoint = e.GetPosition(this);
            e.Pointer.Capture(header);
            ToolTip.SetIsOpen(header, false);
            e.Handled = true;
        }
    }

    private void BoundaryPanel_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (BoundaryRecordingPanel != null && e.Pointer.Captured == sender && sender is Grid header)
        {
            var currentPoint = e.GetPosition(this);
            var distance = Math.Sqrt(Math.Pow(currentPoint.X - _dragStartPoint.X, 2) +
                                    Math.Pow(currentPoint.Y - _dragStartPoint.Y, 2));

            if (!_isDraggingBoundary && distance > TapDistanceThreshold)
            {
                _isDraggingBoundary = true;
                ToolTip.SetIsOpen(header, false);
            }

            if (_isDraggingBoundary)
            {
                var delta = currentPoint - _dragStartPoint;

                double newLeft = Canvas.GetLeft(BoundaryRecordingPanel) + delta.X;
                double newTop = Canvas.GetTop(BoundaryRecordingPanel) + delta.Y;

                double maxLeft = Bounds.Width - BoundaryRecordingPanel.Bounds.Width;
                double maxTop = Bounds.Height - BoundaryRecordingPanel.Bounds.Height;

                newLeft = Math.Clamp(newLeft, 0, Math.Max(0, maxLeft));
                newTop = Math.Clamp(newTop, 0, Math.Max(0, maxTop));

                Canvas.SetLeft(BoundaryRecordingPanel, newLeft);
                Canvas.SetTop(BoundaryRecordingPanel, newTop);

                _dragStartPoint = currentPoint;
            }

            e.Handled = true;
        }
    }

    private void BoundaryPanel_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (BoundaryRecordingPanel != null && e.Pointer.Captured == sender)
        {
            _isDraggingBoundary = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    #region BoundaryPlayerPanel Event Handlers

    // Drag support for BoundaryPlayerPanel
    private void BoundaryPlayerPanel_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (BoundaryPlayerPanel != null)
        {
            _dragStartPoint = e.GetPosition(this);
            e.Pointer.Capture((Control)sender!);
            e.Handled = true;
        }
    }

    private void BoundaryPlayerPanel_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (BoundaryPlayerPanel != null && e.Pointer.Captured == sender)
        {
            var currentPoint = e.GetPosition(this);
            double distance = Math.Sqrt(Math.Pow(currentPoint.X - _dragStartPoint.X, 2) + Math.Pow(currentPoint.Y - _dragStartPoint.Y, 2));

            if (!_isDraggingBoundaryPlayer && distance > TapDistanceThreshold)
            {
                _isDraggingBoundaryPlayer = true;
            }

            if (_isDraggingBoundaryPlayer)
            {
                var offset = currentPoint - _dragStartPoint;
                double currentLeft = Canvas.GetLeft(BoundaryPlayerPanel);
                double currentTop = Canvas.GetTop(BoundaryPlayerPanel);

                if (double.IsNaN(currentLeft)) currentLeft = 100;
                if (double.IsNaN(currentTop)) currentTop = 100;

                double newLeft = Math.Max(0, Math.Min(currentLeft + offset.X, Bounds.Width - BoundaryPlayerPanel.Bounds.Width));
                double newTop = Math.Max(0, Math.Min(currentTop + offset.Y, Bounds.Height - BoundaryPlayerPanel.Bounds.Height));

                Canvas.SetLeft(BoundaryPlayerPanel, newLeft);
                Canvas.SetTop(BoundaryPlayerPanel, newTop);
                _dragStartPoint = currentPoint;
            }
            e.Handled = true;
        }
    }

    private void BoundaryPlayerPanel_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (BoundaryPlayerPanel != null && e.Pointer.Captured == sender)
        {
            _isDraggingBoundaryPlayer = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    // Update the BoundaryPlayerPanel display
    private void UpdateBoundaryPlayerDisplay()
    {
        if (BoundaryRecordingService == null) return;

        var pointsLabel = this.FindControl<TextBlock>("BoundaryPlayerPointsLabel");
        var areaLabel = this.FindControl<TextBlock>("BoundaryPlayerAreaLabel");
        var pausePlayImage = this.FindControl<Image>("BoundaryPausePlayImage");

        if (pointsLabel != null)
        {
            pointsLabel.Text = BoundaryRecordingService.PointCount.ToString();
        }

        if (areaLabel != null)
        {
            areaLabel.Text = BoundaryRecordingService.AreaHectares.ToString("F2");
        }

        // Update Record/Pause button image
        if (pausePlayImage != null)
        {
            try
            {
                var assetUri = _isRecording
                    ? new Uri("avares://AgValoniaGPS.Desktop/Assets/Icons/boundaryPause.png")
                    : new Uri("avares://AgValoniaGPS.Desktop/Assets/Icons/BoundaryRecord.png");
                pausePlayImage.Source = new Avalonia.Media.Imaging.Bitmap(Avalonia.Platform.AssetLoader.Open(assetUri));
            }
            catch { }
        }
    }

    // Offset button clicked - show numeric keypad
    private async void BtnBoundaryOffset_Click(object? sender, RoutedEventArgs e)
    {
        // Show numeric keypad dialog for offset input (0-500, direction controlled by Left/Right button)
        var result = await OnScreenKeyboard.ShowAsync(
            this,
            description: "Boundary Offset (cm)",
            initialValue: _boundaryOffset,
            minValue: 0,
            maxValue: 500,
            maxDecimalPlaces: 0,
            allowNegative: false);

        if (result.HasValue)
        {
            _boundaryOffset = result.Value;

            // Update the display
            var offsetLabel = this.FindControl<TextBlock>("BoundaryOffsetValue");
            if (offsetLabel != null)
            {
                offsetLabel.Text = _boundaryOffset.ToString("F0");
            }

            // Update the offset indicator on the map
            UpdateBoundaryOffsetIndicator();

            if (ViewModel != null)
            {
                ViewModel.StatusMessage = $"Boundary offset set to {_boundaryOffset:F0} cm";
            }
        }
    }

    // Helper to update the boundary offset indicator with the correct direction
    private void UpdateBoundaryOffsetIndicator()
    {
        // Apply direction: right side = positive offset, left side = negative offset
        double signedOffsetMeters = _boundaryOffset / 100.0;
        if (!_isDrawRightSide)
        {
            signedOffsetMeters = -signedOffsetMeters;
        }
        MapControl?.SetBoundaryOffsetIndicator(true, signedOffsetMeters);
    }

    // Restart boundary recording (delete all points)
    private void BtnBoundaryRestart_Click(object? sender, RoutedEventArgs e)
    {
        if (BoundaryRecordingService == null) return;

        // TODO: Show confirmation dialog
        BoundaryRecordingService.ClearPoints();
        _isRecording = false;
        UpdateBoundaryPlayerDisplay();
        UpdateRecordingDisplay(); // Update map display

        if (ViewModel != null)
        {
            ViewModel.StatusMessage = "Boundary points cleared";
        }
    }

    // Toggle section control for boundary recording - Checked event
    private void BtnBoundarySectionControl_Checked(object? sender, RoutedEventArgs e)
    {
        _isBoundarySectionControlOn = true;

        if (ViewModel != null)
        {
            ViewModel.StatusMessage = "Boundary records when section is on";
        }
    }

    // Toggle section control for boundary recording - Unchecked event
    private void BtnBoundarySectionControl_Unchecked(object? sender, RoutedEventArgs e)
    {
        _isBoundarySectionControlOn = false;

        if (ViewModel != null)
        {
            ViewModel.StatusMessage = "Boundary section control off";
        }
    }

    // Delete last point
    private void BtnBoundaryDeleteLast_Click(object? sender, RoutedEventArgs e)
    {
        if (BoundaryRecordingService == null) return;

        BoundaryRecordingService.RemoveLastPoint();
        UpdateBoundaryPlayerDisplay();
        UpdateRecordingDisplay(); // Update map display

        if (ViewModel != null)
        {
            ViewModel.StatusMessage = "Last point deleted";
        }
    }

    // Toggle left/right side
    private void BtnBoundaryLeftRight_Click(object? sender, RoutedEventArgs e)
    {
        _isDrawRightSide = !_isDrawRightSide;

        var image = this.FindControl<Image>("BoundaryLeftRightImage");
        if (image != null)
        {
            try
            {
                var assetUri = _isDrawRightSide
                    ? new Uri("avares://AgValoniaGPS.Desktop/Assets/Icons/BoundaryRight.png")
                    : new Uri("avares://AgValoniaGPS.Desktop/Assets/Icons/BoundaryLeft.png");
                image.Source = new Avalonia.Media.Imaging.Bitmap(Avalonia.Platform.AssetLoader.Open(assetUri));
            }
            catch { }
        }

        // Update the offset indicator (offset sign depends on left/right side)
        UpdateBoundaryOffsetIndicator();

        if (ViewModel != null)
        {
            ViewModel.StatusMessage = _isDrawRightSide ? "Boundary on right side" : "Boundary on left side";
        }
    }

    // Toggle antenna/tool position
    private void BtnBoundaryAntennaTool_Click(object? sender, RoutedEventArgs e)
    {
        _isDrawAtPivot = !_isDrawAtPivot;

        var image = this.FindControl<Image>("BoundaryAntennaToolImage");
        if (image != null)
        {
            try
            {
                var assetUri = _isDrawAtPivot
                    ? new Uri("avares://AgValoniaGPS.Desktop/Assets/Icons/BoundaryRecordPivot.png")
                    : new Uri("avares://AgValoniaGPS.Desktop/Assets/Icons/BoundaryRecordTool.png");
                image.Source = new Avalonia.Media.Imaging.Bitmap(Avalonia.Platform.AssetLoader.Open(assetUri));
            }
            catch { }
        }

        if (ViewModel != null)
        {
            ViewModel.StatusMessage = _isDrawAtPivot ? "Recording at pivot point" : "Recording at tool";
        }
    }

    // Calculate offset position perpendicular to heading
    // Returns (easting, northing) with offset applied
    private (double easting, double northing) CalculateOffsetPosition(double easting, double northing, double headingRadians)
    {
        if (_boundaryOffset == 0)
            return (easting, northing);

        // Offset in meters (input is cm)
        double offsetMeters = _boundaryOffset / 100.0;

        // If drawing on left side, negate the offset
        if (!_isDrawRightSide)
            offsetMeters = -offsetMeters;

        // Calculate perpendicular offset (90 degrees to the right of heading)
        // Right is +90 degrees (π/2 radians) from heading direction
        double perpAngle = headingRadians + Math.PI / 2.0;

        double offsetEasting = easting + offsetMeters * Math.Sin(perpAngle);
        double offsetNorthing = northing + offsetMeters * Math.Cos(perpAngle);

        return (offsetEasting, offsetNorthing);
    }

    // Add point manually
    private void BtnBoundaryAddPoint_Click(object? sender, RoutedEventArgs e)
    {
        if (BoundaryRecordingService == null || ViewModel == null) return;

        // Calculate offset position based on boundary offset setting
        double headingRadians = ViewModel.Heading * Math.PI / 180.0;
        var (offsetEasting, offsetNorthing) = CalculateOffsetPosition(
            ViewModel.Easting, ViewModel.Northing, headingRadians);

        // Add a UTM point with offset applied
        BoundaryRecordingService.AddPoint(offsetEasting, offsetNorthing, headingRadians);
        UpdateBoundaryPlayerDisplay();

        if (ViewModel != null)
        {
            ViewModel.StatusMessage = $"Point added ({BoundaryRecordingService.PointCount} total)";
        }
    }

    // Stop recording and save boundary
    private void BtnBoundaryStop_Click(object? sender, RoutedEventArgs e)
    {
        if (BoundaryRecordingService == null || App.Services == null || ViewModel == null) return;

        // Get the recorded polygon
        var polygon = BoundaryRecordingService.StopRecording();

        if (polygon != null && polygon.Points.Count >= 3)
        {
            // Save the boundary to the current field
            var settingsService = App.Services.GetRequiredService<ISettingsService>();
            var boundaryFileService = App.Services.GetRequiredService<BoundaryFileService>();

            if (!string.IsNullOrEmpty(ViewModel.CurrentFieldName))
            {
                var fieldPath = Path.Combine(settingsService.Settings.FieldsDirectory, ViewModel.CurrentFieldName);

                // Load existing boundary or create new one
                var boundary = boundaryFileService.LoadBoundary(fieldPath) ?? new Models.Boundary();

                // Set as outer boundary
                boundary.OuterBoundary = polygon;

                // Save the boundary (boundary first, then directory)
                boundaryFileService.SaveBoundary(boundary, fieldPath);

                // Update the map
                if (MapControl != null)
                {
                    MapControl.SetBoundary(boundary);
                }

                ViewModel.StatusMessage = $"Boundary saved with {polygon.Points.Count} points, Area: {polygon.AreaHectares:F2} Ha";
            }
            else
            {
                ViewModel.StatusMessage = "Cannot save boundary - no field is open";
            }
        }
        else
        {
            ViewModel.StatusMessage = "Boundary not saved - need at least 3 points";
        }

        // Hide the BoundaryPlayerPanel
        var playerPanel = this.FindControl<Border>("BoundaryPlayerPanel");
        if (playerPanel != null)
        {
            playerPanel.IsVisible = false;
        }

        // Hide boundary offset indicator
        MapControl?.SetBoundaryOffsetIndicator(false, 0);

        _isRecording = false;
    }

    // Toggle recording/pause
    private void BtnBoundaryPausePlay_Click(object? sender, RoutedEventArgs e)
    {
        if (BoundaryRecordingService == null) return;

        if (_isRecording)
        {
            // Pause recording
            BoundaryRecordingService.PauseRecording();
            _isRecording = false;

            // Enable manual point controls when paused
            var addPointBtn = this.FindControl<Button>("BtnBoundaryAddPoint");
            var deleteLastBtn = this.FindControl<Button>("BtnBoundaryDeleteLast");
            if (addPointBtn != null) addPointBtn.IsEnabled = true;
            if (deleteLastBtn != null) deleteLastBtn.IsEnabled = true;

            if (ViewModel != null)
            {
                ViewModel.StatusMessage = "Recording paused";
            }
        }
        else
        {
            // Resume recording
            BoundaryRecordingService.ResumeRecording();
            _isRecording = true;

            // Disable manual point controls while recording
            var addPointBtn = this.FindControl<Button>("BtnBoundaryAddPoint");
            var deleteLastBtn = this.FindControl<Button>("BtnBoundaryDeleteLast");
            if (addPointBtn != null) addPointBtn.IsEnabled = false;
            if (deleteLastBtn != null) deleteLastBtn.IsEnabled = false;

            if (ViewModel != null)
            {
                ViewModel.StatusMessage = "Recording boundary - drive around the perimeter";
            }
        }

        // Update button image
        var pausePlayImage = this.FindControl<Image>("BoundaryPausePlayImage");
        if (pausePlayImage != null)
        {
            try
            {
                var assetUri = _isRecording
                    ? new Uri("avares://AgValoniaGPS.Desktop/Assets/Icons/boundaryPause.png")
                    : new Uri("avares://AgValoniaGPS.Desktop/Assets/Icons/BoundaryRecord.png");
                pausePlayImage.Source = new Avalonia.Media.Imaging.Bitmap(Avalonia.Platform.AssetLoader.Open(assetUri));
            }
            catch { }
        }
    }

    #endregion
}