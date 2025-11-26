using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AgValoniaGPS.Desktop.Views;

/// <summary>
/// Reusable on-screen keyboard for alphanumeric text input.
/// Opens as a modal dialog and returns the entered string.
/// </summary>
public partial class AlphanumericKeyboard : Window
{
    private string _currentValue = "";
    private bool _isShiftActive = false;
    private readonly int? _maxLength;

    // Letter buttons for shift toggle
    private readonly List<Button> _letterButtons = new();

    /// <summary>
    /// The text entered by the user. Null if cancelled.
    /// </summary>
    public string? ResultText { get; private set; }

    /// <summary>
    /// Creates a new alphanumeric on-screen keyboard.
    /// </summary>
    /// <param name="description">Description shown above the input field</param>
    /// <param name="initialValue">Initial text to display</param>
    /// <param name="maxLength">Maximum text length (optional)</param>
    public AlphanumericKeyboard(
        string description = "Enter text:",
        string? initialValue = null,
        int? maxLength = null)
    {
        InitializeComponent();

        _maxLength = maxLength;

        // Set description
        DescriptionLabel.Text = description;

        // Set initial value
        if (!string.IsNullOrEmpty(initialValue))
        {
            _currentValue = initialValue;
        }

        // Collect letter buttons for shift toggle
        CollectLetterButtons();

        UpdateDisplay();
    }

    // Parameterless constructor for XAML designer
    public AlphanumericKeyboard() : this("Enter text:")
    {
    }

    private void CollectLetterButtons()
    {
        // Find all letter buttons by name
        var letterNames = new[] { "KeyQ", "KeyW", "KeyE", "KeyR", "KeyT", "KeyY", "KeyU", "KeyI", "KeyO", "KeyP",
                                   "KeyA", "KeyS", "KeyD", "KeyF", "KeyG", "KeyH", "KeyJ", "KeyK", "KeyL",
                                   "KeyZ", "KeyX", "KeyC", "KeyV", "KeyB", "KeyN", "KeyM" };

        foreach (var name in letterNames)
        {
            var button = this.FindControl<Button>(name);
            if (button != null)
            {
                _letterButtons.Add(button);
            }
        }
    }

    private void UpdateDisplay()
    {
        DisplayText.Text = string.IsNullOrEmpty(_currentValue) ? "" : _currentValue;
    }

    private void UpdateLetterCase()
    {
        foreach (var button in _letterButtons)
        {
            if (button.Content is string content && content.Length == 1)
            {
                button.Content = _isShiftActive ? content.ToUpper() : content.ToLower();
            }
        }

        // Update shift button appearance
        if (_isShiftActive)
        {
            ShiftButton.Classes.Add("ShiftActive");
            ShiftButton.Classes.Remove("ShiftButton");
        }
        else
        {
            ShiftButton.Classes.Remove("ShiftActive");
            ShiftButton.Classes.Add("ShiftButton");
        }
    }

    private void OnKeyClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Content is string key)
        {
            // Check max length
            if (_maxLength.HasValue && _currentValue.Length >= _maxLength.Value)
            {
                return;
            }

            _currentValue += key;
            UpdateDisplay();

            // Auto-disable shift after typing a letter (like real keyboard)
            if (_isShiftActive && key.Length == 1 && char.IsLetter(key[0]))
            {
                _isShiftActive = false;
                UpdateLetterCase();
            }
        }
    }

    private void OnShiftClick(object? sender, RoutedEventArgs e)
    {
        _isShiftActive = !_isShiftActive;
        UpdateLetterCase();
    }

    private void OnSpaceClick(object? sender, RoutedEventArgs e)
    {
        // Check max length
        if (_maxLength.HasValue && _currentValue.Length >= _maxLength.Value)
        {
            return;
        }

        _currentValue += " ";
        UpdateDisplay();
    }

    private void OnBackspaceClick(object? sender, RoutedEventArgs e)
    {
        if (_currentValue.Length > 0)
        {
            _currentValue = _currentValue[..^1];
            UpdateDisplay();
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        ResultText = null;
        Close(false);
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        ResultText = _currentValue;
        Close(true);
    }

    /// <summary>
    /// Static helper to show the keyboard and get a text value.
    /// </summary>
    public static async System.Threading.Tasks.Task<string?> ShowAsync(
        Window owner,
        string description = "Enter text:",
        string? initialValue = null,
        int? maxLength = null)
    {
        var keyboard = new AlphanumericKeyboard(description, initialValue, maxLength);

        var result = await keyboard.ShowDialog<bool?>(owner);

        return result == true ? keyboard.ResultText : null;
    }
}
