using System;
using System.Threading.Tasks;

namespace AgValoniaGPS.Services.Interfaces;

/// <summary>
/// Service for connecting to NTRIP caster to receive RTK correction data
/// Forwards RTCM3 corrections to GPS module via UDP port 2233
/// </summary>
public interface INtripClientService
{
    /// <summary>
    /// Event fired when connection status changes
    /// </summary>
    event EventHandler<NtripConnectionEventArgs>? ConnectionStatusChanged;

    /// <summary>
    /// Event fired when RTCM data is received and forwarded
    /// </summary>
    event EventHandler<RtcmDataReceivedEventArgs>? RtcmDataReceived;

    /// <summary>
    /// Whether NTRIP client is connected to caster
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Total bytes received from caster
    /// </summary>
    ulong TotalBytesReceived { get; }

    /// <summary>
    /// Connect to NTRIP caster with specified configuration
    /// </summary>
    Task ConnectAsync(NtripConfiguration config);

    /// <summary>
    /// Disconnect from NTRIP caster
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Send GGA sentence to caster (for VRS/network RTK)
    /// </summary>
    Task SendGgaSentenceAsync(string ggaSentence);
}

/// <summary>
/// NTRIP connection configuration
/// </summary>
public class NtripConfiguration
{
    /// <summary>
    /// Caster IP address or hostname
    /// </summary>
    public string CasterAddress { get; set; } = "";

    /// <summary>
    /// Caster port (typically 2101 or 80)
    /// </summary>
    public int CasterPort { get; set; } = 2101;

    /// <summary>
    /// Mount point name
    /// </summary>
    public string MountPoint { get; set; } = "";

    /// <summary>
    /// Username for authentication
    /// </summary>
    public string Username { get; set; } = "";

    /// <summary>
    /// Password for authentication
    /// </summary>
    public string Password { get; set; } = "";

    /// <summary>
    /// HTTP version (1.0 or 1.1)
    /// </summary>
    public string HttpVersion { get; set; } = "1.0";

    /// <summary>
    /// UDP port to forward RTCM data to (default 2233)
    /// </summary>
    public int UdpForwardPort { get; set; } = 2233;

    /// <summary>
    /// Subnet for UDP broadcast (e.g., "192.168.5")
    /// </summary>
    public string SubnetAddress { get; set; } = "192.168.5";

    /// <summary>
    /// GGA send interval in seconds (0 = disabled)
    /// </summary>
    public int GgaIntervalSeconds { get; set; } = 10;

    /// <summary>
    /// Use manual position for GGA (vs GPS position)
    /// </summary>
    public bool UseManualPosition { get; set; } = false;

    /// <summary>
    /// Manual latitude for GGA (if UseManualPosition is true)
    /// </summary>
    public double ManualLatitude { get; set; } = 0;

    /// <summary>
    /// Manual longitude for GGA (if UseManualPosition is true)
    /// </summary>
    public double ManualLongitude { get; set; } = 0;
}

/// <summary>
/// Event args for NTRIP connection status change
/// </summary>
public class NtripConnectionEventArgs : EventArgs
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

/// <summary>
/// Event args for RTCM data received
/// </summary>
public class RtcmDataReceivedEventArgs : EventArgs
{
    public int BytesReceived { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}