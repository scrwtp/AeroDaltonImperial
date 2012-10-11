using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace scrwtp.AeroDaltonImperial.Common
{
    /// <summary>
    /// A module responsible for collecting and handling targeting data.
    /// </summary>
    internal class StatisticsModule
    {
        /// <summary>
        /// Gets or sets the target statistics.
        /// </summary>
        /// <value>The target statistics.</value>
        private Dictionary<string, TargetStatistics> TargetStatistics { get; set; }

        public StatisticsModule()
        {
            this.TargetStatistics = new Dictionary<string, TargetStatistics>();
        }

        /// <summary>
        /// Gets the statistics entry for the enemy.
        /// </summary>
        /// <param name="name">The name of enemy robot.</param>
        /// <returns></returns>
        public TargetStatistics GetStatistics(string name)
        {
            if (!TargetStatistics.ContainsKey(name))
            {
                TargetStatistics[name] = new TargetStatistics(name);
            }

            return TargetStatistics[name];
        }
    }
}
