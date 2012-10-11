using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Robocode.Util;

namespace scrwtp.AeroDaltonImperial.Common
{
    /// <summary>
    /// A virtual bullet used for collecting targeting statistics.
    /// For more information, see Guess Factor Targeting.
    /// </summary>
    internal class WaveBullet
    {
        /// <summary>
        /// Gets or sets the start point.
        /// </summary>
        /// <value>The location from which the bullet was fired.</value>
        public Point StartPoint { get; set; }
        
        /// <summary>
        /// Gets or sets the start bearing.
        /// </summary>
        /// <value>The bearing at which the bullet was fired.</value>
        public double StartBearing { get; set; }

        /// <summary>
        /// Gets or sets the direction.
        /// </summary>
        /// <value>The direction in which the bullet was fired.</value>
        public int Direction { get; set; }

        /// <summary>
        /// Gets or sets the name of the target.
        /// </summary>
        /// <value>The name of the target.</value>
        public string TargetName { get; set; }

        /// <summary>
        /// Gets or sets the power.
        /// </summary>
        /// <value>The fire power calculated for the bullet.</value>
        public double Power { get; set; }

        /// <summary>
        /// Gets or sets the time.
        /// </summary>
        /// <value>The time at which the bullet was fired.</value>
        public long Time { get; set; }

        /// <summary>
        /// Gets the bullet speed (depends on fire power).
        /// </summary>
        /// <returns></returns>
        public double GetBulletSpeed()
        {
            return 20 - this.Power * 3;
        }

        /// <summary>
        /// Gets the estimated maximum escape angle for the bullet (depends on bullet speed)
        /// </summary>
        /// <returns></returns>
        public double MaxEscapeAngle()
        {
            return Math.Asin(8 / GetBulletSpeed());
        }

        /// <summary>
        /// Checks if the bullet has hit.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="enemyLocation">The enemy location.</param>
        /// <param name="time">The time.</param>
        /// <param name="guessFactor">Guess factor at which the actual bullet should be fired to hit.</param>
        /// <returns></returns>
        public bool CheckHit(string name, Point enemyLocation, long time, out double guessFactor)
        {
            if (this.TargetName == name && Point.Distance(this.StartPoint, enemyLocation) <= (time - this.Time) * GetBulletSpeed())
            {
                double desiredDirection = Math.Atan2(enemyLocation.X - this.StartPoint.X, enemyLocation.Y - this.StartPoint.Y);
                double angleOffset = Utils.NormalRelativeAngle(desiredDirection - this.StartBearing);
                guessFactor = Math.Max(-1, Math.Min(1, angleOffset / MaxEscapeAngle())) * this.Direction;

                return true;
            }

            guessFactor = 0;
            return false;
        }
    }
}
