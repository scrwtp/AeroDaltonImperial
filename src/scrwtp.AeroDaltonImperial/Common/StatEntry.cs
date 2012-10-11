using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace scrwtp.AeroDaltonImperial.Common
{
    /// <summary>
    /// Statistics entry for segmented targeting data.
    /// Collects the count of wave bullets that have hit the target.
    /// Data is additionally segmented by distance and field area of the target.
    /// </summary>
    internal class StatEntry
    {
        /// <summary>
        /// Gets or sets the guess factor bucket value.
        /// </summary>
        /// <value>The guess factor.</value>
        public int GuessFactorBucket { get; set; }

        /// <summary>
        /// Gets or sets the field area bucket value.
        /// </summary>
        /// <value>The field area.</value>
        public FieldArea FieldAreaBucket { get; set; }

        /// <summary>
        /// Gets or sets the distance bucket value.
        /// </summary>
        /// <value>The target distance.</value>
        public int DistanceBucket { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>Value of the segment (count of wave bullets that have hit the target).</value>
        public int Value { get; set; }
    }
}
