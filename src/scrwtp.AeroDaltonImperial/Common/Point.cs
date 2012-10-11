using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace scrwtp.AeroDaltonImperial.Common
{
    /// <summary>
    /// Simple class representing a point on the battlefield
    /// </summary>
    internal class Point
    {
        public double X { get; set; }
        public double Y { get; set; }

        /// <summary>
        /// Calculates the euclidean distance between two points a and b.
        /// </summary>
        /// <param name="a">Point a.</param>
        /// <param name="b">Point b.</param>
        /// <returns></returns>
        public static double Distance(Point a, Point b)
        {
            return Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
        }
    }
}
