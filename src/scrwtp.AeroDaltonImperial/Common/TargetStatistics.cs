using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace scrwtp.AeroDaltonImperial.Common
{
    /// <summary>
    /// Encapsulates various targeting statistics collected for an enemy.
    /// </summary>
    internal class TargetStatistics
    {
        /// <summary>
        /// Gets or sets the name of the target.
        /// </summary>
        /// <value>The name of the target.</value>
        string TargetName { get; set; }

        /// <summary>
        /// Gets or sets the segmented targeting data.
        /// </summary>
        /// <value>Segmented data collected for guess factor targeting gun.</value>
        public List<StatEntry> SegmentedTargeting { get; set; }
        
        /// <summary>
        /// Gets or sets the enemy pattern history.
        /// </summary>
        /// <value>Past enemy data entries collected for pattern matching gun. Not yet implemented</value>
        public List<EnemyData> PatternHistory { get; set; }

        /// <summary>
        /// Gets or sets the wave bullets.
        /// </summary>
        /// <value>Active wave bullets fired at the target.</value>
        public List<WaveBullet> WaveBullets { get; set; }

        public TargetStatistics(string name)
        {
            this.TargetName = name;

            this.SegmentedTargeting = new List<StatEntry>();
            this.PatternHistory = new List<EnemyData>();
            this.WaveBullets = new List<WaveBullet>();
        }
    }
}
