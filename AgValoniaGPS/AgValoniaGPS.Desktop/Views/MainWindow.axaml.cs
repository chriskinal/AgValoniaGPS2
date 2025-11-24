using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using AgValoniaGPS.ViewModels;
using AgValoniaGPS.Services;

namespace AgValoniaGPS.Desktop.Views;

public partial class MainWindow : Window
{
    private MainViewModel? ViewModel => DataContext as MainViewModel;
    private bool _isDraggingSection = false;
    private bool _isDraggingLeftPanel = false;
    private bool _isDraggingFileMenu = false;
    private bool _isDraggingViewSettings = false;
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

        // Subscribe to GPS position changes
        if (ViewModel != null)
        {
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        // Add keyboard shortcut for 3D mode toggle (F3)
        this.KeyDown += MainWindow_KeyDown;
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

        return false;
    }
}