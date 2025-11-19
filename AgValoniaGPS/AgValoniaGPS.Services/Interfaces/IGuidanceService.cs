using System;
using AgValoniaGPS.Models;

namespace AgValoniaGPS.Services.Interfaces;

/// <summary>
/// Service for guidance calculations (cross track error, lookahead, etc.)
/// </summary>
public interface IGuidanceService
{
    /// <summary>
    /// Event fired when guidance data is updated
    /// </summary>
    event EventHandler<GuidanceData>? GuidanceUpdated;

    /// <summary>
    /// Current cross track error in meters (distance from AB line)
    /// </summary>
    double CrossTrackError { get; }

    /// <summary>
    /// Current lookahead distance in meters
    /// </summary>
    double LookaheadDistance { get; }

    /// <summary>
    /// Whether guidance is currently active
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Calculate guidance based on current position and active AB line
    /// </summary>
    void CalculateGuidance(Position currentPosition, ABLine abLine, Vehicle vehicle);

    /// <summary>
    /// Start guidance
    /// </summary>
    void Start();

    /// <summary>
    /// Stop guidance
    /// </summary>
    void Stop();
}

/// <summary>
/// Guidance calculation results
/// </summary>
public class GuidanceData
{
    public double CrossTrackError { get; set; }
    public double LookaheadDistance { get; set; }
    public double SteerAngle { get; set; }
    public bool IsOnLine { get; set; }
}