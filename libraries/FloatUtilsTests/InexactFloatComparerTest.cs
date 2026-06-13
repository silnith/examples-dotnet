using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;

namespace Silnith.FloatUtils.Tests
{
    [TestClass]
    public class InexactFloatComparerTest
    {

        [TestClass]
        public class GetNormalizedBits
        {

            [TestMethod]
            public void TestGetNormalizedBits_PositiveZero()
            {
                FloatComparerSettings settings = new()
                {
                    MantissaBitsDropped = 1,
                    MinimumExponent = -126,
                };
                InexactFloatComparer comparer = new(Options.Create(settings));

                uint actual = comparer.GetNormalizedBits(0.0f);
                Assert.AreEqual(0x0080_0000u, actual);
            }

            [TestMethod]
            public void TestGetNormalizedBits_NegativeZero()
            {
                FloatComparerSettings settings = new()
                {
                    MantissaBitsDropped = 1,
                    MinimumExponent = -126,
                };
                InexactFloatComparer comparer = new(Options.Create(settings));

                uint actual = comparer.GetNormalizedBits(-0.0f);
                Assert.AreEqual(0x8080_0000u, actual);
            }

            [TestMethod]
            public void TestGetNormalizedBits_PositiveInfinity()
            {
                FloatComparerSettings settings = new()
                {
                    MantissaBitsDropped = 1,
                    MinimumExponent = -126,
                };
                InexactFloatComparer comparer = new(Options.Create(settings));

                uint actual = comparer.GetNormalizedBits(float.PositiveInfinity);
                Assert.AreEqual(0x7f80_0000u, actual);
            }

            [TestMethod]
            public void TestGetNormalizedBits_NegativeInfinity()
            {
                FloatComparerSettings settings = new()
                {
                    MantissaBitsDropped = 1,
                    MinimumExponent = -126,
                };
                InexactFloatComparer comparer = new(Options.Create(settings));

                uint actual = comparer.GetNormalizedBits(float.NegativeInfinity);
                Assert.AreEqual(0xff80_0000u, actual);
            }

            [TestMethod]
            public void TestGetNormalizedBits_PositiveNaN()
            {
                FloatComparerSettings settings = new()
                {
                    MantissaBitsDropped = 1,
                    MinimumExponent = -126,
                };
                InexactFloatComparer comparer = new(Options.Create(settings));

                uint actual = comparer.GetNormalizedBits(BitConverter.Int32BitsToSingle(int.MaxValue));
                Assert.AreEqual(0x7f80_0001u, actual);
            }

            [TestMethod]
            public void TestGetNormalizedBits_NegativeNaN()
            {
                FloatComparerSettings settings = new()
                {
                    MantissaBitsDropped = 1,
                    MinimumExponent = -126,
                };
                InexactFloatComparer comparer = new(Options.Create(settings));

                uint actual = comparer.GetNormalizedBits(BitConverter.Int32BitsToSingle(-1));
                Assert.AreEqual(0xff80_0001u, actual);
            }

            #region Subnormal

            [TestMethod]
            public void TestGetNormalizedBits_PositiveSubnormalLow_MinimalRoundDown()
            {
                FloatComparerSettings settings = new()
                {
                    MantissaBitsDropped = 1,
                    MinimumExponent = -126,
                };
                InexactFloatComparer comparer = new(Options.Create(settings));

                uint actual = comparer.GetNormalizedBits(BitConverter.Int32BitsToSingle(0x0000_00fe));
                uint expected = (0u << 31)
                    | (1u << 23)
                    | 0x00_007fu;
                Assert.AreEqual(expected, actual);
            }

            [TestMethod]
            public void TestGetNormalizedBits_PositiveSubnormalLow_MinimalRoundUp()
            {
                FloatComparerSettings settings = new()
                {
                    MantissaBitsDropped = 1,
                    MinimumExponent = -126,
                };
                InexactFloatComparer comparer = new(Options.Create(settings));

                uint actual = comparer.GetNormalizedBits(BitConverter.Int32BitsToSingle(0x0000_00ff));
                uint expected = (0u << 31)
                    | (1u << 23)
                    | 0x00_0080u;
                Assert.AreEqual(expected, actual);
            }

            [TestMethod]
            public void TestGetNormalizedBits_PositiveSubnormalHigh_MinimalRoundDown()
            {
                FloatComparerSettings settings = new()
                {
                    MantissaBitsDropped = 1,
                    MinimumExponent = -126,
                };
                InexactFloatComparer comparer = new(Options.Create(settings));

                uint actual = comparer.GetNormalizedBits(BitConverter.Int32BitsToSingle(0x007f_fffe));
                uint expected = (0u << 31)
                    | (1u << 23)
                    | 0x3f_ffffu;
                Assert.AreEqual(expected, actual);
            }

            /// <summary>
            /// A subnormal number does not gain the extra <c>1</c> prefix, so even
            /// when rounded up it cannot overflow to increase its exponent.
            /// </summary>
            [TestMethod]
            public void TestGetNormalizedBits_PositiveSubnormalHigh_MinimalRoundUp()
            {
                FloatComparerSettings settings = new()
                {
                    MantissaBitsDropped = 1,
                    MinimumExponent = -126,
                };
                InexactFloatComparer comparer = new(Options.Create(settings));

                uint actual = comparer.GetNormalizedBits(BitConverter.Int32BitsToSingle(0x007f_ffff));
                uint expected = (0u << 31)
                    | (1u << 23)
                    | 0x40_0000u;
                Assert.AreEqual(expected, actual);
            }

            #endregion

            #region Normal

            [TestMethod]
            public void TestGetNormalizedBits_PositiveNormalLow_MinimalRoundDown()
            {
                FloatComparerSettings settings = new()
                {
                    MantissaBitsDropped = 1,
                    MinimumExponent = -126,
                };
                InexactFloatComparer comparer = new(Options.Create(settings));

                uint actual = comparer.GetNormalizedBits(BitConverter.Int32BitsToSingle(0x0080_00fe));
                uint expected = (0u << 31)
                    | (1u << 23)
                    | 0x40_007fu;
                Assert.AreEqual(expected, actual);
            }

            [TestMethod]
            public void TestGetNormalizedBits_PositiveNormalLow_MinimalRoundUp()
            {
                FloatComparerSettings settings = new()
                {
                    MantissaBitsDropped = 1,
                    MinimumExponent = -126,
                };
                InexactFloatComparer comparer = new(Options.Create(settings));

                uint actual = comparer.GetNormalizedBits(BitConverter.Int32BitsToSingle(0x0080_00ff));
                uint expected = (0u << 31)
                    | (1u << 23)
                    | 0x40_0080u;
                Assert.AreEqual(expected, actual);
            }

            [TestMethod]
            public void TestGetNormalizedBits_PositiveNormalMostlyLow_MinimalRoundDown()
            {
                FloatComparerSettings settings = new()
                {
                    MantissaBitsDropped = 1,
                    MinimumExponent = -126,
                };
                InexactFloatComparer comparer = new(Options.Create(settings));

                uint actual = comparer.GetNormalizedBits(BitConverter.Int32BitsToSingle(0x00ff_fffe));
                uint expected = (0u << 31)
                    | (1u << 23)
                    | 0x7f_ffffu;
                Assert.AreEqual(expected, actual);
            }

            [TestMethod]
            public void TestGetNormalizedBits_PositiveNormalMostlyLow_MinimalRoundUp()
            {
                FloatComparerSettings settings = new()
                {
                    MantissaBitsDropped = 1,
                    MinimumExponent = -126,
                };
                InexactFloatComparer comparer = new(Options.Create(settings));

                uint actual = comparer.GetNormalizedBits(BitConverter.Int32BitsToSingle(0x00ff_ffff));
                uint expected = (0u << 31)
                    | (2u << 23)
                    | 0x40_0000u;
                Assert.AreEqual(expected, actual);
            }

            /// <summary>
            /// Checks that the non-zero value gets rounded, but then the minimum
            /// exponent clamps it to zero.
            /// </summary>
            [TestMethod]
            public void TestGetNormalizedBits_PositiveNormalMostlyLow_NotQuiteMinimalRoundDown()
            {
                FloatComparerSettings settings = new()
                {
                    MantissaBitsDropped = 1,
                    MinimumExponent = -125,
                };
                InexactFloatComparer comparer = new(Options.Create(settings));

                uint actual = comparer.GetNormalizedBits(BitConverter.Int32BitsToSingle(0x00ff_fffe));
                uint expected = (0u << 31)
                    | (1u << 23)
                    | 0x00_0000u;
                Assert.AreEqual(expected, actual);
            }

            /// <summary>
            /// Checks that the non-zero value gets rounded up, and that rounding
            /// bumps it high enough that it passes the minimum exponent threshold.
            /// </summary>
            [TestMethod]
            public void TestGetNormalizedBits_PositiveNormalMostlyLow_NotQuiteMinimalRoundUp()
            {
                FloatComparerSettings settings = new()
                {
                    MantissaBitsDropped = 1,
                    MinimumExponent = -125,
                };
                InexactFloatComparer comparer = new(Options.Create(settings));

                uint actual = comparer.GetNormalizedBits(BitConverter.Int32BitsToSingle(0x00ff_ffff));
                uint expected = (0u << 31)
                    | (2u << 23)
                    | 0x40_0000u;
                Assert.AreEqual(expected, actual);
            }

            #endregion

        }

        [TestClass]
        public class HasOneInBitPosition
        {

            [TestMethod]
            public void TestHasOneInBitPosition_ByThirtyTwo()
            {
                Assert.ThrowsException<ArgumentOutOfRangeException>(() => InexactFloatComparer.HasOneInBitPosition(0u, 32));
            }

            [TestMethod]
            public void TestHasOneInBitPosition_ByNegativeOne()
            {
                Assert.ThrowsException<ArgumentOutOfRangeException>(() => InexactFloatComparer.HasOneInBitPosition(0u, -1));
            }

            [TestMethod]
            public void TestHasOneInBitPosition_ByMin()
            {
                Assert.ThrowsException<ArgumentOutOfRangeException>(() => InexactFloatComparer.HasOneInBitPosition(0u, int.MinValue));
            }

            [TestMethod]
            public void TestHasOneInBitPosition_ByMax()
            {
                Assert.ThrowsException<ArgumentOutOfRangeException>(() => InexactFloatComparer.HasOneInBitPosition(0u, int.MaxValue));
            }

            [TestMethod]
            public void TestHasOneInBitPosition_Zero_Zero()
            {
                Assert.IsFalse(InexactFloatComparer.HasOneInBitPosition(0u, 0));
            }

            [TestMethod]
            public void TestHasOneInBitPosition_Zero_One()
            {
                Assert.IsFalse(InexactFloatComparer.HasOneInBitPosition(0u, 1));
            }

            [TestMethod]
            public void TestHasOneInBitPosition_One_Zero()
            {
                Assert.IsTrue(InexactFloatComparer.HasOneInBitPosition(1u, 0));
            }

            [TestMethod]
            public void TestHasOneInBitPosition_One_One()
            {
                Assert.IsFalse(InexactFloatComparer.HasOneInBitPosition(1u, 1));
            }

            [TestMethod]
            public void TestHasOneInBitPosition_One_Two()
            {
                Assert.IsFalse(InexactFloatComparer.HasOneInBitPosition(1u, 2));
            }

            [TestMethod]
            public void TestHasOneInBitPosition_Two_Zero()
            {
                Assert.IsFalse(InexactFloatComparer.HasOneInBitPosition(2u, 0));
            }

            [TestMethod]
            public void TestHasOneInBitPosition_Two_One()
            {
                Assert.IsTrue(InexactFloatComparer.HasOneInBitPosition(2u, 1));
            }

            [TestMethod]
            public void TestHasOneInBitPosition_Two_Two()
            {
                Assert.IsFalse(InexactFloatComparer.HasOneInBitPosition(2u, 2));
            }

            [TestMethod]
            public void TestHasOneInBitPosition_Max_ThirtyOne()
            {
                Assert.IsTrue(InexactFloatComparer.HasOneInBitPosition(uint.MaxValue, 31));
            }

        }

        #region GetConstants

        /// <summary>
        /// Notice that this comparer does not use the "correct" representation for zero.
        /// This is intentional.
        /// </summary>
        [TestMethod]
        public void TestGetZero_Positive()
        {
            Assert.AreEqual(0x0080_0000u, InexactFloatComparer.GetZero(0u));
        }

        /// <summary>
        /// Notice that this comparer does not use the "correct" representation for zero.
        /// This is intentional.
        /// </summary>
        [TestMethod]
        public void TestGetZero_Negative()
        {
            Assert.AreEqual(0x8080_0000u, InexactFloatComparer.GetZero(1u));
        }

        [TestMethod]
        public void TestGetInfinity_Positive()
        {
            Assert.AreEqual(0x7f80_0000u, InexactFloatComparer.GetInfinity(0u));
        }

        [TestMethod]
        public void TestGetInfinity_Negative()
        {
            Assert.AreEqual(0xff80_0000u, InexactFloatComparer.GetInfinity(1u));
        }

        [TestMethod]
        public void TestGetNotANumber_Positive()
        {
            Assert.AreEqual(0x7f80_0001u, InexactFloatComparer.GetNotANumber(0u));
        }

        [TestMethod]
        public void TestGetNotANumber_Negative()
        {
            Assert.AreEqual(0xff80_0001u, InexactFloatComparer.GetNotANumber(1u));
        }

        #endregion

        [TestClass]
        public class GetSignBit
        {

            [TestMethod]
            public void TestGetSignBit_Positive_Zero()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(0.0f);
                Assert.AreEqual(0u, InexactFloatComparer.GetSignBit(bits));
            }

            [TestMethod]
            public void TestGetSignBit_Negative_Zero()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(-0.0f);
                Assert.AreEqual(1u, InexactFloatComparer.GetSignBit(bits));
            }

            [TestMethod]
            public void TestGetSignBit_Positive_One()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(1.0f);
                Assert.AreEqual(0u, InexactFloatComparer.GetSignBit(bits));
            }

            [TestMethod]
            public void TestGetSignBit_Negative_One()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(-1.0f);
                Assert.AreEqual(1u, InexactFloatComparer.GetSignBit(bits));
            }

            [TestMethod]
            public void TestGetSignBit_Positive_Infinity()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(float.PositiveInfinity);
                Assert.AreEqual(0u, InexactFloatComparer.GetSignBit(bits));
            }

            [TestMethod]
            public void TestGetSignBit_Negative_Infinity()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(float.NegativeInfinity);
                Assert.AreEqual(1u, InexactFloatComparer.GetSignBit(bits));
            }

            [TestMethod]
            public void TestGetSignBit_Positive_NaN()
            {
                Assert.AreEqual(0u, InexactFloatComparer.GetSignBit(0x7f80_0001u));
            }

            [TestMethod]
            public void TestGetSignBit_Negative_NaN()
            {
                Assert.AreEqual(1u, InexactFloatComparer.GetSignBit(0xffff_ffff));
            }

        }

        [TestClass]
        public class GetExponentBits
        {

            [TestMethod]
            public void TestGetExponentBits_Positive_Zero()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(0.0f);
                Assert.AreEqual(0u, InexactFloatComparer.GetExponentBits(bits));
            }

            [TestMethod]
            public void TestGetExponentBits_Negative_Zero()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(-0.0f);
                Assert.AreEqual(0u, InexactFloatComparer.GetExponentBits(bits));
            }

            [TestMethod]
            public void TestGetExponentBits_Positive_SmallValue()
            {
                Assert.AreEqual(1u, InexactFloatComparer.GetExponentBits(0x0080_0000u));
            }

            [TestMethod]
            public void TestGetExponentBits_Negative_SmallValue()
            {
                Assert.AreEqual(1u, InexactFloatComparer.GetExponentBits(0x8080_0000u));
            }

            [TestMethod]
            public void TestGetExponentBits_Positive_OneHalf()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(0.5f);
                Assert.AreEqual(126u, InexactFloatComparer.GetExponentBits(bits));
            }

            [TestMethod]
            public void TestGetExponentBits_Negative_OneHalf()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(-0.5f);
                Assert.AreEqual(126u, InexactFloatComparer.GetExponentBits(bits));
            }

            [TestMethod]
            public void TestGetExponentBits_Positive_One()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(1.0f);
                Assert.AreEqual(127u, InexactFloatComparer.GetExponentBits(bits));
            }

            [TestMethod]
            public void TestGetExponentBits_Negative_One()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(-1.0f);
                Assert.AreEqual(127u, InexactFloatComparer.GetExponentBits(bits));
            }

            [TestMethod]
            public void TestGetExponentBits_Positive_Two()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(2.0f);
                Assert.AreEqual(128u, InexactFloatComparer.GetExponentBits(bits));
            }

            [TestMethod]
            public void TestGetExponentBits_Negative_Two()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(-2.0f);
                Assert.AreEqual(128u, InexactFloatComparer.GetExponentBits(bits));
            }

            [TestMethod]
            public void TestGetExponentBits_Positive_LargeValue()
            {
                Assert.AreEqual(254u, InexactFloatComparer.GetExponentBits(0x7f00_0000u));
            }

            [TestMethod]
            public void TestGetExponentBits_Negative_LargeValue()
            {
                Assert.AreEqual(254u, InexactFloatComparer.GetExponentBits(0xff00_0000u));
            }

            [TestMethod]
            public void TestGetExponentBits_Positive_Infinity()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(float.PositiveInfinity);
                Assert.AreEqual(255u, InexactFloatComparer.GetExponentBits(bits));
            }

            [TestMethod]
            public void TestGetExponentBits_Negative_Infinity()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(float.NegativeInfinity);
                Assert.AreEqual(255u, InexactFloatComparer.GetExponentBits(bits));
            }

            [TestMethod]
            public void TestGetExponentBits_NaN()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(float.NaN);
                Assert.AreEqual(255u, InexactFloatComparer.GetExponentBits(bits));
            }

        }

        [TestClass]
        public class GetMantissaBits
        {

            [TestMethod]
            public void TestGetMantissaBits_Positive_Zero()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(0.0f);
                Assert.AreEqual(0u, InexactFloatComparer.GetMantissaBits(bits));
            }

            [TestMethod]
            public void TestGetMantissaBits_Negative_Zero()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(-0.0f);
                Assert.AreEqual(0u, InexactFloatComparer.GetMantissaBits(bits));
            }

            [TestMethod]
            public void TestGetMantissaBits_Positive_SmallValue()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(float.Epsilon);
                Assert.AreEqual(1u, InexactFloatComparer.GetMantissaBits(bits));
            }

            [TestMethod]
            public void TestGetMantissaBits_Negative_SmallValue()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(-float.Epsilon);
                Assert.AreEqual(1u, InexactFloatComparer.GetMantissaBits(bits));
            }

            [TestMethod]
            public void TestGetMantissaBits_Positive_OneHalf()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(0.5f);
                Assert.AreEqual(0u, InexactFloatComparer.GetMantissaBits(bits));
            }

            [TestMethod]
            public void TestGetMantissaBits_Negative_OneHalf()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(-0.5f);
                Assert.AreEqual(0u, InexactFloatComparer.GetMantissaBits(bits));
            }

            [TestMethod]
            public void TestGetMantissaBits_Positive_One()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(1.0f);
                Assert.AreEqual(0u, InexactFloatComparer.GetMantissaBits(bits));
            }

            [TestMethod]
            public void TestGetMantissaBits_Negative_One()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(-1.0f);
                Assert.AreEqual(0u, InexactFloatComparer.GetMantissaBits(bits));
            }

            [TestMethod]
            public void TestGetMantissaBits_Positive_OneAndOneHalf()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(1.5f);
                Assert.AreEqual(0x40_0000u, InexactFloatComparer.GetMantissaBits(bits));
            }

            [TestMethod]
            public void TestGetMantissaBits_Negative_OneAndOneHalf()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(-1.5f);
                Assert.AreEqual(0x40_0000u, InexactFloatComparer.GetMantissaBits(bits));
            }

            [TestMethod]
            public void TestGetMantissaBits_Positive_Two()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(2.0f);
                Assert.AreEqual(0u, InexactFloatComparer.GetMantissaBits(bits));
            }

            [TestMethod]
            public void TestGetMantissaBits_Negative_Two()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(-2.0f);
                Assert.AreEqual(0u, InexactFloatComparer.GetMantissaBits(bits));
            }

            [TestMethod]
            public void TestGetMantissaBits_Positive_TwoAndOneHalf()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(2.5f);
                Assert.AreEqual(0x20_0000u, InexactFloatComparer.GetMantissaBits(bits));
            }

            [TestMethod]
            public void TestGetMantissaBits_Negative_TwoAndOneHalf()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(-2.5f);
                Assert.AreEqual(0x20_0000u, InexactFloatComparer.GetMantissaBits(bits));
            }

            [TestMethod]
            public void TestGetMantissaBits_Positive_LargeValue()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(float.MaxValue);
                Assert.AreEqual(0x7f_ffffu, InexactFloatComparer.GetMantissaBits(bits));
            }

            [TestMethod]
            public void TestGetMantissaBits_Negative_LargeValue()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(float.MinValue);
                Assert.AreEqual(0x7f_ffffu, InexactFloatComparer.GetMantissaBits(bits));
            }

            [TestMethod]
            public void TestGetMantissaBits_Positive_Infinity()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(float.PositiveInfinity);
                Assert.AreEqual(0u, InexactFloatComparer.GetMantissaBits(bits));
            }

            [TestMethod]
            public void TestGetMantissaBits_Negative_Infinity()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(float.NegativeInfinity);
                Assert.AreEqual(0u, InexactFloatComparer.GetMantissaBits(bits));
            }

            [TestMethod]
            public void TestGetMantissaBits_NaN()
            {
                uint bits = (uint) BitConverter.SingleToInt32Bits(float.NaN);
                Assert.AreNotEqual(0u, InexactFloatComparer.GetMantissaBits(bits));
            }

        }

        [TestClass]
        public class AssembleBits
        {

            [TestMethod]
            public void TestAssembleBits_Positive_Zero()
            {
                uint actual = InexactFloatComparer.AssembleBits(0u, 0u, 0u);
                uint expected = 0u;
                Assert.AreEqual(expected, actual);
            }

            [TestMethod]
            public void TestAssembleBits_Negative_Zero()
            {
                uint actual = InexactFloatComparer.AssembleBits(1u, 0u, 0u);
                uint expected = 0x8000_0000u;
                Assert.AreEqual(expected, actual);
            }

            [TestMethod]
            public void TestAssembleBits_Positive_SmallExponent()
            {
                uint actual = InexactFloatComparer.AssembleBits(0u, 1u, 0u);
                uint expected = 0x0080_0000u;
                Assert.AreEqual(expected, actual);
            }

            [TestMethod]
            public void TestAssembleBits_Negative_SmallExponent()
            {
                uint actual = InexactFloatComparer.AssembleBits(1u, 1u, 0u);
                uint expected = 0x8080_0000u;
                Assert.AreEqual(expected, actual);
            }

            [TestMethod]
            public void TestAssembleBits_Positive_LargeExponent()
            {
                uint actual = InexactFloatComparer.AssembleBits(0u, 0xffu, 0u);
                uint expected = 0x7f80_0000u;
                Assert.AreEqual(expected, actual);
            }

            [TestMethod]
            public void TestAssembleBits_Negative_LargeExponent()
            {
                uint actual = InexactFloatComparer.AssembleBits(1u, 0xffu, 0u);
                uint expected = 0xff80_0000u;
                Assert.AreEqual(expected, actual);
            }

            [TestMethod]
            public void TestAssembleBits_Positive_SmallMantissa()
            {
                uint actual = InexactFloatComparer.AssembleBits(0u, 0u, 1u);
                uint expected = 0x0000_0001u;
                Assert.AreEqual(expected, actual);
            }

            [TestMethod]
            public void TestAssembleBits_Negative_SmallMantissa()
            {
                uint actual = InexactFloatComparer.AssembleBits(1u, 0u, 1u);
                uint expected = 0x8000_0001u;
                Assert.AreEqual(expected, actual);
            }

            [TestMethod]
            public void TestAssembleBits_Positive_SmallExponent_SmallMantissa()
            {
                uint actual = InexactFloatComparer.AssembleBits(0u, 1u, 1u);
                uint expected = 0x0080_0001u;
                Assert.AreEqual(expected, actual);
            }

            [TestMethod]
            public void TestAssembleBits_Negative_SmallExponent_SmallMantissa()
            {
                uint actual = InexactFloatComparer.AssembleBits(1u, 1u, 1u);
                uint expected = 0x8080_0001u;
                Assert.AreEqual(expected, actual);
            }

            [TestMethod]
            public void TestAssembleBits_Positive_LargeExponent_SmallMantissa()
            {
                uint actual = InexactFloatComparer.AssembleBits(0u, 0xffu, 1u);
                uint expected = 0x7f80_0001u;
                Assert.AreEqual(expected, actual);
            }

            [TestMethod]
            public void TestAssembleBits_Negative_LargeExponent_SmallMantissa()
            {
                uint actual = InexactFloatComparer.AssembleBits(1u, 0xffu, 1u);
                uint expected = 0xff80_0001u;
                Assert.AreEqual(expected, actual);
            }

            [TestMethod]
            public void TestAssembleBits_Positive_LargeMantissa()
            {
                uint actual = InexactFloatComparer.AssembleBits(0u, 0u, 0x7f_ffffu);
                uint expected = 0x007f_ffffu;
                Assert.AreEqual(expected, actual);
            }

            [TestMethod]
            public void TestAssembleBits_Negative_LargeMantissa()
            {
                uint actual = InexactFloatComparer.AssembleBits(1u, 0u, 0x7f_ffffu);
                uint expected = 0x807f_ffffu;
                Assert.AreEqual(expected, actual);
            }

            [TestMethod]
            public void TestAssembleBits_Positive_SmallExponent_LargeMantissa()
            {
                uint actual = InexactFloatComparer.AssembleBits(0u, 1u, 0x7f_ffffu);
                uint expected = 0x00ff_ffffu;
                Assert.AreEqual(expected, actual);
            }

            [TestMethod]
            public void TestAssembleBits_Negative_SmallExponent_LargeMantissa()
            {
                uint actual = InexactFloatComparer.AssembleBits(1u, 1u, 0x7f_ffffu);
                uint expected = 0x80ff_ffffu;
                Assert.AreEqual(expected, actual);
            }

            [TestMethod]
            public void TestAssembleBits_Positive_LargeExponent_LargeMantissa()
            {
                uint actual = InexactFloatComparer.AssembleBits(0u, 0xffu, 0x7f_ffffu);
                uint expected = 0x7fff_ffffu;
                Assert.AreEqual(expected, actual);
            }

            [TestMethod]
            public void TestAssembleBits_Negative_LargeExponent_LargeMantissa()
            {
                uint actual = InexactFloatComparer.AssembleBits(1u, 0xffu, 0x7f_ffffu);
                uint expected = 0xffff_ffffu;
                Assert.AreEqual(expected, actual);
            }

            [TestMethod]
            public void TestAssembleBits_SignOutOfRange_Small()
            {
                Assert.ThrowsException<ArgumentOutOfRangeException>(() => InexactFloatComparer.AssembleBits(2u, 0u, 0u));
            }

            [TestMethod]
            public void TestAssembleBits_SignOutOfRange_Large()
            {
                Assert.ThrowsException<ArgumentOutOfRangeException>(() => InexactFloatComparer.AssembleBits(0xffff_ffffu, 0u, 0u));
            }

            [TestMethod]
            public void TestAssembleBits_ExponentOutOfRange_Small()
            {
                Assert.ThrowsException<ArgumentOutOfRangeException>(() => InexactFloatComparer.AssembleBits(0u, 0x100u, 0u));
            }

            [TestMethod]
            public void TestAssembleBits_ExponentOutOfRange_Large()
            {
                Assert.ThrowsException<ArgumentOutOfRangeException>(() => InexactFloatComparer.AssembleBits(0u, 0xffff_ffffu, 0u));
            }

            [TestMethod]
            public void TestAssembleBits_MantissaOutOfRange_Small()
            {
                Assert.ThrowsException<ArgumentOutOfRangeException>(() => InexactFloatComparer.AssembleBits(0u, 0u, 0x80_0000u));
            }

            [TestMethod]
            public void TestAssembleBits_MantissaOutOfRange_Large()
            {
                Assert.ThrowsException<ArgumentOutOfRangeException>(() => InexactFloatComparer.AssembleBits(0u, 0u, 0xffff_ffffu));
            }

        }

    }
}
