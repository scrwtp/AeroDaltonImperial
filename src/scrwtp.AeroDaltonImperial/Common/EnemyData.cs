using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace scrwtp.AeroDaltonImperial.Common
{
    /// <summary>
    /// A snapshot of data collected when scanning targets
    /// </summary>
    internal class EnemyData
    {        
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The target name.</value>
        public string Name { get; set; }
                
        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        /// <value>The target location.</value>
        public Point Location { get; set; }
        
        /// <summary>
        /// Gets or sets the time.
        /// </summary>
        /// <value>The time when the snapshot was taken.</value>
        public long Time { get; set; }
        
        /// <summary>
        /// Gets or sets the energy.
        /// </summary>
        /// <value>The energy the target has left.</value>
        public double Energy { get; set; }

        /// <summary>
        /// Gets or sets the last bullet.
        /// </summary>
        /// <value>The last wave bullet fired at target.</value>
        public WaveBullet LastBullet { get; set; }

        /// <summary>
        /// Gets or sets the heading in radians.
        /// </summary>
        /// <value>The heading in radians.</value>
        public double HeadingRadians { get; set; }

        /// <summary>
        /// Gets or sets the bearing in radians.
        /// </summary>
        /// <value>The bearing in radians.</value>
        public double BearingRadians { get; set; }

        /// <summary>
        /// Gets or sets the velocity of the target.
        /// </summary>
        /// <value>The velocity.</value>
        public double Velocity { get; set; }
    }
}
