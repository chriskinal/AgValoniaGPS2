using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AgOpenGPS.Core.Models;
using AgOpenGPS.Core.Models.IsoXml;
using AgOpenGPS.Core.Services.IsoXml;
using AgValoniaGPS.Models;

namespace AgValoniaGPS.Desktop.Views;

/// <summary>
/// Result from the ISO-XML import dialog.
/// </summary>
public class IsoXmlImportResult
{
    public string NewFieldName { get; set; } = string.Empty;
    public string NewFieldPath { get; set; } = string.Empty;
    public Wgs84 Origin { get; set; } = new Wgs84();
    public List<IsoXmlBoundary> Boundaries { get; set; } = new();
    public List<AgOpenGPS.Core.Models.Base.Vec3> Headland { get; set; } = new();
    public List<IsoXmlTrack> GuidanceLines { get; set; } = new();
}

/// <summary>
/// Tree node data for ISO-XML fields
/// </summary>
public class IsoXmlTreeNode
{
    public string DisplayText { get; set; } = string.Empty;
    public int FieldIndex { get; set; } = -1;
    public bool IsField { get; set; }
    public List<IsoXmlTreeNode> Children { get; set; } = new();
}

public partial class IsoXmlImportDialog : Window
{
    private readonly string _fieldsRootDirectory;
    private XmlDocument? _xmlDocument;
    private XmlNodeList? _pfdNodes;
    private int _selectedFieldIndex = -1;
    private string _currentFieldName = string.Empty;

    public IsoXmlImportResult? Result { get; private set; }

    public IsoXmlImportDialog() : this(string.Empty)
    {
    }

    public IsoXmlImportDialog(string fieldsRootDirectory)
    {
        _fieldsRootDirectory = fieldsRootDirectory;
        InitializeComponent();
    }

    public async void LoadXmlFile(string xmlFilePath)
    {
        try
        {
            _xmlDocument = new XmlDocument { PreserveWhitespace = false };
            _xmlDocument.Load(xmlFilePath);

            _pfdNodes = _xmlDocument.GetElementsByTagName("PFD");
            PopulateTreeView();
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
                    Text = $"Failed to load ISO-XML file:\n{ex.Message}",
                    Margin = new Avalonia.Thickness(20),
                    Foreground = Avalonia.Media.Brushes.Black,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                }
            };
            await errorDialog.ShowDialog(this);
            Close(false);
        }
    }

    private void PopulateTreeView()
    {
        if (_pfdNodes == null) return;

        var treeItems = new List<TreeViewItem>();
        int index = 0;

        foreach (XmlNode nodePFD in _pfdNodes)
        {
            // Get field name and area
            string fieldName = nodePFD.Attributes?["C"]?.Value ?? "Unnamed";
            double areaHa = GetFieldArea(nodePFD);
            string fieldLabel = $"{fieldName} - {areaHa:F2} Ha";

            var fieldItem = new TreeViewItem
            {
                Header = fieldLabel,
                Tag = index,
                IsExpanded = true
            };

            // Add child nodes for guidance lines
            AddGuidanceLineNodes(nodePFD, fieldItem);

            treeItems.Add(fieldItem);
            index++;
        }

        FieldTreeView.ItemsSource = treeItems;

        // Pre-select first field if available
        if (treeItems.Count > 0)
        {
            treeItems[0].IsSelected = true;
        }
    }

    private double GetFieldArea(XmlNode nodePFD)
    {
        // Try to get area from attribute D
        if (double.TryParse(nodePFD.Attributes?["D"]?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double areaRaw) && areaRaw >= 1)
        {
            return areaRaw * 0.0001; // Convert m² to hectares
        }

        // Estimate from boundary points using shoelace formula
        return EstimateAreaFromPln(nodePFD);
    }

    private double EstimateAreaFromPln(XmlNode nodePfd)
    {
        foreach (XmlNode nodePln in nodePfd.SelectNodes("PLN")!)
        {
            if (nodePln.Attributes?["A"]?.Value != "1") continue;

            var lsg = nodePln.SelectSingleNode("LSG[@A='1']");
            if (lsg == null) continue;

            var pts = lsg.SelectNodes("PNT");
            if (pts == null || pts.Count < 3) continue;

            var coords = new List<(double lat, double lon)>();
            foreach (XmlNode pnt in pts)
            {
                if (double.TryParse(pnt.Attributes?["C"]?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double lat) &&
                    double.TryParse(pnt.Attributes?["D"]?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double lon))
                {
                    coords.Add((lat, lon));
                }
            }

            if (coords.Count < 3) continue;

            // Rough area calculation (simplified for display purposes)
            // Convert to approximate meters and use shoelace
            double avgLat = 0;
            foreach (var c in coords) avgLat += c.lat;
            avgLat /= coords.Count;

            double metersPerDegreeLat = 111132.92;
            double metersPerDegreeLon = 111412.84 * Math.Cos(avgLat * Math.PI / 180);

            var vecs = new List<(double x, double y)>();
            foreach (var c in coords)
            {
                vecs.Add((c.lon * metersPerDegreeLon, c.lat * metersPerDegreeLat));
            }

            double area = 0;
            for (int i = 0, j = vecs.Count - 1; i < vecs.Count; j = i++)
            {
                area += (vecs[j].x + vecs[i].x) * (vecs[j].y - vecs[i].y);
            }
            return Math.Abs(area / 2.0) * 0.0001; // Convert m² to hectares
        }

        return 0.0;
    }

    private void AddGuidanceLineNodes(XmlNode nodePFD, TreeViewItem parentItem)
    {
        var children = new List<TreeViewItem>();

        // Parse GGP → GPN → LSG structure (v3-style)
        foreach (XmlNode nodeGgp in nodePFD.SelectNodes("GGP")!)
        {
            var gpn = nodeGgp.SelectSingleNode("GPN");
            if (gpn == null) continue;

            string name = nodeGgp.Attributes?["B"]?.Value ?? gpn.Attributes?["B"]?.Value ?? "Unnamed";
            string type = gpn.Attributes?["C"]?.Value ?? "";

            string prefix = type switch
            {
                "1" => "AB: ",
                "2" => "A+: ",
                "3" => "Curve: ",
                _ => ""
            };

            if (!string.IsNullOrEmpty(prefix))
            {
                children.Add(new TreeViewItem { Header = prefix + name });
            }
        }

        // Parse PLN nodes (v2-style guidance lines)
        foreach (XmlNode nodePart in nodePFD.ChildNodes)
        {
            if (nodePart.Name != "PLN") continue;

            string name = nodePart.Attributes?["B"]?.Value ?? "Unnamed";
            string type = nodePart.Attributes?["C"]?.Value ?? "";
            var pnts = nodePart.SelectNodes("PNT");
            int pointCount = pnts?.Count ?? 0;

            string? label = null;
            if (type == "1" && pointCount == 2) label = "AB: " + name;
            else if (type == "2" && pointCount == 1) label = "A+: " + name;
            else if (type == "3" && pointCount > 2) label = "Curve: " + name;
            else if (type == "4" && pointCount > 0) label = "Pivot: " + name;
            else if (type == "5" && pointCount > 0) label = "Spiral: " + name;

            if (label != null)
            {
                children.Add(new TreeViewItem { Header = label });
            }
        }

        // Parse direct LSG nodes (v3 standalone guidance)
        foreach (XmlNode nodePart in nodePFD.ChildNodes)
        {
            if (nodePart.Name != "LSG" || nodePart.Attributes?["A"]?.Value != "5") continue;

            string name = nodePart.Attributes?["B"]?.Value ?? "Unnamed";
            var pnts = nodePart.SelectNodes("PNT");
            int pointCount = pnts?.Count ?? 0;

            string? label = null;
            if (pointCount == 2) label = "AB: " + name;
            else if (pointCount > 2) label = "Curve: " + name;

            if (label != null)
            {
                children.Add(new TreeViewItem { Header = label });
            }
        }

        parentItem.ItemsSource = children;
    }

    private void FieldTreeView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (FieldTreeView.SelectedItem is TreeViewItem selectedItem)
        {
            // Get the field index (either from this item or its parent)
            int fieldIndex;
            if (selectedItem.Tag is int idx)
            {
                fieldIndex = idx;
            }
            else if (selectedItem.Parent is TreeViewItem parentItem && parentItem.Tag is int parentIdx)
            {
                fieldIndex = parentIdx;
            }
            else
            {
                return;
            }

            _selectedFieldIndex = fieldIndex;

            if (_pfdNodes != null && fieldIndex >= 0 && fieldIndex < _pfdNodes.Count)
            {
                string fieldName = _pfdNodes[fieldIndex].Attributes?["C"]?.Value ?? "Unnamed";
                SelectedFieldLabel.Text = fieldName;
                _currentFieldName = fieldName;
                FieldNameTextBlock.Text = _currentFieldName;
                BtnOk.IsEnabled = true;
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
        if (_pfdNodes == null || _selectedFieldIndex < 0 || _selectedFieldIndex >= _pfdNodes.Count)
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

        // Parse the selected field using Core parser
        var fieldParts = _pfdNodes[_selectedFieldIndex].ChildNodes;

        // Extract origin first
        if (!IsoXmlParserHelpers.TryExtractOrigin(fieldParts, out Wgs84 origin))
        {
            var errorDialog = new Window
            {
                Title = "Error",
                Width = 450,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new TextBlock
                {
                    Text = "Cannot calculate center of field.\nMissing Outer Boundary or AB line.",
                    Margin = new Avalonia.Thickness(20),
                    Foreground = Avalonia.Media.Brushes.Black
                }
            };
            await errorDialog.ShowDialog(this);
            return;
        }

        // Create LocalPlane for coordinate conversion
        var sharedProps = new SharedFieldProperties();
        var localPlane = new LocalPlane(origin, sharedProps);

        // Parse boundaries, headland, and guidance lines
        var boundaries = IsoXmlParserHelpers.ParseBoundaries(fieldParts, localPlane);
        var headland = IsoXmlParserHelpers.ParseHeadland(fieldParts, localPlane);
        var guidanceLines = IsoXmlParserHelpers.ParseAllGuidanceLines(fieldParts, localPlane);

        Result = new IsoXmlImportResult
        {
            NewFieldName = newFieldName,
            NewFieldPath = newFieldPath,
            Origin = origin,
            Boundaries = boundaries,
            Headland = headland,
            GuidanceLines = guidanceLines
        };

        Close(true);
    }
}
