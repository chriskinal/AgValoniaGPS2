using System;
using System.Collections.Generic;
using System.Linq;
using AgOpenGPS.Core.Models;
using CoreField = AgOpenGPS.Core.Models.AgShare;

namespace AgOpenGPS.Classes.AgShare.Helpers
{
    /// <summary>
    /// WinForms representation of LocalFieldModel.
    /// Uses implicit conversions to/from AgOpenGPS.Core.Models.Field types.
    /// </summary>
    public class LocalFieldModel
    {
        public Guid FieldId;
        public string Name;
        public Wgs84 Origin; // StartFix in lat/lon
        public List<List<LocalPoint>> Boundaries; // Outer and holes in local plane
        public List<AbLineLocal> AbLines; // Tracks in local plane with heading

        /// <summary>
        /// Implicit conversion to Core LocalFieldModel
        /// </summary>
        public static implicit operator CoreField.LocalFieldModel(LocalFieldModel model)
        {
            if (model == null) return null;

            return new CoreField.LocalFieldModel
            {
                FieldId = model.FieldId,
                Name = model.Name,
                Origin = model.Origin,
                Boundaries = model.Boundaries?.Select(b => b.Select(p => (CoreField.LocalPoint)p).ToList()).ToList(),
                AbLines = model.AbLines?.Select(a => (CoreField.AbLineLocal)a).ToList()
            };
        }

        /// <summary>
        /// Implicit conversion from Core LocalFieldModel
        /// </summary>
        public static implicit operator LocalFieldModel(CoreField.LocalFieldModel model)
        {
            if (model == null) return null;

            return new LocalFieldModel
            {
                FieldId = model.FieldId,
                Name = model.Name,
                Origin = model.Origin,
                Boundaries = model.Boundaries?.Select(b => b.Select(p => (LocalPoint)p).ToList()).ToList(),
                AbLines = model.AbLines?.Select(a => (AbLineLocal)a).ToList()
            };
        }
    }

    /// <summary>
    /// WinForms representation of LocalPoint.
    /// Uses implicit conversions to/from AgOpenGPS.Core.Models.Field.LocalPoint.
    /// </summary>
    public struct LocalPoint
    {
        public double Easting;
        public double Northing;
        public double Heading;

        public LocalPoint(double e, double n, double heading = 0)
        {
            Easting = e;
            Northing = n;
            Heading = heading;
        }

        /// <summary>
        /// Implicit conversion to Core LocalPoint
        /// </summary>
        public static implicit operator CoreField.LocalPoint(LocalPoint point)
        {
            return new CoreField.LocalPoint(point.Easting, point.Northing, point.Heading);
        }

        /// <summary>
        /// Implicit conversion from Core LocalPoint
        /// </summary>
        public static implicit operator LocalPoint(CoreField.LocalPoint point)
        {
            return new LocalPoint(point.Easting, point.Northing, point.Heading);
        }
    }

    /// <summary>
    /// WinForms representation of AbLineLocal.
    /// Uses implicit conversions to/from AgOpenGPS.Core.Models.Field.AbLineLocal.
    /// </summary>
    public class AbLineLocal
    {
        public string Name;
        public LocalPoint PtA;
        public LocalPoint PtB;
        public double Heading;
        public List<LocalPoint> CurvePoints; // Optional, only filled if Curve

        /// <summary>
        /// Implicit conversion to Core AbLineLocal
        /// </summary>
        public static implicit operator CoreField.AbLineLocal(AbLineLocal line)
        {
            if (line == null) return null;

            return new CoreField.AbLineLocal
            {
                Name = line.Name,
                PtA = line.PtA,
                PtB = line.PtB,
                Heading = line.Heading,
                CurvePoints = line.CurvePoints?.Select(p => (CoreField.LocalPoint)p).ToList()
            };
        }

        /// <summary>
        /// Implicit conversion from Core AbLineLocal
        /// </summary>
        public static implicit operator AbLineLocal(CoreField.AbLineLocal line)
        {
            if (line == null) return null;

            return new AbLineLocal
            {
                Name = line.Name,
                PtA = line.PtA,
                PtB = line.PtB,
                Heading = line.Heading,
                CurvePoints = line.CurvePoints?.Select(p => (LocalPoint)p).ToList()
            };
        }
    }
}
