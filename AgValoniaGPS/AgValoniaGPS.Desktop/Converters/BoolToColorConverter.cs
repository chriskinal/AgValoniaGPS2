using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AgValoniaGPS.Desktop.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Brushes.LimeGreen : Brushes.Gray;
        }
        return Brushes.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToStatusConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? "Connected" : "Disconnected";
        }
        return "Unknown";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class FixQualityToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string fixQuality)
        {
            return fixQuality switch
            {
                "RTK Fixed" => Brushes.LimeGreen,      // Green for RTK Fixed
                "RTK Float" => Brushes.Yellow,          // Yellow for RTK Float
                _ => Brushes.Red                        // Red for everything else (No Fix, GPS Fix, DGPS)
            };
        }
        return Brushes.Red;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}