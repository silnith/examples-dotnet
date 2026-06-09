using Microsoft.Extensions.Options;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Silnith.FloatUtils
{
    /// <summary>
    /// A comparer for floating-point numbers that allows tolerance between close numbers.
    /// </summary>
    /// <seealso cref="ExactFloatComparer"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-single#floating-point-representation-and-precision"/>
    public class InexactFloatComparer : IEqualityComparer<float>
    {
        /// <summary>
        /// Creates a new comparer using the provided settings.
        /// </summary>
        /// <example>
        /// <code>
        /// using Microsoft.Extensions.Options;
        /// 
        /// IOptions&lt;FloatComparerSettings&gt; options = Options.Create(new FloatComparerSettings
        /// {
        ///     MantissaBitsDropped = 4,
        ///     MinimumExponent = -120,
        /// });
        /// InexactFloatComparer floatComparer = new InexactFloatComparer(options);
        /// </code>
        /// </example>
        /// <param name="options">The settings that control how comparisons are done.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="options"/> is <see langword="null"/>.</exception>
        public InexactFloatComparer(IOptions<FloatComparerSettings> options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            MantissaBitsDropped = options.Value.MantissaBitsDropped;
            if (MantissaBitsDropped < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(FloatComparerSettings.MantissaBitsDropped), MantissaBitsDropped, "Must be at least 1.");
            }
            if (MantissaBitsDropped > 24)
            {
                throw new ArgumentOutOfRangeException(nameof(FloatComparerSettings.MantissaBitsDropped), MantissaBitsDropped, "Cannot be greater than 24.");
            }
            /*
             * We need to validate the MantissaBitsDropped because otherwise
             * the shift operations will ignore bits of the value, producing
             * surprising behavior at runtime.
             */
            DropMask = (1u << MantissaBitsDropped) - 1u;
            MantissaBitsRetained = 24 - MantissaBitsDropped;
            MinimumExponent = options.Value.MinimumExponent;
            /*
             * We do not actually care whether MinimumExponent is a valid value.
             * If the value is nonsense, the code will still work.
             */
        }

        /// <summary>
        /// The number of bits that this comparer drops from the mantissa.
        /// This will be in the range <c>[1, 24]</c>.
        /// </summary>
        [Range(1, 24)]
        public int MantissaBitsDropped
        {
            get;
        }

        /// <summary>
        /// The mask used for dropping mantissa bits.
        /// This is of bit length <see cref="MantissaBitsDropped"/>,
        /// and is all <c>1</c>s.
        /// </summary>
        private uint DropMask
        {
            get;
        }

        /// <summary>
        /// The number of bits of the mantissa that this comparer retains.
        /// This will be in the range <c>[0, 23]</c>.
        /// </summary>
        [Range(0, 23)]
        public int MantissaBitsRetained
        {
            get;
        }

        /// <summary>
        /// The minimum exponent necessary for a number to be considered non-zero.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The single-precision floating-point format does not extend lower than
        /// <c>-126</c> for exponents.  It can go up to <c>127</c>, so setting
        /// a value higher will cause the comparer to treat all non-infinity
        /// values as zero.
        /// </para>
        /// </remarks>
        [Range(-126, 128)]
        public int MinimumExponent
        {
            get;
        }

        /// <inheritdoc/>
        public bool Equals(float x, float y)
        {
            return GetNormalizedBits(x) == GetNormalizedBits(y);
        }

        /// <inheritdoc/>
        public int GetHashCode(float f)
        {
            return (int) GetNormalizedBits(f);
        }

        public uint GetNormalizedBits(float f)
        {
            uint signBit;
            uint newExponentBits;
            uint newMantissaBits;
            {
                int realExponent;
                uint shortenedMantissa;
                uint droppedBits;
                {
                    /*
                     * A scope to make sure that the full mantissa goes out of scope
                     * once we shorten it by dropping bits.
                     */
                    uint fullMantissa;
                    {
                        /*
                         * A scope to make sure the bit fields go out of scope once we
                         * have extracted the values from them.
                         */
                        uint exponentBits;
                        uint mantissaBits;
                        {
                            /*
                             * And one more scope to make sure the raw bits are
                             * only ever available for extracting the component
                             * fields.
                             */
                            uint foo = BitConverter.ToUInt32(BitConverter.GetBytes(f));
                            if (BitConverter.IsLittleEndian)
                            {
                            }
                            else
                            {
                            }
                            uint bits = (uint) BitConverter.SingleToInt32Bits(f);
                            signBit = GetSignBit(bits);
                            exponentBits = GetExponentBits(bits);
                            mantissaBits = GetMantissaBits(bits);
                        }
                        /*
                         * At this point we know:
                         * signBit is in the range [0, 1]
                         * exponentBits is in the range [0, 255]
                         * mantissaBits is in the range [0, 0x7f_ffff] (23 bits)
                         */
                        switch (exponentBits)
                        {
                            case 0u:
                                // subnormal number
                                realExponent = -126;
                                fullMantissa = mantissaBits;
                                break;
                            case 255u:
                                // infinity or NaN
                                if (mantissaBits == 0)
                                {
                                    return GetInfinity(signBit);
                                }
                                else
                                {
                                    return GetNotANumber(signBit);
                                }
                            default:
                                // normal number
                                realExponent = (int) exponentBits - 127;
                                // exponentBits will be in the range [1, 254]
                                // 0 and 255 were handled by other switch cases
                                fullMantissa = 0x80_0000 | mantissaBits;
                                break;
                        }
                        /*
                         * Once we calculate the real exponent and real mantissa,
                         * we want the bits to go out of scope so we do not accidentally
                         * use them anymore.
                         */
                    }
                    /*
                     * At this point we know:
                     * signBit is in the range [0, 1]
                     * realExponent is in the range [-126, 127]
                     * fullMantissa is in the range [0, 0xff_ffff] (24 bits long)
                     */

                    droppedBits = fullMantissa & DropMask;
                    // droppedBits has a bit length of MantissaBitsDropped, in the range [1, 24]
                    shortenedMantissa = fullMantissa >> MantissaBitsDropped;
                    // shortenedMantissa has a bit length of MantissaBitsRetained, in the range [0, 23]
                    /*
                     * The full mantissa goes out of scope so we do not accidentally
                     * use it instead of the shortened mantissa.
                     */
                }
                /*
                 * At this point we know:
                 * signBit is in the range [0, 1]
                 * realExponent is in the range [-126, 127]
                 * shortenedMantissa has a bit length of MantissaBitsRetained ([0, 23] bits)
                 * droppedBits has a bit length of MantissaBitsDropped ([1, 24] bits)
                 */

                // Need to determine if the dropped bits indicate we should round up.
                // We need to check the highest bit of the dropped bits.
                if (HasOneInBitPosition(droppedBits, MantissaBitsDropped - 1))
                {
                    // Round up.
                    shortenedMantissa++;
                    // Check whether the mantissa overflowed its current size.
                    // That would mean the bit higher than the highest bit is set.
                    if (HasOneInBitPosition(shortenedMantissa, MantissaBitsRetained))
                    {
                        shortenedMantissa >>= 1;
                        // This restores shortenedMantissa to at most 23 bits.
                        realExponent++;
                        // It is possible this bumps realExponent to 128.
                        if (realExponent > 127)
                        {
                            return GetInfinity(signBit);
                        }
                    }
                }
                /*
                 * At this point we know:
                 * signBit is in the range [0, 1]
                 * realExponent is in the range [-126, 127]
                 * shortenedMantissa has a bit length of MantissaBitsRetained ([0, 23] bits)
                 */

                if (realExponent < MinimumExponent)
                {
                    return GetZero(signBit);
                }

                // If MantissaBitsRetained == 0, this will always trigger.
                if (shortenedMantissa == 0)
                {
                    return GetZero(signBit);
                }
                /*
                 * At this point we know:
                 * signBit is in the range [0, 1]
                 * realExponent is in the range [MinimumExponent, 127]
                 * shortenedMantissa is in the range [1, ((1u << MantissaBitsRetained) - 1u)]
                 */

                newExponentBits = (uint) (realExponent + 127);
                // newExponentBits will be in the range [1, 254]
                newMantissaBits = shortenedMantissa << (MantissaBitsDropped - 1);
                //newMantissaBits = shortenedMantissa << (23 - MantissaBitsRetained);
                // Restores the proper length of the mantissa.
            }
            /*
             * At this point we know:
             * signBit is in the range [0, 1]
             * newExponentBits is in the range [1, 254]
             * MantissaBitsDropped < 23
             * newMantissaBits is 23 bits long and the bottom MantissaBitsDropped are all 0,
             * newMantissaBits is not 0
             */

            return AssembleBits(signBit, newExponentBits, newMantissaBits);
        }

        /// <summary>
        /// Returns a consistent representation for <c>0</c>.
        /// </summary>
        /// <param name="signBit">The sign bit.</param>
        /// <returns>A representation for either positive or negative zero.</returns>
        public uint GetZero(uint signBit)
        {
            // Return a consistent representation for zero.
            // This (rather arbitrarily) uses the exponent -126.
            // Maybe that plays nicer with hash codes, maybe not.
            return AssembleBits(signBit, 1u, 0u);
        }

        /// <summary>
        /// Returns a consistent representation for infinity.
        /// </summary>
        /// <param name="signBit">The sign bit.</param>
        /// <returns>A representation for either positive or negative infinity.</returns>
        public uint GetInfinity(uint signBit)
        {
            return AssembleBits(signBit, 255u, 0u);
        }

        /// <summary>
        /// Returns a consistent representation for "not a number".
        /// </summary>
        /// <param name="signBit">The sign bit.</param>
        /// <returns>A representation for either positive or negative NaN.</returns>
        public uint GetNotANumber(uint signBit)
        {
            // Any non-zero mantissa would be valid here.
            // Just pick one so we are consistent.
            return AssembleBits(signBit, 255u, 1u);
        }

        /// <summary>
        /// Checks whether the bit in a specific position is set to <c>1</c>.
        /// The <paramref name="bitPosition"/> is zero-based.
        /// </summary>
        /// <param name="bits">The bits to check.</param>
        /// <param name="bitPosition">The position of the bit to check.  A value of <c>0</c> means check the least significant bit.</param>
        /// <returns><see langword="true"/> if the specified bit is <c>1</c>.</returns>
        public bool HasOneInBitPosition(uint bits, int bitPosition)
        {
            if ((bitPosition & ~0x1f) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bitPosition), bitPosition, "Must be in the range [0-31]");
            }
            uint shifted = bits >> bitPosition;
            return (shifted & 1u) == 1u;
        }

        /// <summary>
        /// Extracts the sign bit from the IEEE 754 representation of a single-precision floating-point number.
        /// This will return <c>0</c> if the number was positive,
        /// and <c>1</c> if the number was negative.
        /// </summary>
        /// <param name="bits">The float bits to parse.</param>
        /// <returns>A value in the range <c>[0, 1]</c>.  <c>0</c> represents positive, <c>1</c> represents negative.</returns>
        public uint GetSignBit(uint bits)
        {
            return (bits >> 31) & 1u;
        }

        /// <summary>
        /// Extracts the exponent from the IEEE 754 representation of a single-precision floating-point number.
        /// This will return a value in the range <c>[0, 255]</c>.
        /// </summary>
        /// <param name="bits">The float bits to parse.</param>
        /// <returns>A value in the range <c>[0, 255]</c>.</returns>
        public uint GetExponentBits(uint bits)
        {
            return (bits >> 23) & 0xffu;
        }

        /// <summary>
        /// Extracts the mantissa from the IEEE 754 representation of a single-precision floating-point number.
        /// This will return a 23-bit number, which is a value in the range <c>[0, 0x7fffff]</c>.
        /// </summary>
        /// <param name="bits">The float bits to parse.</param>
        /// <returns>A 23-bit number in the range <c>[0, 0x7fffff]</c>.</returns>
        public uint GetMantissaBits(uint bits)
        {
            return bits & 0x7f_ffffu;
        }

        internal static uint AssembleBits(uint signBit, uint exponentBits, uint mantissaBits)
        {
            if (signBit > 1u)
            {
                throw new ArgumentOutOfRangeException(nameof(signBit), signBit, "Must be 0 or 1.");
            }
            if (exponentBits > 0xffu)
            {
                throw new ArgumentOutOfRangeException(nameof(exponentBits), exponentBits, "Must be in the range [0, 0xff].");
            }
            if (mantissaBits > 0x7f_ffffu)
            {
                throw new ArgumentOutOfRangeException(nameof(mantissaBits), mantissaBits, "Must be in the range [0, 0x7fffff].");
            }

            return ((signBit & 1u) << 31)
                | ((exponentBits & 0xffu) << 23)
                | (mantissaBits & 0x7f_ffff);
        }
    }
}
