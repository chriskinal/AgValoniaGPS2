namespace AgValoniaGPS.Models;

/// <summary>
/// Represents a georeferenced background image (satellite/aerial photo)
/// Metadata from BackPic.Txt, actual image in BackPic.png
/// </summary>
public class BackgroundImage
{
    /// <summary>
    /// Whether the background image is enabled
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Maximum easting (right edge) in meters
    /// </summary>
    public double MaxEasting { get; set; }

    /// <summary>
    /// Minimum easting (left edge) in meters
    /// </summary>
    public double MinEasting { get; set; }

    /// <summary>
    /// Maximum northing (top edge) in meters
    /// </summary>
    public double MaxNorthing { get; set; }

    /// <summary>
    /// Minimum northing (bottom edge) in meters
    /// </summary>
    public double MinNorthing { get; set; }

    /// <summary>
    /// Width in meters
    /// </summary>
    public double Width => MaxEasting - MinEasting;

    /// <summary>
    /// Height in meters
    /// </summary>
    public double Height => MaxNorthing - MinNorthing;

    /// <summary>
    /// Path to the image file (BackPic.png)
    /// </summary>
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>
    /// Check if bounds are valid
    /// </summary>
    public bool IsValid => Width > 0 && Height > 0;
}
