using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using AgValoniaGPS.Models;

namespace AgValoniaGPS.Desktop.Views;

public partial class NewFieldDialog : Window
{
    private readonly Position _currentPosition;

    public NewFieldDialog(Position currentPosition)
    {
        _currentPosition = currentPosition;
        InitializeComponent();

        // Display current GPS position
        TxtCurrentPosition.Text = $"Latitude: {currentPosition.Latitude:F6}, Longitude: {currentPosition.Longitude:F6}";
    }

    // Parameterless constructor for designer
    public NewFieldDialog() : this(new Position { Latitude = 0, Longitude = 0 })
    {
    }

    private void BtnCreate_Click(object? sender, RoutedEventArgs e)
    {
        var fieldName = TxtFieldName.Text?.Trim();

        // Validate field name
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            ShowError("Please enter a field name.");
            return;
        }

        // Use the current GPS position as the field origin
        Close((Success: true, FieldName: fieldName, Origin: _currentPosition));
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
