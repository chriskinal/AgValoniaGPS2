using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AgValoniaGPS.Services;
using AgValoniaGPS.Models;

namespace AgValoniaGPS.Desktop.Views;

public partial class FieldSelectionDialog : Window
{
    private readonly IFieldService _fieldService = null!;
    private string _fieldsRootDirectory = string.Empty;
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

        // Set initial directory
        TxtFieldsDirectory.Text = _fieldsRootDirectory;

        // Load fields
        LoadFieldsList();
    }

    private void LoadFieldsList()
    {
        _fieldsRootDirectory = TxtFieldsDirectory.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_fieldsRootDirectory) || !Directory.Exists(_fieldsRootDirectory))
        {
            FieldsList.ItemsSource = new List<string> { "(No fields directory set or directory not found)" };
            return;
        }

        var fields = _fieldService.GetAvailableFields(_fieldsRootDirectory);

        if (fields.Count == 0)
        {
            FieldsList.ItemsSource = new List<string> { "(No fields found)" };
        }
        else
        {
            FieldsList.ItemsSource = fields;
        }
    }

    private async void BtnBrowse_Click(object? sender, RoutedEventArgs e)
    {
        var storageProvider = StorageProvider;
        var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Fields Directory",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            TxtFieldsDirectory.Text = folders[0].Path.LocalPath;
            LoadFieldsList();
        }
    }

    private void BtnRefresh_Click(object? sender, RoutedEventArgs e)
    {
        LoadFieldsList();
    }

    private void FieldsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // Enable/disable buttons based on selection
    }

    private async void BtnNewField_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new NewFieldDialog();
        var result = await dialog.ShowDialog<(bool Success, string FieldName, Position Origin)?>(this);

        if (result.HasValue && result.Value.Success)
        {
            try
            {
                var field = _fieldService.CreateField(
                    _fieldsRootDirectory,
                    result.Value.FieldName,
                    result.Value.Origin);

                SelectedField = field;
                _fieldService.SetActiveField(field);
                Close(true);
            }
            catch (Exception ex)
            {
                // Show error (could use a message box here)
                var errorDialog = new Window
                {
                    Title = "Error",
                    Width = 400,
                    Height = 150,
                    Content = new StackPanel
                    {
                        Margin = new Avalonia.Thickness(20),
                        Children =
                        {
                            new TextBlock { Text = $"Failed to create field: {ex.Message}" }
                        }
                    }
                };
                await errorDialog.ShowDialog(this);
            }
        }
    }

    private async void BtnDeleteField_Click(object? sender, RoutedEventArgs e)
    {
        if (FieldsList.SelectedItem is string fieldName &&
            !fieldName.StartsWith("("))
        {
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
                        new TextBlock { Text = $"Are you sure you want to delete field '{fieldName}'?" },
                        new StackPanel
                        {
                            Orientation = Avalonia.Layout.Orientation.Horizontal,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                            Spacing = 10,
                            Children =
                            {
                                new Button { Content = "Yes", Tag = true },
                                new Button { Content = "No", Tag = false }
                            }
                        }
                    }
                }
            };

            var result = await confirmDialog.ShowDialog<bool?>(this);
            if (result == true)
            {
                try
                {
                    var fieldDirectory = Path.Combine(_fieldsRootDirectory, fieldName);
                    _fieldService.DeleteField(fieldDirectory);
                    LoadFieldsList();
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

    private void BtnOpen_Click(object? sender, RoutedEventArgs e)
    {
        if (FieldsList.SelectedItem is string fieldName &&
            !fieldName.StartsWith("("))
        {
            try
            {
                var fieldDirectory = Path.Combine(_fieldsRootDirectory, fieldName);
                SelectedField = _fieldService.LoadField(fieldDirectory);
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
                errorDialog.ShowDialog(this);
            }
        }
    }

    private void BtnCancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
