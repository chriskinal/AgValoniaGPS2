using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AgValoniaGPS.Services;
using AgValoniaGPS.Models;

namespace AgValoniaGPS.Desktop.Views;

public class FieldInfo
{
    public string Name { get; set; } = string.Empty;
    public double Distance { get; set; }
    public double Area { get; set; }
    public string DirectoryPath { get; set; } = string.Empty;
}

public partial class FieldSelectionDialog : Window
{
    private readonly IFieldService _fieldService = null!;
    private string _fieldsRootDirectory = string.Empty;
    private ObservableCollection<FieldInfo> _fields = new();
    public Field? SelectedField { get; private set; }

    // Parameterless constructor for XAML preview (not used at runtime)
    public FieldSelectionDialog() : this(null!, string.Empty)
    {
    }

    public FieldSelectionDialog(IFieldService fieldService, string fieldsRootDirectory)
    {
        _fieldService = fieldService;
        _fieldsRootDirectory = fieldsRootDirectory;

        InitializeComponent();

        // Load fields
        LoadFieldsList();
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
                Distance = 0.0, // TODO: Calculate actual distance from current position
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
                // Calculate area using shoelace formula
                double area = 0;
                var points = boundary.OuterBoundary.Points;
                for (int i = 0; i < points.Count; i++)
                {
                    int j = (i + 1) % points.Count;
                    area += points[i].Easting * points[j].Northing;
                    area -= points[j].Easting * points[i].Northing;
                }
                // Convert to hectares (area is in square meters, 1 hectare = 10000 m²)
                return Math.Abs(area) / 2.0 / 10000.0;
            }
        }
        catch
        {
            // If we can't load boundary, return 0
        }

        return 0.0;
    }

    private void BtnSort_Click(object? sender, RoutedEventArgs e)
    {
        // Toggle sort order between ascending and descending
        var sorted = _fields.OrderBy(f => f.Name).ToList();
        _fields.Clear();
        foreach (var field in sorted)
        {
            _fields.Add(field);
        }
    }

    private async void BtnDeleteField_Click(object? sender, RoutedEventArgs e)
    {
        if (FieldsListBox.SelectedItem is FieldInfo selectedField)
        {
            // Create buttons with click handlers
            var btnYes = new Button { Content = "Yes", Width = 80 };
            var btnNo = new Button { Content = "No", Width = 80 };

            var confirmDialog = new Window
            {
                Title = "Confirm Delete",
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Spacing = 20,
                    Children =
                    {
                        new TextBlock { Text = $"Are you sure you want to delete field '{selectedField.Name}'?" },
                        new StackPanel
                        {
                            Orientation = Avalonia.Layout.Orientation.Horizontal,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                            Spacing = 10,
                            Children =
                            {
                                btnYes,
                                btnNo
                            }
                        }
                    }
                }
            };

            // Wire up button click handlers
            btnYes.Click += (s, e) => confirmDialog.Close(true);
            btnNo.Click += (s, e) => confirmDialog.Close(false);

            var result = await confirmDialog.ShowDialog<bool?>(this);
            if (result == true)
            {
                try
                {
                    _fieldService.DeleteField(selectedField.DirectoryPath);
                    _fields.Remove(selectedField);
                }
                catch (Exception ex)
                {
                    var errorDialog = new Window
                    {
                        Title = "Error",
                        Width = 400,
                        Height = 150,
                        Content = new TextBlock
                        {
                            Text = $"Failed to delete field: {ex.Message}",
                            Margin = new Avalonia.Thickness(20)
                        }
                    };
                    await errorDialog.ShowDialog(this);
                }
            }
        }
    }

    private async void BtnOpen_Click(object? sender, RoutedEventArgs e)
    {
        if (FieldsListBox.SelectedItem is FieldInfo selectedField)
        {
            try
            {
                SelectedField = _fieldService.LoadField(selectedField.DirectoryPath);
                _fieldService.SetActiveField(SelectedField);
                Close(true);
            }
            catch (Exception ex)
            {
                // Show error
                var errorDialog = new Window
                {
                    Title = "Error",
                    Width = 400,
                    Height = 150,
                    Content = new TextBlock
                    {
                        Text = $"Failed to load field: {ex.Message}",
                        Margin = new Avalonia.Thickness(20)
                    }
                };
                await errorDialog.ShowDialog(this);
            }
        }
    }

    private void BtnCancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
