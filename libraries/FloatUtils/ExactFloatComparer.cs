using System.Collections.Generic;

namespace Silnith.FloatUtils
{
    /// <summary>
    /// A comparer that compares the exact IEEE 754 representation.
    /// </summary>
    /// <seealso cref="InexactFloatComparer"/>
    public class ExactFloatComparer : IEqualityComparer<float>
    {
        /// <inheritdoc/>
        public bool Equals(float x, float y)
        {
            return x == y;
        }

        /// <inheritdoc/>
        public int GetHashCode(float f)
        {
            return f.GetHashCode();
        }
    }
}
