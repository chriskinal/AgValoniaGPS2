using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using AgOpenGPS.Core.Services.AgShare;

namespace AgValoniaGPS.Desktop.Views;

/// <summary>
/// Result from the AgShare settings dialog.
/// </summary>
public class AgShareSettingsResult
{
    public string ServerUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}

public partial class AgShareSettingsDialog : Window
{
    private string _currentServerUrl = "https://agshare.agopengps.com";
    private string _currentApiKey = string.Empty;

    public AgShareSettingsResult? Result { get; private set; }

    public AgShareSettingsDialog()
    {
        InitializeComponent();
    }

    public void LoadSettings(string serverUrl, string apiKey, bool enabled)
    {
        _currentServerUrl = string.IsNullOrEmpty(serverUrl) ? "https://agshare.agopengps.com" : serverUrl;
        _currentApiKey = apiKey ?? string.Empty;

        ServerUrlTextBlock.Text = _currentServerUrl;
        ApiKeyTextBlock.Text = MaskApiKey(_currentApiKey);
        EnabledCheckBox.IsChecked = enabled;
    }

    private string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            return "(not set)";
        if (apiKey.Length <= 8)
            return new string('*', apiKey.Length);
        return apiKey.Substring(0, 4) + new string('*', apiKey.Length - 8) + apiKey.Substring(apiKey.Length - 4);
    }

    private async void ServerUrlBorder_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var result = await AlphanumericKeyboard.ShowAsync(
            this,
            description: "Enter server URL:",
            initialValue: _currentServerUrl,
            maxLength: 200);

        if (result != null)
        {
            _currentServerUrl = result;
            ServerUrlTextBlock.Text = _currentServerUrl;
        }
    }

    private async void ApiKeyBorder_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var result = await AlphanumericKeyboard.ShowAsync(
            this,
            description: "Enter API key:",
            initialValue: _currentApiKey,
            maxLength: 100);

        if (result != null)
        {
            _currentApiKey = result;
            ApiKeyTextBlock.Text = MaskApiKey(_currentApiKey);
        }
    }

    private async void BtnPasteApiKey_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                var text = await clipboard.GetTextAsync();
                if (!string.IsNullOrEmpty(text))
                {
                    _currentApiKey = text.Trim();
                    ApiKeyTextBlock.Text = MaskApiKey(_currentApiKey);
                    ConnectionStatusLabel.Text = "API key pasted from clipboard";
                    ConnectionStatusLabel.Foreground = new SolidColorBrush(Color.Parse("#BDC3C7"));
                }
            }
        }
        catch (Exception ex)
        {
            ConnectionStatusLabel.Text = "Failed to paste: " + ex.Message;
            ConnectionStatusLabel.Foreground = new SolidColorBrush(Color.Parse("#E74C3C"));
        }
    }

    private void BtnBackspaceServer_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentServerUrl.Length > 0)
        {
            _currentServerUrl = _currentServerUrl.Substring(0, _currentServerUrl.Length - 1);
            ServerUrlTextBlock.Text = _currentServerUrl;
        }
    }

    private void BtnBackspaceApiKey_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentApiKey.Length > 0)
        {
            _currentApiKey = _currentApiKey.Substring(0, _currentApiKey.Length - 1);
            ApiKeyTextBlock.Text = MaskApiKey(_currentApiKey);
        }
    }

    private async void BtnTestConnection_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentApiKey))
        {
            ConnectionStatusLabel.Text = "Please enter an API key first";
            ConnectionStatusLabel.Foreground = new SolidColorBrush(Color.Parse("#E74C3C"));
            return;
        }

        ConnectionStatusLabel.Text = "Testing connection...";
        ConnectionStatusLabel.Foreground = new SolidColorBrush(Color.Parse("#BDC3C7"));

        try
        {
            var client = new AgShareClient(_currentServerUrl, _currentApiKey);

            var (ok, message) = await client.CheckApiAsync();

            if (ok)
            {
                ConnectionStatusLabel.Text = "Connection successful!";
                ConnectionStatusLabel.Foreground = new SolidColorBrush(Color.Parse("#27AE60"));
            }
            else
            {
                ConnectionStatusLabel.Text = $"Connection failed: {message}";
                ConnectionStatusLabel.Foreground = new SolidColorBrush(Color.Parse("#E74C3C"));
            }
        }
        catch (Exception ex)
        {
            ConnectionStatusLabel.Text = "Error: " + ex.Message;
            ConnectionStatusLabel.Foreground = new SolidColorBrush(Color.Parse("#E74C3C"));
        }
    }

    private void BtnCancel_Click(object? sender, RoutedEventArgs e)
    {
        Result = null;
        Close(false);
    }

    private void BtnOk_Click(object? sender, RoutedEventArgs e)
    {
        Result = new AgShareSettingsResult
        {
            ServerUrl = _currentServerUrl,
            ApiKey = _currentApiKey,
            Enabled = EnabledCheckBox.IsChecked ?? false
        };

        Close(true);
    }
}
