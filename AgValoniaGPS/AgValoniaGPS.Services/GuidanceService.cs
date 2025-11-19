using System;
using AgValoniaGPS.Models;
using AgValoniaGPS.Services.Interfaces;

namespace AgValoniaGPS.Services;

/// <summary>
/// Implementation of guidance calculation service
/// Ported algorithms from AOG_Dev CVehicle.cs
/// </summary>
public class GuidanceService : IGuidanceService
{
    private readonly VehicleConfiguration _vehicleConfig;

    // State tracking for dead zone
    private int _deadZoneDelayCounter = 0;
    private bool _isInDeadZone = false;

    public event EventHandler<GuidanceData>? GuidanceUpdated;

    public double CrossTrackError { get; private set; }

    public double LookaheadDistance { get; private set; }

    public bool IsActive { get; private set; }

    public GuidanceService(VehicleConfiguration vehicleConfig)
    {
        _vehicleConfig = vehicleConfig;
    }

    public void CalculateGuidance(Position currentPosition, ABLine abLine, Vehicle vehicle)
    {
        if (!IsActive)
            return;

        // TODO: Calculate cross-track error and heading error from currentPosition and abLine
        // For now, using placeholders until ABLine calculations are implemented
        double xte = CrossTrackError; // Placeholder - actual calculation needed
        double headingError = 0; // Placeholder - actual calculation needed
        double currentSpeed = currentPosition.Speed; // Speed comes from GPS position

        // Calculate dynamic lookahead distance using AOG_Dev algorithm
        LookaheadDistance = CalculateGoalPointDistance(currentSpeed, xte, true);

        // Update dead zone state
        UpdateDeadZone(headingError);

        // Calculate steering angle (using Pure Pursuit or Stanley)
        double steerAngle = 0;
        if (!_isInDeadZone)
        {
            // Example using Pure Pursuit (would need goal point calculations)
            // steerAngle = CalculatePurePursuitSteering(goalPointX, goalPointY);

            // Or using Stanley
            steerAngle = CalculateStanleySteering(xte, headingError, currentSpeed);
        }

        var guidanceData = new GuidanceData
        {
            CrossTrackError = CrossTrackError,
            LookaheadDistance = LookaheadDistance,
            SteerAngle = steerAngle,
            IsOnLine = Math.Abs(CrossTrackError) < 0.1 // Within 10cm
        };

        GuidanceUpdated?.Invoke(this, guidanceData);
    }

    /// <summary>
    /// Calculate goal point distance based on speed and cross-track error
    /// Ported from AOG_Dev CVehicle.UpdateGoalPointDistance()
    /// </summary>
    private double CalculateGoalPointDistance(double currentSpeed, double crossTrackError, bool isAutoSteerOn)
    {
        double xTE = Math.Abs(crossTrackError);

        // Base goal point distance: speed * 0.05 * multiplier
        double goalPointDistance = currentSpeed * 0.05 * _vehicleConfig.GoalPointLookAheadMult;

        double lookAheadHold = _vehicleConfig.GoalPointLookAheadHold;
        double lookAheadAcquire = lookAheadHold * _vehicleConfig.GoalPointAcquireFactor;

        if (!isAutoSteerOn)
        {
            lookAheadHold = 5;
            lookAheadAcquire = lookAheadHold * _vehicleConfig.GoalPointAcquireFactor;
        }

        // Adjust look-ahead based on cross-track error
        if (xTE <= 0.1)
        {
            // Very close to line - use hold distance
            goalPointDistance *= lookAheadHold;
            goalPointDistance += lookAheadHold;
        }
        else if (xTE > 0.1 && xTE < 0.4)
        {
            // Transition zone - interpolate
            xTE -= 0.1;
            lookAheadHold = (1 - (xTE / 0.3)) * (lookAheadHold - lookAheadAcquire);
            lookAheadHold += lookAheadAcquire;

            goalPointDistance *= lookAheadHold;
            goalPointDistance += lookAheadHold;
        }
        else
        {
            // Far from line - use acquire distance
            goalPointDistance *= lookAheadAcquire;
            goalPointDistance += lookAheadAcquire;
        }

        // Enforce minimum
        if (goalPointDistance < _vehicleConfig.MinLookAheadDistance)
        {
            goalPointDistance = _vehicleConfig.MinLookAheadDistance;
        }

        return goalPointDistance;
    }

    /// <summary>
    /// Update heading dead zone state
    /// </summary>
    private void UpdateDeadZone(double headingError)
    {
        double deadZoneHeading = _vehicleConfig.DeadZoneHeading * 0.01; // Convert

        if (Math.Abs(headingError) < deadZoneHeading)
        {
            if (_deadZoneDelayCounter < _vehicleConfig.DeadZoneDelay)
            {
                _deadZoneDelayCounter++;
            }
            else
            {
                _isInDeadZone = true;
            }
        }
        else
        {
            _isInDeadZone = false;
            _deadZoneDelayCounter = 0;
        }
    }

    /// <summary>
    /// Calculate Stanley steering output
    /// </summary>
    private double CalculateStanleySteering(double crossTrackError, double headingError, double currentSpeed)
    {
        double speedMs = currentSpeed * 0.27778; // Convert km/h to m/s
        if (speedMs < 0.1) speedMs = 0.1;

        double distanceComponent = Math.Atan(
            _vehicleConfig.StanleyDistanceErrorGain * crossTrackError / speedMs);

        double headingComponent = headingError * _vehicleConfig.StanleyHeadingErrorGain;

        return headingComponent + distanceComponent;
    }

    /// <summary>
    /// Calculate Pure Pursuit steering output
    /// </summary>
    private double CalculatePurePursuitSteering(double goalPointX, double goalPointY)
    {
        double lookAheadDist = Math.Sqrt(goalPointX * goalPointX + goalPointY * goalPointY);
        if (lookAheadDist < 0.1) return 0;

        double alpha = Math.Atan2(goalPointY, goalPointX);
        double curvature = 2 * Math.Sin(alpha) / lookAheadDist;

        return Math.Atan(curvature * _vehicleConfig.Wheelbase);
    }

    public void Start()
    {
        IsActive = true;
        _deadZoneDelayCounter = 0;
        _isInDeadZone = false;
    }

    public void Stop()
    {
        IsActive = false;
    }
}