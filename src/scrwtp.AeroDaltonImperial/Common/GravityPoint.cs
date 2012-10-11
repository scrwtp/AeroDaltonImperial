using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace scrwtp.AeroDaltonImperial.Common
{
    /// <summary>
    /// A gravity source
    /// </summary>
    internal class GravityPoint
    {
        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        /// <value>The location of the gravity source.</value>
        public Point Location { get; set; }
        
        /// <summary>
        /// Gets or sets the power of the gravity source.
        /// </summary>
        /// <value>The gravity force value, positive pulls towards location, negative pushes away.</value>
        public double Power { get; set; }
        
        /// <summary>
        /// Gets or sets the distance decay rate.
        /// </summary>
        /// <value>The rate at which force value decays with distance (an exponent).</value>
        public double DistanceDecay { get; set; }
    }
}
