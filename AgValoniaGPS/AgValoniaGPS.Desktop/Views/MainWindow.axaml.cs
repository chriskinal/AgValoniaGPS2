using System;
using System.IO;
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
    private Avalonia.Point _dragStartPoint;
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

        return false;
    }
}