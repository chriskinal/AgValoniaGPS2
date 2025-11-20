using System;
using System.Collections.Generic;
using System.Linq;
using AgOpenGPS.Core.Models.Base;
using AgOpenGPS.Core.Models.Guidance;

namespace AgOpenGPS
{
    /// <summary>
    /// WinForms wrapper for HeadlandLine from AgOpenGPS.Core
    /// Maintains backward compatibility while enabling Core integration
    /// </summary>
    public class CHeadLine
    {
        public List<CHeadPath> tracksArr = new List<CHeadPath>();

        public int idx;

        public List<vec3> desList = new List<vec3>();

        public CHeadLine()
        {
            // FormGPS dependency removed - no longer couples to UI
        }

        /// <summary>
        /// Convert this WinForms CHeadLine to Core HeadlandLine for serialization/cross-platform use
        /// </summary>
        public HeadlandLine ToCoreHeadlandLine()
        {
            return new HeadlandLine
            {
                Tracks = tracksArr.Select(t => t.ToCoreHeadlandPath()).ToList(),
                CurrentIndex = idx,
                DesiredPoints = desList.Select(v => (Vec3)v).ToList()
            };
        }

        /// <summary>
        /// Create WinForms CHeadLine from Core HeadlandLine
        /// </summary>
        public static CHeadLine FromCoreHeadlandLine(HeadlandLine core)
        {
            return new CHeadLine
            {
                tracksArr = core.Tracks.Select(CHeadPath.FromCoreHeadlandPath).ToList(),
                idx = core.CurrentIndex,
                desList = core.DesiredPoints.Select(v => (vec3)v).ToList()
            };
        }
    }

    /// <summary>
    /// WinForms wrapper for HeadlandPath from AgOpenGPS.Core
    /// Maintains backward compatibility while enabling Core integration
    /// </summary>
    public class CHeadPath
    {
        public List<vec3> trackPts = new List<vec3>();
        public string name = "";
        public double moveDistance = 0;
        public int mode = 0;
        public int a_point = 0;

        /// <summary>
        /// Convert this WinForms CHeadPath to Core HeadlandPath for serialization/cross-platform use
        /// </summary>
        public HeadlandPath ToCoreHeadlandPath()
        {
            return new HeadlandPath
            {
                TrackPoints = trackPts.Select(v => (Vec3)v).ToList(),
                Name = name,
                MoveDistance = moveDistance,
                Mode = mode,
                APointIndex = a_point
            };
        }

        /// <summary>
        /// Create WinForms CHeadPath from Core HeadlandPath
        /// </summary>
        public static CHeadPath FromCoreHeadlandPath(HeadlandPath core)
        {
            return new CHeadPath
            {
                trackPts = core.TrackPoints.Select(v => (vec3)v).ToList(),
                name = core.Name,
                moveDistance = core.MoveDistance,
                mode = core.Mode,
                a_point = core.APointIndex
            };
        }
    }
}