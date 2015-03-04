﻿using System.Collections.Generic;
using System.Linq;
using SharpDX;

namespace LeagueSharp.CommonEx.Core.Math
{
    /// <summary>
    ///     Provides methods for finding the minimum enclosing circles.
    /// </summary>
    public class MEC
    {
        /// <summary>
        ///     For debuging. Returns the minimun maximun corners.
        /// </summary>
        public static Vector2[] MinMaxCorners;

        /// <summary>
        ///     For debuging. Returns the minimun max box.
        /// </summary>
        public static RectangleF MinMaxBox;

        /// <summary>
        ///     For debugging. Returns the non culled points.
        /// </summary>
        public static Vector2[] NonCulledPoints;

        /// <summary>
        ///     Returns the mininimum enclosing circle from a list of points.
        /// </summary>
        /// <param name="points">List of points</param>
        /// <returns>
        ///     <see cref="MecCircle" />
        /// </returns>
        public static MecCircle GetMec(List<Vector2> points)
        {
            Vector2 center;
            float radius;

            var convexHull = MakeConvexHull(points);
            FindMinimalBoundingCircle(convexHull, out center, out radius);
            return new MecCircle(center, radius);
        }

        /// <summary>
        ///     Find the points nearest the upper left, upper right, lower left, and lower right corners.
        /// </summary>
        /// <param name="points">Points</param>
        /// <param name="upperLeft">Upper left <see cref="Vector2" /></param>
        /// <param name="upperRight">Upper right <see cref="Vector2" /></param>
        /// <param name="lowerLeft">Lower left <see cref="Vector2" /></param>
        /// <param name="lowerRight">Lower right <see cref="Vector2" /></param>
        private static void GetMinMaxCorners(IReadOnlyList<Vector2> points,
            ref Vector2 upperLeft,
            ref Vector2 upperRight,
            ref Vector2 lowerLeft,
            ref Vector2 lowerRight)
        {
            // Start with the first point as the solution.
            upperLeft = points[0];
            upperRight = upperLeft;
            lowerLeft = upperLeft;
            lowerRight = upperLeft;

            // Search the other points.
            foreach (var pt in points)
            {
                if (-pt.X - pt.Y > -upperLeft.X - upperLeft.Y)
                {
                    upperLeft = pt;
                }
                if (pt.X - pt.Y > upperRight.X - upperRight.Y)
                {
                    upperRight = pt;
                }
                if (-pt.X + pt.Y > -lowerLeft.X + lowerLeft.Y)
                {
                    lowerLeft = pt;
                }
                if (pt.X + pt.Y > lowerRight.X + lowerRight.Y)
                {
                    lowerRight = pt;
                }
            }

            MinMaxCorners = new[] { upperLeft, upperRight, lowerRight, lowerLeft };
        }

        /// <summary>
        ///     Find a box that fits inside the MinMax quadrilateral.
        /// </summary>
        /// <param name="points">Points</param>
        /// <returns>
        ///     <see cref="RectangleF" />
        /// </returns>
        private static RectangleF GetMinMaxBox(IReadOnlyList<Vector2> points)
        {
            // Find the MinMax quadrilateral.
            Vector2 ul = new Vector2(0, 0), ur = ul, ll = ul, lr = ul;
            GetMinMaxCorners(points, ref ul, ref ur, ref ll, ref lr);

            // Get the coordinates of a box that lies inside this quadrilateral.
            var xmin = ul.X;
            var ymin = ul.Y;

            var xmax = ur.X;
            if (ymin < ur.Y)
            {
                ymin = ur.Y;
            }

            if (xmax > lr.X)
            {
                xmax = lr.X;
            }
            var ymax = lr.Y;

            if (xmin < ll.X)
            {
                xmin = ll.X;
            }
            if (ymax > ll.Y)
            {
                ymax = ll.Y;
            }

            var result = new RectangleF(xmin, ymin, xmax - xmin, ymax - ymin);
            MinMaxBox = result;
            return result;
        }

        /// <summary>
        ///     Cull points out of the convex hull that lie inside the trapezoid defined by the vertices with smallest and largest
        ///     X and Y coordinates. Return the points that are not culled.
        /// </summary>
        /// <param name="points">Points</param>
        /// <returns>List of <see cref="Vector2" /></returns>
        private static List<Vector2> HullCull(IReadOnlyList<Vector2> points)
        {
            // Find a culling box.
            var cullingBox = GetMinMaxBox(points);

            // Cull the points.
            var results =
                points.Where(
                    pt =>
                        pt.X <= cullingBox.Left || pt.X >= cullingBox.Right || pt.Y <= cullingBox.Top ||
                        pt.Y >= cullingBox.Bottom).ToList();

            NonCulledPoints = new Vector2[results.Count]; // For debugging.
            results.CopyTo(NonCulledPoints); // For debugging.
            return results;
        }

        /// <summary>
        ///     Return the points that make up a polygon's convex hull. This method leaves the points list unchanged.
        /// </summary>
        /// <param name="points">Points</param>
        /// <returns>List of <see cref="Vector2" /></returns>
        public static List<Vector2> MakeConvexHull(List<Vector2> points)
        {
            // Cull.
            points = HullCull(points);

            // Find the remaining point with the smallest Y value.
            // if (there's a tie, take the one with the smaller X value.
            Vector2[] bestPt = { points[0] };
            foreach (var pt in
                points.Where(
                    pt =>
                        (pt.Y < bestPt[0].Y) ||
                        ((System.Math.Abs(pt.Y - bestPt[0].Y) < float.Epsilon) && (pt.X < bestPt[0].X))))
            {
                bestPt[0] = pt;
            }

            // Move this point to the convex hull.
            var hull = new List<Vector2> { bestPt[0] };
            points.Remove(bestPt[0]);

            // Start wrapping up the other points.
            float sweepAngle = 0;
            for (;;)
            {
                // If all of the points are on the hull, we're done.
                if (points.Count == 0)
                {
                    break;
                }

                // Find the point with smallest AngleValue
                // from the last point.
                var x = hull[hull.Count - 1].X;
                var y = hull[hull.Count - 1].Y;
                bestPt[0] = points[0];
                float bestAngle = 3600;

                // Search the rest of the points.
                foreach (var pt in points)
                {
                    var testAngle = AngleValue(x, y, pt.X, pt.Y);

                    if ((!(testAngle >= sweepAngle)) || (!(bestAngle > testAngle)))
                    {
                        continue;
                    }

                    bestAngle = testAngle;
                    bestPt[0] = pt;
                }

                // See if the first point is better.
                // If so, we are done.
                var firstAngle = AngleValue(x, y, hull[0].X, hull[0].Y);
                if ((firstAngle >= sweepAngle) && (bestAngle >= firstAngle))
                {
                    // The first point is better. We're done.
                    break;
                }

                // Add the best point to the convex hull.
                hull.Add(bestPt[0]);
                points.Remove(bestPt[0]);

                sweepAngle = bestAngle;
            }

            return hull;
        }

        /// <summary>
        ///     Return a number that gives the ordering of angles
        ///     WRST horizontal from the point (x1, y1) to (x2, y2).
        ///     In other words, AngleValue(x1, y1, x2, y2) is not
        ///     the angle, but if:
        ///     Angle(x1, y1, x2, y2) > Angle(x1, y1, x2, y2)
        ///     then
        ///     AngleValue(x1, y1, x2, y2) > AngleValue(x1, y1, x2, y2)
        ///     this angle is greater than the angle for another set
        ///     of points,) this number for
        ///     This function is dy / (dy + dx).
        /// </summary>
        /// <param name="x1">First X</param>
        /// <param name="y1">First Y</param>
        /// <param name="x2">Second X</param>
        /// <param name="y2">Second Y</param>
        /// <returns></returns>
        private static float AngleValue(float x1, float y1, float x2, float y2)
        {
            float t;

            var dx = x2 - x1;
            var ax = System.Math.Abs(dx);
            var dy = y2 - y1;
            var ay = System.Math.Abs(dy);
            if ((ax + ay).Equals(0))
            {
                // if (the two points are the same, return 360.
                t = 360f / 9f;
            }
            else
            {
                t = dy / (ax + ay);
            }
            if (dx < 0)
            {
                t = 2 - t;
            }
            else if (dy < 0)
            {
                t = 4 + t;
            }
            return t * 90;
        }

        /// <summary>
        ///     Find a minimal bounding circle.
        /// </summary>
        /// <param name="points">Points</param>
        /// <param name="center">The center</param>
        /// <param name="radius">The radius</param>
        public static void FindMinimalBoundingCircle(List<Vector2> points, out Vector2 center, out float radius)
        {
            // Find the convex hull.
            var hull = MakeConvexHull(points);

            // The best solution so far.
            var bestCenter = points[0];
            var bestRadius2 = float.MaxValue;

            // Look at pairs of hull points.
            for (var i = 0; i < hull.Count - 1; i++)
            {
                for (var j = i + 1; j < hull.Count; j++)
                {
                    // Find the circle through these two points.
                    var testCenter = new Vector2((hull[i].X + hull[j].X) / 2f, (hull[i].Y + hull[j].Y) / 2f);
                    var dx = testCenter.X - hull[i].X;
                    var dy = testCenter.Y - hull[i].Y;
                    var testRadius2 = dx * dx + dy * dy;

                    // See if this circle would be an improvement.
                    if (!(testRadius2 < bestRadius2))
                    {
                        continue;
                    }

                    // See if this circle encloses all of the points.
                    if (!CircleEnclosesPoints(testCenter, testRadius2, points, i, j, -1))
                    {
                        continue;
                    }

                    // Save this solution.
                    bestCenter = testCenter;
                    bestRadius2 = testRadius2;
                } // for i
            } // for j

            // Look at triples of hull points.
            for (var i = 0; i < hull.Count - 2; i++)
            {
                for (var j = i + 1; j < hull.Count - 1; j++)
                {
                    for (var k = j + 1; k < hull.Count; k++)
                    {
                        // Find the circle through these three points.
                        Vector2 testCenter;
                        float testRadius2;
                        FindCircle(hull[i], hull[j], hull[k], out testCenter, out testRadius2);

                        // See if this circle would be an improvement.
                        if (!(testRadius2 < bestRadius2))
                        {
                            continue;
                        }

                        // See if this circle encloses all of the points.
                        if (!CircleEnclosesPoints(testCenter, testRadius2, points, i, j, k))
                        {
                            continue;
                        }
                        // Save this solution.
                        bestCenter = testCenter;
                        bestRadius2 = testRadius2;
                    } // for k
                } // for i
            } // for j

            center = bestCenter;
            if (bestRadius2.Equals(float.MaxValue))
            {
                radius = 0;
            }
            else
            {
                radius = (float) System.Math.Sqrt(bestRadius2);
            }
        }

        // Return true if the indicated circle encloses all of the points.
        private static bool CircleEnclosesPoints(Vector2 center,
            float radius2,
            IEnumerable<Vector2> points,
            int skip1,
            int skip2,
            int skip3)
        {
            return (from point in points.Where((t, i) => (i != skip1) && (i != skip2) && (i != skip3))
                let dx = center.X - point.X
                let dy = center.Y - point.Y
                select dx * dx + dy * dy).All(testRadius2 => !(testRadius2 > radius2));
        }

        // Find a circle through the three points.
        private static void FindCircle(Vector2 a, Vector2 b, Vector2 c, out Vector2 center, out float radius2)
        {
            // Get the perpendicular bisector of (x1, y1) and (x2, y2).
            var x1 = (b.X + a.X) / 2;
            var y1 = (b.Y + a.Y) / 2;
            var dy1 = b.X - a.X;
            var dx1 = -(b.Y - a.Y);

            // Get the perpendicular bisector of (x2, y2) and (x3, y3).
            var x2 = (c.X + b.X) / 2;
            var y2 = (c.Y + b.Y) / 2;
            var dy2 = c.X - b.X;
            var dx2 = -(c.Y - b.Y);

            // See where the lines intersect.
            var cx = (y1 * dx1 * dx2 + x2 * dx1 * dy2 - x1 * dy1 * dx2 - y2 * dx1 * dx2) / (dx1 * dy2 - dy1 * dx2);
            var cy = (cx - x1) * dy1 / dx1 + y1;
            center = new Vector2(cx, cy);

            var dx = cx - a.X;
            var dy = cy - a.Y;
            radius2 = dx * dx + dy * dy;
        }

        /// <summary>
        ///     Contains Center and Radius
        /// </summary>
        public struct MecCircle
        {
            /// <summary>
            ///     The center
            /// </summary>
            public Vector2 Center;

            /// <summary>
            ///     The Radius
            /// </summary>
            public float Radius;

            internal MecCircle(Vector2 center, float radius)
            {
                Center = center;
                Radius = radius;
            }
        }
    }
}