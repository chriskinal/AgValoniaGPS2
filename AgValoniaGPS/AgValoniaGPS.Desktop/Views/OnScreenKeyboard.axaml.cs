using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AgValoniaGPS.Desktop.Views;

/// <summary>
/// Reusable on-screen keyboard for numeric input.
/// Opens as a modal dialog and returns the entered value.
/// </summary>
public partial class OnScreenKeyboard : Window
{
    private string _currentValue = "";
    private bool _hasDecimal = false;
    private readonly double? _minValue;
    private readonly double? _maxValue;
    private readonly int _maxDecimalPlaces;
    private readonly bool _allowNegative;
    private readonly bool _integerOnly;

    /// <summary>
    /// The value entered by the user. Null if cancelled.
    /// </summary>
    public double? ResultValue { get; private set; }

    /// <summary>
    /// Creates a new on-screen keyboard for numeric input.
    /// </summary>
    /// <param name="description">Description shown above the input field</param>
    /// <param name="initialValue">Initial value to display</param>
    /// <param name="minValue">Minimum allowed value (optional)</param>
    /// <param name="maxValue">Maximum allowed value (optional)</param>
    /// <param name="maxDecimalPlaces">Maximum decimal places allowed (default 2)</param>
    /// <param name="allowNegative">Whether negative values are allowed (default true)</param>
    /// <param name="integerOnly">Whether only integer values are allowed (default false)</param>
    public OnScreenKeyboard(
        string description = "Enter value:",
        double? initialValue = null,
        double? minValue = null,
        double? maxValue = null,
        int maxDecimalPlaces = 2,
        bool allowNegative = true,
        bool integerOnly = false)
    {
        InitializeComponent();

        _minValue = minValue;
        _maxValue = maxValue;
        _maxDecimalPlaces = integerOnly ? 0 : maxDecimalPlaces;
        _allowNegative = allowNegative;
        _integerOnly = integerOnly;

        // Set description
        DescriptionLabel.Text = description;

        // Set initial value
        if (initialValue.HasValue)
        {
            _currentValue = integerOnly
                ? ((int)initialValue.Value).ToString()
                : initialValue.Value.ToString(CultureInfo.InvariantCulture);
            _hasDecimal = _currentValue.Contains('.');
        }

        // Hide decimal button if integer only
        if (integerOnly)
        {
            DecimalButton.IsVisible = false;
        }

        // Show range if specified
        UpdateRangeLabel();
        UpdateDisplay();
    }

    // Parameterless constructor for XAML designer
    public OnScreenKeyboard() : this("Enter value:")
    {
    }

    private void UpdateRangeLabel()
    {
        if (_minValue.HasValue && _maxValue.HasValue)
        {
            RangeLabel.Text = $"Range: {_minValue.Value} to {_maxValue.Value}";
        }
        else if (_minValue.HasValue)
        {
            RangeLabel.Text = $"Minimum: {_minValue.Value}";
        }
        else if (_maxValue.HasValue)
        {
            RangeLabel.Text = $"Maximum: {_maxValue.Value}";
        }
    }

    private void UpdateDisplay()
    {
        if (string.IsNullOrEmpty(_currentValue) || _currentValue == "-")
        {
            DisplayText.Text = _currentValue.Length > 0 ? _currentValue : "0";
        }
        else
        {
            DisplayText.Text = _currentValue;
        }
    }

    private void OnDigitClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Content is string digit)
        {
            // Prevent leading zeros (except for "0.")
            if (_currentValue == "0" && digit != ".")
            {
                _currentValue = digit;
            }
            else if (_currentValue == "-0" && digit != ".")
            {
                _currentValue = "-" + digit;
            }
            else
            {
                // Check decimal places limit
                if (_hasDecimal)
                {
                    var parts = _currentValue.Split('.');
                    if (parts.Length > 1 && parts[1].Length >= _maxDecimalPlaces)
                    {
                        return; // Don't add more decimal places
                    }
                }

                _currentValue += digit;
            }

            UpdateDisplay();
        }
    }

    private void OnDecimalClick(object? sender, RoutedEventArgs e)
    {
        if (_integerOnly || _hasDecimal) return;

        if (string.IsNullOrEmpty(_currentValue) || _currentValue == "-")
        {
            _currentValue += "0.";
        }
        else
        {
            _currentValue += ".";
        }

        _hasDecimal = true;
        UpdateDisplay();
    }

    private void OnBackspaceClick(object? sender, RoutedEventArgs e)
    {
        if (_currentValue.Length > 0)
        {
            var removed = _currentValue[^1];
            _currentValue = _currentValue[..^1];

            if (removed == '.')
            {
                _hasDecimal = false;
            }

            UpdateDisplay();
        }
    }

    private void OnClearClick(object? sender, RoutedEventArgs e)
    {
        _currentValue = "";
        _hasDecimal = false;
        UpdateDisplay();
    }

    private void OnNegateClick(object? sender, RoutedEventArgs e)
    {
        if (!_allowNegative) return;

        if (_currentValue.StartsWith('-'))
        {
            _currentValue = _currentValue[1..];
        }
        else if (!string.IsNullOrEmpty(_currentValue))
        {
            _currentValue = "-" + _currentValue;
        }
        else
        {
            _currentValue = "-";
        }

        UpdateDisplay();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        ResultValue = null;
        Close(false);
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        // Parse and validate
        if (string.IsNullOrEmpty(_currentValue) || _currentValue == "-" || _currentValue == ".")
        {
            ResultValue = 0;
        }
        else if (double.TryParse(_currentValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
        {
            // Validate range
            if (_minValue.HasValue && value < _minValue.Value)
            {
                value = _minValue.Value;
            }
            if (_maxValue.HasValue && value > _maxValue.Value)
            {
                value = _maxValue.Value;
            }

            ResultValue = value;
        }
        else
        {
            ResultValue = 0;
        }

        Close(true);
    }

    /// <summary>
    /// Static helper to show the keyboard and get a value.
    /// </summary>
    public static async System.Threading.Tasks.Task<double?> ShowAsync(
        Window owner,
        string description = "Enter value:",
        double? initialValue = null,
        double? minValue = null,
        double? maxValue = null,
        int maxDecimalPlaces = 2,
        bool allowNegative = true,
        bool integerOnly = false)
    {
        var keyboard = new OnScreenKeyboard(
            description,
            initialValue,
            minValue,
            maxValue,
            maxDecimalPlaces,
            allowNegative,
            integerOnly);

        var result = await keyboard.ShowDialog<bool?>(owner);

        return result == true ? keyboard.ResultValue : null;
    }
}
