using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AgValoniaGPS.Desktop.Views;

/// <summary>
/// Result from the KML import dialog.
/// </summary>
public class KmlImportResult
{
    public string NewFieldName { get; set; } = string.Empty;
    public string NewFieldPath { get; set; } = string.Empty;
    public double CenterLatitude { get; set; }
    public double CenterLongitude { get; set; }
    public List<(double Latitude, double Longitude)> BoundaryPoints { get; set; } = new();
}

public partial class KmlImportDialog : Window
{
    private readonly string _fieldsRootDirectory;
    private string _currentFieldName = string.Empty;
    private string _kmlFilePath = string.Empty;
    private double _centerLatitude;
    private double _centerLongitude;
    private List<(double Latitude, double Longitude)> _boundaryPoints = new();

    public KmlImportResult? Result { get; private set; }

    public KmlImportDialog() : this(string.Empty)
    {
    }

    public KmlImportDialog(string fieldsRootDirectory)
    {
        _fieldsRootDirectory = fieldsRootDirectory;
        InitializeComponent();
    }

    public async void LoadKmlFile(string kmlFilePath)
    {
        try
        {
            _kmlFilePath = kmlFilePath;
            var fileName = Path.GetFileNameWithoutExtension(kmlFilePath);

            // Set default field name from file name
            _currentFieldName = fileName;
            FieldNameTextBlock.Text = _currentFieldName;
            KmlFileLabel.Text = Path.GetFileName(kmlFilePath);

            // Parse the KML file
            ParseKmlFile(kmlFilePath);

            if (_boundaryPoints.Count >= 3)
            {
                PointCountLabel.Text = _boundaryPoints.Count.ToString();
                CenterCoordsLabel.Text = $"Lat: {_centerLatitude:F6}, Lon: {_centerLongitude:F6}";
                BtnOk.IsEnabled = true;
            }
            else
            {
                PointCountLabel.Text = "Insufficient points";
                CenterCoordsLabel.Text = "";
                BtnOk.IsEnabled = false;
            }
        }
        catch (Exception ex)
        {
            var errorDialog = new Window
            {
                Title = "Error",
                Width = 450,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new TextBlock
                {
                    Text = $"Failed to load KML file:\n{ex.Message}",
                    Margin = new Avalonia.Thickness(20),
                    Foreground = Avalonia.Media.Brushes.Black,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                }
            };
            await errorDialog.ShowDialog(this);
            Close(false);
        }
    }

    private void ParseKmlFile(string filePath)
    {
        _boundaryPoints.Clear();
        string? coordinates = null;
        int startIndex;

        using var reader = new StreamReader(filePath);
        while (!reader.EndOfStream)
        {
            string? line = reader.ReadLine();
            if (line == null) continue;

            startIndex = line.IndexOf("<coordinates>");

            if (startIndex != -1)
            {
                // Found start of coordinates block
                while (true)
                {
                    int endIndex = line.IndexOf("</coordinates>");

                    if (endIndex == -1)
                    {
                        // No closing tag on this line - add content and read more
                        if (startIndex == -1)
                            coordinates += " " + line.Substring(0);
                        else
                            coordinates += line.Substring(startIndex + 13);
                    }
                    else
                    {
                        // Found closing tag
                        if (startIndex == -1)
                            coordinates += " " + line.Substring(0, endIndex);
                        else
                            coordinates += line.Substring(startIndex + 13, endIndex - (startIndex + 13));
                        break;
                    }

                    line = reader.ReadLine();
                    if (line == null) break;
                    line = line.Trim();
                    startIndex = -1;
                }

                if (coordinates == null) continue;

                // Parse coordinate pairs: format is "lon,lat,alt lon,lat,alt ..."
                char[] delimiterChars = { ' ', '\t', '\r', '\n' };
                string[] numberSets = coordinates.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);

                if (numberSets.Length >= 3)
                {
                    double sumLat = 0, sumLon = 0;
                    int validPoints = 0;

                    foreach (string item in numberSets)
                    {
                        if (item.Length < 3) continue;

                        string[] parts = item.Split(',');
                        if (parts.Length >= 2 &&
                            double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double lon) &&
                            double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double lat))
                        {
                            _boundaryPoints.Add((lat, lon));
                            sumLat += lat;
                            sumLon += lon;
                            validPoints++;
                        }
                    }

                    if (validPoints > 0)
                    {
                        _centerLatitude = sumLat / validPoints;
                        _centerLongitude = sumLon / validPoints;
                    }
                }

                // Only parse first coordinate block (outer boundary)
                break;
            }
        }
    }

    private async void FieldNameBorder_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
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

    private void BtnAppendDate_Click(object? sender, RoutedEventArgs e)
    {
        var dateStr = DateTime.Now.ToString("yyyy-MM-dd");
        _currentFieldName = _currentFieldName + " " + dateStr;
        FieldNameTextBlock.Text = _currentFieldName;
    }

    private void BtnAppendTime_Click(object? sender, RoutedEventArgs e)
    {
        var timeStr = DateTime.Now.ToString("HH-mm");
        _currentFieldName = _currentFieldName + " " + timeStr;
        FieldNameTextBlock.Text = _currentFieldName;
    }

    private void BtnCancel_Click(object? sender, RoutedEventArgs e)
    {
        Result = null;
        Close(false);
    }

    private async void BtnOk_Click(object? sender, RoutedEventArgs e)
    {
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
        if (Directory.Exists(newFieldPath))
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

        if (_boundaryPoints.Count < 3)
        {
            var errorDialog = new Window
            {
                Title = "Error",
                Width = 400,
                Height = 120,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new TextBlock
                {
                    Text = "KML file must contain at least 3 boundary points.",
                    Margin = new Avalonia.Thickness(20),
                    Foreground = Avalonia.Media.Brushes.Black
                }
            };
            await errorDialog.ShowDialog(this);
            return;
        }

        Result = new KmlImportResult
        {
            NewFieldName = newFieldName,
            NewFieldPath = newFieldPath,
            CenterLatitude = _centerLatitude,
            CenterLongitude = _centerLongitude,
            BoundaryPoints = new List<(double, double)>(_boundaryPoints)
        };

        Close(true);
    }
}
