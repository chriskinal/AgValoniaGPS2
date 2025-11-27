using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using AgOpenGPS.Core.Services.AgShare;
using AgOpenGPS.Core.Models;
using AgOpenGPS.Core.Models.AgShare;
using AgOpenGPS.Core.Models.Base;
using AgValoniaGPS.Services;

namespace AgValoniaGPS.Desktop.Views;

/// <summary>
/// View model for local field list items in the upload dialog
/// </summary>
public class LocalFieldListItem : INotifyPropertyChanged
{
    private bool _isSelected;

    public string Name { get; set; } = string.Empty;
    public string DirectoryPath { get; set; } = string.Empty;
    public bool HasBoundary { get; set; }
    public string HasBoundaryDisplay => HasBoundary ? "Yes" : "No";
    public IBrush HasBoundaryColor => HasBoundary
        ? new SolidColorBrush(Color.Parse("#27AE60"))
        : new SolidColorBrush(Color.Parse("#BDC3C7"));

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>
/// Result from the AgShare upload dialog.
/// </summary>
public class AgShareUploadResult
{
    public int UploadedCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> UploadedFields { get; set; } = new();
    public List<string> FailedFields { get; set; } = new();
}

public partial class AgShareUploadDialog : Window
{
    private readonly string _fieldsRootDirectory;
    private readonly AgShareClient _client;
    private readonly AgShareUploaderService _uploaderService;
    private readonly BoundaryFileService _boundaryFileService;
    private ObservableCollection<LocalFieldListItem> _fields = new();

    public AgShareUploadResult? Result { get; private set; }

    public AgShareUploadDialog() : this(string.Empty, string.Empty, string.Empty)
    {
    }

    public AgShareUploadDialog(string fieldsRootDirectory, string serverUrl, string apiKey)
    {
        _fieldsRootDirectory = fieldsRootDirectory;

        var url = string.IsNullOrEmpty(serverUrl) ? "https://agshare.agopengps.com" : serverUrl;
        _client = new AgShareClient(url, apiKey);

        _uploaderService = new AgShareUploaderService();
        _boundaryFileService = new BoundaryFileService();

        InitializeComponent();

        FieldListBox.ItemsSource = _fields;

        // Subscribe to selection changes to update button state
        foreach (var field in _fields)
        {
            field.PropertyChanged += Field_PropertyChanged;
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        LoadLocalFields();
    }

    private void LoadLocalFields()
    {
        StatusLabel.Text = "Loading local fields...";
        _fields.Clear();
        BtnUpload.IsEnabled = false;

        try
        {
            if (!Directory.Exists(_fieldsRootDirectory))
            {
                StatusLabel.Text = "Fields directory not found";
                return;
            }

            var fieldDirs = Directory.GetDirectories(_fieldsRootDirectory);

            if (fieldDirs.Length == 0)
            {
                StatusLabel.Text = "No fields found in local directory";
                return;
            }

            foreach (var dir in fieldDirs.OrderBy(d => Path.GetFileName(d)))
            {
                var fieldName = Path.GetFileName(dir);

                // Check if Field.txt exists (indicates valid field directory)
                var fieldFilePath = Path.Combine(dir, "Field.txt");
                if (!File.Exists(fieldFilePath))
                    continue;

                // Check for boundary file
                var boundaryPath = Path.Combine(dir, "Boundary.txt");
                var hasBoundary = File.Exists(boundaryPath);

                var item = new LocalFieldListItem
                {
                    Name = fieldName,
                    DirectoryPath = dir,
                    HasBoundary = hasBoundary
                };

                item.PropertyChanged += Field_PropertyChanged;
                _fields.Add(item);
            }

            StatusLabel.Text = $"Found {_fields.Count} field(s) - select fields to upload";
            UpdateUploadButtonState();
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error loading fields: {ex.Message}";
        }
    }

    private void Field_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LocalFieldListItem.IsSelected))
        {
            Dispatcher.UIThread.Invoke(UpdateUploadButtonState);
        }
    }

    private void UpdateUploadButtonState()
    {
        var selectedCount = _fields.Count(f => f.IsSelected);
        BtnUpload.IsEnabled = selectedCount > 0;

        if (selectedCount > 0)
        {
            StatusLabel.Text = $"{selectedCount} field(s) selected for upload";
        }
        else
        {
            StatusLabel.Text = $"Found {_fields.Count} field(s) - select fields to upload";
        }
    }

    private void BtnSelectAll_Click(object? sender, RoutedEventArgs e)
    {
        foreach (var field in _fields)
        {
            field.IsSelected = true;
        }
    }

    private void BtnDeselectAll_Click(object? sender, RoutedEventArgs e)
    {
        foreach (var field in _fields)
        {
            field.IsSelected = false;
        }
    }

    private async void BtnUpload_Click(object? sender, RoutedEventArgs e)
    {
        var selectedFields = _fields.Where(f => f.IsSelected).ToList();

        if (selectedFields.Count == 0)
        {
            StatusLabel.Text = "No fields selected";
            StatusLabel.Foreground = new SolidColorBrush(Color.Parse("#E74C3C"));
            return;
        }

        ProgressBar.IsVisible = true;
        ProgressBar.Value = 0;
        BtnUpload.IsEnabled = false;

        var uploadedFields = new List<string>();
        var failedFields = new List<string>();
        var isPublic = PublicCheckBox.IsChecked ?? false;

        try
        {
            for (int i = 0; i < selectedFields.Count; i++)
            {
                var field = selectedFields[i];
                var progress = (int)((i + 1) * 100.0 / selectedFields.Count);

                StatusLabel.Text = $"Uploading '{field.Name}'... ({i + 1}/{selectedFields.Count})";
                StatusLabel.Foreground = new SolidColorBrush(Color.Parse("#BDC3C7"));
                ProgressBar.Value = progress;

                var (success, message) = await UploadSingleFieldAsync(field, isPublic);

                if (success)
                {
                    uploadedFields.Add(field.Name);
                }
                else
                {
                    failedFields.Add($"{field.Name}: {message}");
                }
            }

            // Set result
            Result = new AgShareUploadResult
            {
                UploadedCount = uploadedFields.Count,
                FailedCount = failedFields.Count,
                UploadedFields = uploadedFields,
                FailedFields = failedFields
            };

            if (failedFields.Count == 0)
            {
                StatusLabel.Text = $"Successfully uploaded {uploadedFields.Count} field(s)!";
                StatusLabel.Foreground = new SolidColorBrush(Color.Parse("#27AE60"));
                await Task.Delay(1500);
                Close(true);
            }
            else if (uploadedFields.Count > 0)
            {
                StatusLabel.Text = $"Uploaded {uploadedFields.Count}, failed {failedFields.Count}";
                StatusLabel.Foreground = new SolidColorBrush(Color.Parse("#F39C12"));
            }
            else
            {
                StatusLabel.Text = $"Failed to upload any fields";
                StatusLabel.Foreground = new SolidColorBrush(Color.Parse("#E74C3C"));
            }
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
            StatusLabel.Foreground = new SolidColorBrush(Color.Parse("#E74C3C"));
        }
        finally
        {
            ProgressBar.IsVisible = false;
            BtnUpload.IsEnabled = true;
        }
    }

    private async Task<(bool success, string message)> UploadSingleFieldAsync(LocalFieldListItem field, bool isPublic)
    {
        try
        {
            // Read field origin from Field.txt
            Wgs84 fieldOrigin = new Wgs84(0, 0);
            var fieldFilePath = Path.Combine(field.DirectoryPath, "Field.txt");
            if (File.Exists(fieldFilePath))
            {
                var lines = await File.ReadAllLinesAsync(fieldFilePath);
                if (lines.Length >= 2)
                {
                    if (double.TryParse(lines[0], out var lat) && double.TryParse(lines[1], out var lon))
                    {
                        fieldOrigin = new Wgs84(lat, lon);
                    }
                }
            }

            // Load boundary
            var boundaries = new List<List<Vec3>>();
            if (field.HasBoundary)
            {
                var boundary = _boundaryFileService.LoadBoundary(field.DirectoryPath);
                if (boundary?.OuterBoundary != null && boundary.OuterBoundary.Points.Count > 0)
                {
                    var outerVec3 = new List<Vec3>();
                    foreach (var pt in boundary.OuterBoundary.Points)
                    {
                        outerVec3.Add(new Vec3(pt.Easting, pt.Northing, pt.Heading));
                    }
                    boundaries.Add(outerVec3);

                    // Add inner boundaries if any
                    if (boundary.InnerBoundaries != null)
                    {
                        foreach (var inner in boundary.InnerBoundaries)
                        {
                            var innerVec3 = new List<Vec3>();
                            foreach (var pt in inner.Points)
                            {
                                innerVec3.Add(new Vec3(pt.Easting, pt.Northing, pt.Heading));
                            }
                            boundaries.Add(innerVec3);
                        }
                    }
                }
            }

            if (boundaries.Count == 0)
            {
                return (false, "No boundary data");
            }

            // Check for existing AgShare field ID
            Guid? existingFieldId = null;
            var agShareTxtPath = Path.Combine(field.DirectoryPath, "agshare.txt");
            if (File.Exists(agShareTxtPath))
            {
                var idText = await File.ReadAllTextAsync(agShareTxtPath);
                if (Guid.TryParse(idText.Trim(), out var parsedId))
                {
                    existingFieldId = parsedId;
                }
            }

            // Build upload input
            var input = new FieldSnapshotInput
            {
                FieldId = existingFieldId,
                FieldName = field.Name,
                Origin = fieldOrigin,
                Boundaries = boundaries,
                Tracks = new List<TrackLineInput>(),
                IsPublic = isPublic,
                Convergence = 0
            };

            var (success, message, fieldId) = await _uploaderService.UploadFieldAsync(
                input,
                _client,
                field.DirectoryPath);

            return (success, message);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private void BtnCancel_Click(object? sender, RoutedEventArgs e)
    {
        Result = null;
        Close(false);
    }
}
