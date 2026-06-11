
using System;
using System.ComponentModel.DataAnnotations;

namespace Silnith.FloatUtils
{
    /// <summary>
    /// Configurable settings for <see cref="InexactFloatComparer"/>.
    /// This allows controlling the rounding behavior for comparisons.
    /// </summary>
    /// <seealso cref="ExactFloatComparer"/>
    /// <seealso cref="InexactFloatComparer"/>
    public class FloatComparerSettings
    {
        /// <summary>
        /// The number of bits to drop from the mantissa when comparing
        /// floating-point values.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <see cref="InexactFloatComparer"/> always drops at least one bit
        /// of precision
        /// </para>
        /// <para>
        /// The current implementation rounds instead of truncating.
        /// </para>
        /// </remarks>
        [Range(1, 24)]
        public int MantissaBitsDropped
        {
            get;
            set;
        } = 1;
        // TODO: Provide configurable rounding behavior.

        /// <summary>
        /// The exponent below which numbers will be regarded as <c>0</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When comparing floating-point numbers, achieving numerical stability
        /// at very low exponents is difficult.  Therefore most algorithms
        /// include some form of cut-off for very small values.
        /// </para>
        /// </remarks>
        [Range(-126, 128)]
        public int MinimumExponent
        {
            get;
            set;
        } = -126;
    }
}
