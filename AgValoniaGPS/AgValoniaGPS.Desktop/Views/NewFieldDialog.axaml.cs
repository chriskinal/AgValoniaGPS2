using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using AgValoniaGPS.Models;

namespace AgValoniaGPS.Desktop.Views;

public partial class NewFieldDialog : Window
{
    public NewFieldDialog()
    {
        InitializeComponent();
    }

    private void BtnCreate_Click(object? sender, RoutedEventArgs e)
    {
        var fieldName = TxtFieldName.Text?.Trim();
        var latText = TxtLatitude.Text?.Trim();
        var lonText = TxtLongitude.Text?.Trim();

        // Validate inputs
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            ShowError("Please enter a field name.");
            return;
        }

        if (!double.TryParse(latText, NumberStyles.Float, CultureInfo.InvariantCulture, out double latitude))
        {
            ShowError("Please enter a valid latitude.");
            return;
        }

        if (!double.TryParse(lonText, NumberStyles.Float, CultureInfo.InvariantCulture, out double longitude))
        {
            ShowError("Please enter a valid longitude.");
            return;
        }

        // Validate latitude/longitude ranges
        if (latitude < -90 || latitude > 90)
        {
            ShowError("Latitude must be between -90 and 90 degrees.");
            return;
        }

        if (longitude < -180 || longitude > 180)
        {
            ShowError("Longitude must be between -180 and 180 degrees.");
            return;
        }

        var origin = new Position
        {
            Latitude = latitude,
            Longitude = longitude,
            Altitude = 0
        };

        Close((Success: true, FieldName: fieldName, Origin: origin));
    }

    private void BtnCancel_Click(object? sender, RoutedEventArgs e)
    {
        Close((Success: false, FieldName: string.Empty, Origin: new Position()));
    }

    private async void ShowError(string message)
    {
        var okButton = new Button
        {
            Content = "OK",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        var errorDialog = new Window
        {
            Title = "Validation Error",
            Width = 350,
            Height = 120,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new SolidColorBrush(Color.FromRgb(169, 50, 38)),
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(20),
                Spacing = 15,
                Children =
                {
                    new TextBlock
                    {
                        Text = message,
                        Foreground = new SolidColorBrush(Colors.White),
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    },
                    okButton
                }
            }
        };

        okButton.Click += (s, e) => errorDialog.Close();

        await errorDialog.ShowDialog(this);
    }
}
