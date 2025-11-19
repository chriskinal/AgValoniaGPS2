using System;
using AgValoniaGPS.Models;

namespace AgValoniaGPS.Services.Interfaces;

/// <summary>
/// Service for GPS data processing and management
/// </summary>
public interface IGpsService
{
    /// <summary>
    /// Event fired when new GPS data is received
    /// </summary>
    event EventHandler<GpsData>? GpsDataUpdated;

    /// <summary>
    /// Current GPS data
    /// </summary>
    GpsData CurrentData { get; }

    /// <summary>
    /// Whether GPS is currently connected and receiving data
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Start GPS service
    /// </summary>
    void Start();

    /// <summary>
    /// Stop GPS service
    /// </summary>
    void Stop();

    /// <summary>
    /// Process NMEA sentence
    /// </summary>
    void ProcessNmeaSentence(string sentence);
}