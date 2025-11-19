using System;
using AgValoniaGPS.Models;

namespace AgValoniaGPS.Services;

/// <summary>
/// Field statistics and area calculation service
/// Ported from AOG_Dev CFieldData.cs
/// </summary>
public class FieldStatisticsService
{
    /// <summary>
    /// Total area worked (sum of all section areas) in square meters
    /// </summary>
    public double WorkedAreaSquareMeters { get; set; } = 0;

    /// <summary>
    /// User-accumulated distance in meters
    /// </summary>
    public double UserDistance { get; set; } = 0;

    /// <summary>
    /// Boundary area (outer minus inner) in square meters
    /// </summary>
    public double BoundaryAreaSquareMeters { get; private set; } = 0;

    /// <summary>
    /// Actual area covered (worked area minus overlap) in square meters
    /// </summary>
    public double ActualAreaCovered { get; private set; } = 0;

    /// <summary>
    /// Overlap percentage
    /// </summary>
    public double OverlapPercent { get; private set; } = 0;

    /// <summary>
    /// Update boundary area from field boundary
    /// </summary>
    public void UpdateBoundaryArea(Boundary? boundary)
    {
        if (boundary == null || !boundary.IsValid)
        {
            BoundaryAreaSquareMeters = 0;
            return;
        }

        // Outer boundary area
        double outerArea = boundary.OuterBoundary?.AreaSquareMeters ?? 0;

        // Subtract inner boundaries (holes)
        double innerArea = 0;
        foreach (var inner in boundary.InnerBoundaries)
        {
            innerArea += inner.AreaSquareMeters;
        }

        BoundaryAreaSquareMeters = outerArea - innerArea;
    }

    /// <summary>
    /// Calculate overlap statistics
    /// </summary>
    public void CalculateOverlap()
    {
        if (WorkedAreaSquareMeters > 0)
        {
            // This is simplified - actual implementation would need section data
            // to calculate actual vs theoretical coverage
            ActualAreaCovered = WorkedAreaSquareMeters * 0.95; // Placeholder
            OverlapPercent = ((WorkedAreaSquareMeters - ActualAreaCovered) / WorkedAreaSquareMeters) * 100;
        }
        else
        {
            ActualAreaCovered = 0;
            OverlapPercent = 0;
        }
    }

    /// <summary>
    /// Get remaining area to work in hectares
    /// </summary>
    public double GetRemainingAreaHectares()
    {
        double remaining = BoundaryAreaSquareMeters - WorkedAreaSquareMeters;
        return remaining / 10000.0; // Convert to hectares
    }

    /// <summary>
    /// Get remaining area percentage
    /// </summary>
    public double GetRemainingPercent()
    {
        if (BoundaryAreaSquareMeters > 10)
        {
            return ((BoundaryAreaSquareMeters - WorkedAreaSquareMeters) * 100 / BoundaryAreaSquareMeters);
        }
        return 0;
    }

    /// <summary>
    /// Calculate estimated time to finish
    /// Ported from AOG_Dev CFieldData.TimeTillFinished
    /// </summary>
    /// <param name="currentSpeed">Current speed in km/h</param>
    /// <param name="toolWidth">Tool width in meters</param>
    /// <returns>Estimated time in minutes</returns>
    public double GetEstimatedTimeToFinish(double currentSpeed, double toolWidth)
    {
        if (currentSpeed > 2)
        {
            // Remaining area (ha) / (tool width (m) * speed (km/h) * 0.1)
            double hoursRemaining = (BoundaryAreaSquareMeters - WorkedAreaSquareMeters) / 10000.0
                / (toolWidth * currentSpeed * 0.1);

            return hoursRemaining * 60; // Convert to minutes
        }

        return double.PositiveInfinity; // Infinite time if not moving
    }

    /// <summary>
    /// Calculate current work rate in hectares per hour
    /// Ported from AOG_Dev CFieldData.WorkRateHour
    /// </summary>
    /// <param name="currentSpeed">Current speed in km/h</param>
    /// <param name="toolWidth">Tool width in meters</param>
    /// <returns>Work rate in hectares per hour</returns>
    public double GetWorkRatePerHour(double currentSpeed, double toolWidth)
    {
        // Tool width (m) * speed (km/h) * 0.1 (km to ha conversion)
        return toolWidth * currentSpeed * 0.1;
    }

    /// <summary>
    /// Reset all statistics
    /// </summary>
    public void Reset()
    {
        WorkedAreaSquareMeters = 0;
        UserDistance = 0;
        ActualAreaCovered = 0;
        OverlapPercent = 0;
    }

    /// <summary>
    /// Format area for display in hectares or acres
    /// </summary>
    public string FormatArea(double squareMeters, bool useMetric = true)
    {
        if (useMetric)
        {
            return (squareMeters / 10000.0).ToString("F2") + " ha";
        }
        else
        {
            return (squareMeters / 4046.86).ToString("F2") + " ac";
        }
    }

    /// <summary>
    /// Format distance for display in meters or feet
    /// </summary>
    public string FormatDistance(double meters, bool useMetric = true)
    {
        if (useMetric)
        {
            return meters.ToString("F1") + " m";
        }
        else
        {
            return (meters * 3.28084).ToString("F1") + " ft";
        }
    }
}
