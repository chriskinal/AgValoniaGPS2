using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AgValoniaGPS.Services;

namespace AgValoniaGPS.Desktop.Views;

/// <summary>
/// Options for what data to copy from the existing field to the new field.
/// </summary>
public class FieldCopyOptions
{
    public bool IncludeFlags { get; set; } = true;
    public bool IncludeMapping { get; set; } = true;
    public bool IncludeHeadland { get; set; } = true;
    public bool IncludeLines { get; set; } = true;
}

/// <summary>
/// Result from the From Existing Field dialog.
/// </summary>
public class FromExistingFieldResult
{
    public string SourceFieldPath { get; set; } = string.Empty;
    public string NewFieldName { get; set; } = string.Empty;
    public FieldCopyOptions CopyOptions { get; set; } = new();
}

public partial class FromExistingFieldDialog : Window
{
    private readonly IFieldService _fieldService;
    private readonly string _fieldsRootDirectory;
    private readonly string _vehicleName;
    private readonly ObservableCollection<FieldInfo> _fields = new();
    private readonly FieldCopyOptions _copyOptions = new();
    private string _currentFieldName = string.Empty;

    public FromExistingFieldResult? Result { get; private set; }

    public FromExistingFieldDialog() : this(null!, string.Empty, string.Empty)
    {
    }

    public FromExistingFieldDialog(IFieldService fieldService, string fieldsRootDirectory, string vehicleName)
    {
        _fieldService = fieldService;
        _fieldsRootDirectory = fieldsRootDirectory;
        _vehicleName = vehicleName;

        InitializeComponent();

        LoadFieldsList();
        UpdateToggleButtonStates();

        // Pre-select first field if available
        if (_fields.Count > 0)
        {
            FieldsListBox.SelectedIndex = 0;
            UpdateFieldNameFromSelection();
        }

        // Update name when selection changes
        FieldsListBox.SelectionChanged += (s, e) => UpdateFieldNameFromSelection();
    }

    private void LoadFieldsList()
    {
        _fields.Clear();

        if (string.IsNullOrWhiteSpace(_fieldsRootDirectory) || !Directory.Exists(_fieldsRootDirectory))
        {
            FieldsListBox.ItemsSource = _fields;
            return;
        }

        var fieldNames = _fieldService.GetAvailableFields(_fieldsRootDirectory);

        foreach (var fieldName in fieldNames)
        {
            var fieldDirectory = Path.Combine(_fieldsRootDirectory, fieldName);
            var fieldInfo = new FieldInfo
            {
                Name = fieldName,
                Distance = 0.0,
                Area = CalculateFieldArea(fieldDirectory),
                DirectoryPath = fieldDirectory
            };
            _fields.Add(fieldInfo);
        }

        FieldsListBox.ItemsSource = _fields;
    }

    private double CalculateFieldArea(string fieldDirectory)
    {
        try
        {
            var boundaryService = new BoundaryFileService();
            var boundary = boundaryService.LoadBoundary(fieldDirectory);

            if (boundary?.OuterBoundary?.Points != null && boundary.OuterBoundary.Points.Count > 2)
            {
                double area = 0;
                var points = boundary.OuterBoundary.Points;
                for (int i = 0; i < points.Count; i++)
                {
                    int j = (i + 1) % points.Count;
                    area += points[i].Easting * points[j].Northing;
                    area -= points[j].Easting * points[i].Northing;
                }
                return Math.Abs(area) / 2.0 / 10000.0;
            }
        }
        catch
        {
        }

        return 0.0;
    }

    private void UpdateFieldNameFromSelection()
    {
        if (FieldsListBox.SelectedItem is FieldInfo selectedField)
        {
            _currentFieldName = selectedField.Name;
            FieldNameTextBlock.Text = _currentFieldName;
        }
    }

    private void UpdateToggleButtonStates()
    {
        UpdateToggleButton(BtnToggleFlags, _copyOptions.IncludeFlags);
        UpdateToggleButton(BtnToggleMapping, _copyOptions.IncludeMapping);
        UpdateToggleButton(BtnToggleHeadland, _copyOptions.IncludeHeadland);
        UpdateToggleButton(BtnToggleLines, _copyOptions.IncludeLines);
    }

    private void UpdateToggleButton(Button button, bool isOn)
    {
        if (isOn)
        {
            if (!button.Classes.Contains("ToggleOn"))
                button.Classes.Add("ToggleOn");
        }
        else
        {
            button.Classes.Remove("ToggleOn");
        }
    }

    private async void FieldNameBorder_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Open alphanumeric keyboard
        var result = await AlphanumericKeyboard.ShowAsync(
            this,
            description: "Enter field name:",
            initialValue: _currentFieldName,
            maxLength: 100);

        if (result != null)
        {
            _currentFieldName = result;
            FieldNameTextBlock.Text = _currentFieldName;
        }
    }

    private void BtnBackspace_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentFieldName.Length > 0)
        {
            _currentFieldName = _currentFieldName.Substring(0, _currentFieldName.Length - 1);
            FieldNameTextBlock.Text = _currentFieldName;
        }
    }

    private void BtnAppendVehicle_Click(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_vehicleName))
        {
            _currentFieldName = _currentFieldName + " " + _vehicleName;
            FieldNameTextBlock.Text = _currentFieldName;
        }
    }

    private void BtnAppendDate_Click(object? sender, RoutedEventArgs e)
    {
        var dateStr = DateTime.Now.ToString("yyyy-MMM-dd");
        _currentFieldName = _currentFieldName + " " + dateStr;
        FieldNameTextBlock.Text = _currentFieldName;
    }

    private void BtnAppendTime_Click(object? sender, RoutedEventArgs e)
    {
        var timeStr = DateTime.Now.ToString("HH-mm");
        _currentFieldName = _currentFieldName + " " + timeStr;
        FieldNameTextBlock.Text = _currentFieldName;
    }

    private void BtnToggleFlags_Click(object? sender, RoutedEventArgs e)
    {
        _copyOptions.IncludeFlags = !_copyOptions.IncludeFlags;
        UpdateToggleButton(BtnToggleFlags, _copyOptions.IncludeFlags);
    }

    private void BtnToggleMapping_Click(object? sender, RoutedEventArgs e)
    {
        _copyOptions.IncludeMapping = !_copyOptions.IncludeMapping;
        UpdateToggleButton(BtnToggleMapping, _copyOptions.IncludeMapping);
    }

    private void BtnToggleHeadland_Click(object? sender, RoutedEventArgs e)
    {
        _copyOptions.IncludeHeadland = !_copyOptions.IncludeHeadland;
        UpdateToggleButton(BtnToggleHeadland, _copyOptions.IncludeHeadland);
    }

    private void BtnToggleLines_Click(object? sender, RoutedEventArgs e)
    {
        _copyOptions.IncludeLines = !_copyOptions.IncludeLines;
        UpdateToggleButton(BtnToggleLines, _copyOptions.IncludeLines);
    }

    private void BtnCancel_Click(object? sender, RoutedEventArgs e)
    {
        Result = null;
        Close(false);
    }

    private async void BtnOk_Click(object? sender, RoutedEventArgs e)
    {
        if (FieldsListBox.SelectedItem is not FieldInfo selectedField)
        {
            return;
        }

        var newFieldName = _currentFieldName.Trim();

        if (string.IsNullOrWhiteSpace(newFieldName))
        {
            var errorDialog = new Window
            {
                Title = "Error",
                Width = 350,
                Height = 120,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new TextBlock
                {
                    Text = "Please enter a field name.",
                    Margin = new Avalonia.Thickness(20),
                    Foreground = Avalonia.Media.Brushes.Black
                }
            };
            await errorDialog.ShowDialog(this);
            return;
        }

        // Check if field already exists
        var newFieldPath = Path.Combine(_fieldsRootDirectory, newFieldName);
        if (Directory.Exists(newFieldPath) && newFieldName != selectedField.Name)
        {
            var errorDialog = new Window
            {
                Title = "Error",
                Width = 400,
                Height = 120,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new TextBlock
                {
                    Text = $"A field named '{newFieldName}' already exists.",
                    Margin = new Avalonia.Thickness(20),
                    Foreground = Avalonia.Media.Brushes.Black
                }
            };
            await errorDialog.ShowDialog(this);
            return;
        }

        Result = new FromExistingFieldResult
        {
            SourceFieldPath = selectedField.DirectoryPath,
            NewFieldName = newFieldName,
            CopyOptions = _copyOptions
        };

        Close(true);
    }
}
