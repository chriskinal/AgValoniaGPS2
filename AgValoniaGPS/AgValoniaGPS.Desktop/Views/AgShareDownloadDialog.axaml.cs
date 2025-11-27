using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using AgOpenGPS.Core.Services.AgShare;
using AgOpenGPS.Core.Models.AgShare;

namespace AgValoniaGPS.Desktop.Views;

/// <summary>
/// View model for field list items in the download dialog
/// </summary>
public class AgShareFieldListItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double AreaHa { get; set; }
    public string AreaDisplay => $"{AreaHa:F2}";
}

/// <summary>
/// Result from the AgShare download dialog.
/// </summary>
public class AgShareDownloadResult
{
    public Guid FieldId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string FieldPath { get; set; } = string.Empty;
}

public partial class AgShareDownloadDialog : Window
{
    private readonly string _fieldsRootDirectory;
    private readonly AgShareClient _client;
    private readonly AgShareDownloaderService _downloaderService;
    private ObservableCollection<AgShareFieldListItem> _fields = new();
    private AgShareFieldListItem? _selectedField;

    public AgShareDownloadResult? Result { get; private set; }

    public AgShareDownloadDialog() : this(string.Empty, string.Empty, string.Empty)
    {
    }

    public AgShareDownloadDialog(string fieldsRootDirectory, string serverUrl, string apiKey)
    {
        _fieldsRootDirectory = fieldsRootDirectory;

        var url = string.IsNullOrEmpty(serverUrl) ? "https://agshare.agopengps.com" : serverUrl;
        _client = new AgShareClient(url, apiKey);

        _downloaderService = new AgShareDownloaderService(_client);

        InitializeComponent();

        FieldListBox.ItemsSource = _fields;
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        await LoadFieldsAsync();
    }

    private async Task LoadFieldsAsync()
    {
        StatusLabel.Text = "Loading fields from AgShare...";
        _fields.Clear();
        BtnDownload.IsEnabled = false;

        try
        {
            var fields = await _downloaderService.GetOwnFieldsAsync();

            if (fields == null || !fields.Any())
            {
                StatusLabel.Text = "No fields found in your AgShare account";
                return;
            }

            foreach (var field in fields.OrderBy(f => f.Name))
            {
                _fields.Add(new AgShareFieldListItem
                {
                    Id = field.Id,
                    Name = field.Name,
                    AreaHa = field.AreaHa
                });
            }

            StatusLabel.Text = $"Found {_fields.Count} field(s)";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error loading fields: {ex.Message}";
        }
    }

    private void FieldListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _selectedField = FieldListBox.SelectedItem as AgShareFieldListItem;
        BtnDownload.IsEnabled = _selectedField != null;
    }

    private async void BtnRefresh_Click(object? sender, RoutedEventArgs e)
    {
        await LoadFieldsAsync();
    }

    private async void BtnDownloadAll_Click(object? sender, RoutedEventArgs e)
    {
        if (_fields.Count == 0)
        {
            StatusLabel.Text = "No fields to download";
            return;
        }

        bool forceOverwrite = ForceOverwriteCheckBox.IsChecked ?? false;

        StatusLabel.Text = "Downloading all fields...";
        ProgressBar.IsVisible = true;
        ProgressBar.Value = 0;

        try
        {
            var progress = new Progress<int>(percent =>
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    ProgressBar.Value = percent;
                    StatusLabel.Text = $"Downloading... {percent}%";
                });
            });

            var (downloaded, skipped) = await _downloaderService.DownloadAllAsync(
                _fieldsRootDirectory,
                forceOverwrite,
                progress);

            StatusLabel.Text = $"Downloaded {downloaded} field(s), skipped {skipped}";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
        }
        finally
        {
            ProgressBar.IsVisible = false;
        }
    }

    private async void BtnDownload_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedField == null)
            return;

        StatusLabel.Text = $"Downloading '{_selectedField.Name}'...";
        ProgressBar.IsVisible = true;
        ProgressBar.Value = 50;

        try
        {
            var (success, message) = await _downloaderService.DownloadAndSaveAsync(
                _selectedField.Id,
                _fieldsRootDirectory);

            if (success)
            {
                // Field is saved to a directory named after the field
                var fieldPath = System.IO.Path.Combine(_fieldsRootDirectory, _selectedField.Name);

                Result = new AgShareDownloadResult
                {
                    FieldId = _selectedField.Id,
                    FieldName = _selectedField.Name,
                    FieldPath = fieldPath
                };

                Close(true);
            }
            else
            {
                StatusLabel.Text = $"Failed to download field: {message}";
            }
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
        }
        finally
        {
            ProgressBar.IsVisible = false;
        }
    }

    private void BtnCancel_Click(object? sender, RoutedEventArgs e)
    {
        Result = null;
        Close(false);
    }
}
