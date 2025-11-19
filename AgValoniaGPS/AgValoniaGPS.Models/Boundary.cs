using System.Collections.Generic;

namespace AgValoniaGPS.Models;

/// <summary>
/// Represents a field boundary with outer boundary and optional inner boundaries (holes)
/// Matches AgOpenGPS Boundary.txt format
/// </summary>
public class Boundary
{
    /// <summary>
    /// Outer boundary polygon (required)
    /// </summary>
    public BoundaryPolygon? OuterBoundary { get; set; }

    /// <summary>
    /// Inner boundary polygons (holes/exclusions)
    /// </summary>
    public List<BoundaryPolygon> InnerBoundaries { get; set; } = new List<BoundaryPolygon>();

    /// <summary>
    /// Whether this boundary is turned on for display/guidance
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Total area in hectares (calculated from outer boundary minus inner boundaries)
    /// </summary>
    public double AreaHectares
    {
        get
        {
            double area = OuterBoundary?.AreaHectares ?? 0;
            foreach (var inner in InnerBoundaries)
            {
                area -= inner.AreaHectares;
            }
            return area;
        }
    }

    /// <summary>
    /// Check if this boundary is valid (has a valid outer boundary)
    /// </summary>
    public bool IsValid => OuterBoundary?.IsValid ?? false;

    /// <summary>
    /// Check if a point is inside the boundary area (inside outer, outside inner holes)
    /// Ported from AOG_Dev CBoundary.IsPointInsideFenceArea()
    /// </summary>
    /// <param name="easting">Point easting coordinate</param>
    /// <param name="northing">Point northing coordinate</param>
    /// <returns>True if point is inside the usable boundary area</returns>
    public bool IsPointInside(double easting, double northing)
    {
        // First check if inside outer boundary
        if (OuterBoundary == null || !OuterBoundary.IsPointInside(easting, northing))
        {
            return false;
        }

        // Check if inside any inner boundary (hole) - if so, it's outside the usable area
        foreach (var innerBoundary in InnerBoundaries)
        {
            if (!innerBoundary.IsDriveThrough && innerBoundary.IsPointInside(easting, northing))
            {
                return false;
            }
        }

        // Inside outer, outside all inner holes
        return true;
    }

    /// <summary>
    /// Check if a position is inside the boundary
    /// </summary>
    public bool IsPointInside(Position position)
    {
        return IsPointInside(position.Easting, position.Northing);
    }
}
