using System;
using System.Globalization;
using AgOpenGPS.Core.Interfaces.Services;
using AgOpenGPS.Core.Models.GPS;

namespace AgValoniaGPS.Services;

/// <summary>
/// NMEA sentence parser for PANDA and PAOGI formats
/// Based on AgIO NMEA parser
/// </summary>
public class NmeaParserService
{
    private readonly IGpsService _gpsService;

    public event EventHandler? ImuDataReceived;

    public NmeaParserService(IGpsService gpsService)
    {
        _gpsService = gpsService;
    }

    /// <summary>
    /// Parse NMEA sentence and update GPS data
    /// Supports $PANDA and $PAOGI formats
    /// </summary>
    public void ParseSentence(string sentence)
    {
        if (string.IsNullOrWhiteSpace(sentence)) return;

        // Validate checksum
        if (!ValidateChecksum(sentence)) return;

        // Remove checksum
        int asterisk = sentence.IndexOf("*", StringComparison.Ordinal);
        if (asterisk > 0)
        {
            sentence = sentence.Substring(0, asterisk);
        }

        string[] words = sentence.Split(',');
        if (words.Length < 3) return;

        if (words[0] == "$PANDA" && words.Length > 14)
        {
            ParsePANDA(words);
        }
        else if (words[0] == "$PAOGI" && words.Length > 14)
        {
            ParsePAOGI(words);
        }
    }

    private void ParsePANDA(string[] words)
    {
        /*
        $PANDA
        (1) Time of fix
        (2,3) 4807.038,N Latitude 48 deg 07.038' N
        (4,5) 01131.000,E Longitude 11 deg 31.000' E
        (6) Fix quality (0-8)
        (7) Number of satellites
        (8) HDOP
        (9) Altitude in meters
        (10) Age of differential
        (11) Speed in knots
        (12) Heading in degrees
        (13) Roll in degrees
        (14) Pitch in degrees
        (15) Yaw rate in degrees/second
        */

        var gpsData = new GpsData();

        try
        {
            // Parse latitude
            if (!string.IsNullOrEmpty(words[2]) && !string.IsNullOrEmpty(words[3]))
            {
                double latitude = ParseLatitude(words[2], words[3]);
                gpsData.CurrentPosition = gpsData.CurrentPosition with { Latitude = latitude };
            }

            // Parse longitude
            if (!string.IsNullOrEmpty(words[4]) && !string.IsNullOrEmpty(words[5]))
            {
                double longitude = ParseLongitude(words[4], words[5]);
                gpsData.CurrentPosition = gpsData.CurrentPosition with { Longitude = longitude };
            }

            // Fix quality
            if (byte.TryParse(words[6], NumberStyles.Float, CultureInfo.InvariantCulture, out byte fixQuality))
            {
                gpsData.FixQuality = fixQuality;
            }

            // Satellites
            if (int.TryParse(words[7], NumberStyles.Float, CultureInfo.InvariantCulture, out int satellites))
            {
                gpsData.SatellitesInUse = satellites;
            }

            // HDOP
            if (double.TryParse(words[8], NumberStyles.Float, CultureInfo.InvariantCulture, out double hdop))
            {
                gpsData.Hdop = hdop;
            }

            // Altitude
            if (float.TryParse(words[9], NumberStyles.Float, CultureInfo.InvariantCulture, out float altitude))
            {
                gpsData.CurrentPosition = gpsData.CurrentPosition with { Altitude = altitude };
            }

            // Age of differential
            if (double.TryParse(words[10], NumberStyles.Float, CultureInfo.InvariantCulture, out double age))
            {
                gpsData.DifferentialAge = age;
            }

            // Speed in knots - convert to m/s
            if (float.TryParse(words[11], NumberStyles.Float, CultureInfo.InvariantCulture, out float speedKnots))
            {
                double speedMs = speedKnots * 0.514444; // knots to m/s
                gpsData.CurrentPosition = gpsData.CurrentPosition with { Speed = speedMs };
            }

            // Heading
            if (float.TryParse(words[12], NumberStyles.Float, CultureInfo.InvariantCulture, out float heading))
            {
                gpsData.CurrentPosition = gpsData.CurrentPosition with { Heading = heading };
            }

            gpsData.Timestamp = DateTime.Now;

            // Update GPS service with parsed data
            _gpsService.UpdateGpsData(gpsData);
        }
        catch
        {
            // Ignore parse errors
        }
    }

    private void ParsePAOGI(string[] words)
    {
        // PAOGI has same format as PANDA
        ParsePANDA(words);
    }

    private double ParseLatitude(string latString, string hemisphere)
    {
        // Format: DDMM.MMMM
        int decim = latString.IndexOf(".", StringComparison.Ordinal);
        if (decim == -1)
        {
            latString += ".00";
            decim = latString.IndexOf(".", StringComparison.Ordinal);
        }

        decim -= 2; // DD part

        if (!double.TryParse(latString.Substring(0, decim), NumberStyles.Float, CultureInfo.InvariantCulture, out double degrees))
            return 0;

        if (!double.TryParse(latString.Substring(decim), NumberStyles.Float, CultureInfo.InvariantCulture, out double minutes))
            return 0;

        double latitude = degrees + (minutes * 0.01666666666666666666666666666667); // minutes to degrees

        if (hemisphere == "S")
            latitude *= -1;

        return latitude;
    }

    private double ParseLongitude(string lonString, string hemisphere)
    {
        // Format: DDDMM.MMMM
        int decim = lonString.IndexOf(".", StringComparison.Ordinal);
        if (decim == -1)
        {
            lonString += ".00";
            decim = lonString.IndexOf(".", StringComparison.Ordinal);
        }

        decim -= 2; // DDD part

        if (!double.TryParse(lonString.Substring(0, decim), NumberStyles.Float, CultureInfo.InvariantCulture, out double degrees))
            return 0;

        if (!double.TryParse(lonString.Substring(decim), NumberStyles.Float, CultureInfo.InvariantCulture, out double minutes))
            return 0;

        double longitude = degrees + (minutes * 0.01666666666666666666666666666667); // minutes to degrees

        if (hemisphere == "W")
            longitude *= -1;

        return longitude;
    }

    private bool ValidateChecksum(string sentence)
    {
        // Find checksum position
        int asterisk = sentence.IndexOf("*", StringComparison.Ordinal);
        if (asterisk < 1) return false;

        // Calculate checksum
        byte checksum = 0;
        for (int i = 1; i < asterisk; i++) // Start after $
        {
            checksum ^= (byte)sentence[i];
        }

        // Get provided checksum
        string checksumStr = sentence.Substring(asterisk + 1, Math.Min(2, sentence.Length - asterisk - 1));
        if (byte.TryParse(checksumStr, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte providedChecksum))
        {
            return checksum == providedChecksum;
        }

        return false;
    }
}