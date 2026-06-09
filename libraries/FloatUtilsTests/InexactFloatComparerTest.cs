using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;

namespace Silnith.FloatUtils.Tests
{
    [TestClass]
    public class InexactFloatComparerTest
    {
        [TestMethod]
        public void TestBehavior()
        {
            int bits = -1;
            Assert.AreEqual(0xffffffffu, (uint)bits);
        }

        #region HasOneInBitPosition

        [TestMethod]
        public void TestHasOneInBitPosition_ByThirtyTwo()
        {
            IOptions<FloatComparerSettings> options = Options.Create(new FloatComparerSettings()
            {
                MantissaBitsDropped = 1,
                MinimumExponent = -126,
            });
            InexactFloatComparer floatComparer = new(options);

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => floatComparer.HasOneInBitPosition(0u, 32));
        }

        [TestMethod]
        public void TestHasOneInBitPosition_ByNegativeOne()
        {
            IOptions<FloatComparerSettings> options = Options.Create(new FloatComparerSettings()
            {
                MantissaBitsDropped = 1,
                MinimumExponent = -126,
            });
            InexactFloatComparer floatComparer = new(options);

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => floatComparer.HasOneInBitPosition(0u, -1));
        }

        [TestMethod]
        public void TestHasOneInBitPosition_ByMin()
        {
            IOptions<FloatComparerSettings> options = Options.Create(new FloatComparerSettings()
            {
                MantissaBitsDropped = 1,
                MinimumExponent = -126,
            });
            InexactFloatComparer floatComparer = new(options);

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => floatComparer.HasOneInBitPosition(0u, int.MinValue));
        }

        [TestMethod]
        public void TestHasOneInBitPosition_ByMax()
        {
            IOptions<FloatComparerSettings> options = Options.Create(new FloatComparerSettings()
            {
                MantissaBitsDropped = 1,
                MinimumExponent = -126,
            });
            InexactFloatComparer floatComparer = new(options);

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => floatComparer.HasOneInBitPosition(0u, int.MaxValue));
        }

        [TestMethod]
        public void TestHasOneInBitPosition_Zero_Zero()
        {
            IOptions<FloatComparerSettings> options = Options.Create(new FloatComparerSettings()
            {
                MantissaBitsDropped = 1,
                MinimumExponent = -126,
            });
            InexactFloatComparer floatComparer = new(options);

            Assert.IsFalse(floatComparer.HasOneInBitPosition(0u, 0));
        }

        [TestMethod]
        public void TestHasOneInBitPosition_Zero_One()
        {
            IOptions<FloatComparerSettings> options = Options.Create(new FloatComparerSettings()
            {
                MantissaBitsDropped = 1,
                MinimumExponent = -126,
            });
            InexactFloatComparer floatComparer = new(options);

            Assert.IsFalse(floatComparer.HasOneInBitPosition(0u, 1));
        }

        [TestMethod]
        public void TestHasOneInBitPosition_One_Zero()
        {
            IOptions<FloatComparerSettings> options = Options.Create(new FloatComparerSettings()
            {
                MantissaBitsDropped = 1,
                MinimumExponent = -126,
            });
            InexactFloatComparer floatComparer = new(options);

            Assert.IsTrue(floatComparer.HasOneInBitPosition(1u, 0));
        }

        [TestMethod]
        public void TestHasOneInBitPosition_One_One()
        {
            IOptions<FloatComparerSettings> options = Options.Create(new FloatComparerSettings()
            {
                MantissaBitsDropped = 1,
                MinimumExponent = -126,
            });
            InexactFloatComparer floatComparer = new(options);

            Assert.IsFalse(floatComparer.HasOneInBitPosition(1u, 1));
        }

        [TestMethod]
        public void TestHasOneInBitPosition_One_Two()
        {
            IOptions<FloatComparerSettings> options = Options.Create(new FloatComparerSettings()
            {
                MantissaBitsDropped = 1,
                MinimumExponent = -126,
            });
            InexactFloatComparer floatComparer = new(options);

            Assert.IsFalse(floatComparer.HasOneInBitPosition(1u, 2));
        }

        [TestMethod]
        public void TestHasOneInBitPosition_Two_Zero()
        {
            IOptions<FloatComparerSettings> options = Options.Create(new FloatComparerSettings()
            {
                MantissaBitsDropped = 1,
                MinimumExponent = -126,
            });
            InexactFloatComparer floatComparer = new(options);

            Assert.IsFalse(floatComparer.HasOneInBitPosition(2u, 0));
        }

        [TestMethod]
        public void TestHasOneInBitPosition_Two_One()
        {
            IOptions<FloatComparerSettings> options = Options.Create(new FloatComparerSettings()
            {
                MantissaBitsDropped = 1,
                MinimumExponent = -126,
            });
            InexactFloatComparer floatComparer = new(options);

            Assert.IsTrue(floatComparer.HasOneInBitPosition(2u, 1));
        }

        [TestMethod]
        public void TestHasOneInBitPosition_Two_Two()
        {
            IOptions<FloatComparerSettings> options = Options.Create(new FloatComparerSettings()
            {
                MantissaBitsDropped = 1,
                MinimumExponent = -126,
            });
            InexactFloatComparer floatComparer = new(options);

            Assert.IsFalse(floatComparer.HasOneInBitPosition(2u, 2));
        }

        [TestMethod]
        public void TestHasOneInBitPosition_Max_ThirtyOne()
        {
            IOptions<FloatComparerSettings> options = Options.Create(new FloatComparerSettings()
            {
                MantissaBitsDropped = 1,
                MinimumExponent = -126,
            });
            InexactFloatComparer floatComparer = new(options);

            Assert.IsTrue(floatComparer.HasOneInBitPosition(uint.MaxValue, 31));
        }

        #endregion

        #region GetBits

        #region GetSignBit

        [TestMethod]
        public void TestGetSignBit_Zero()
        {
            IOptions<FloatComparerSettings> options = Options.Create(new FloatComparerSettings()
            {
                MantissaBitsDropped = 1,
                MinimumExponent = -126,
            });
            InexactFloatComparer floatComparer = new(options);

            uint bits = (uint)BitConverter.SingleToInt32Bits(0.0f);
            Assert.AreEqual(0u, floatComparer.GetSignBit(bits));
        }

        [TestMethod]
        public void TestGetSignBit_One()
        {
            IOptions<FloatComparerSettings> options = Options.Create(new FloatComparerSettings()
            {
                MantissaBitsDropped = 1,
                MinimumExponent = -126,
            });
            InexactFloatComparer floatComparer = new(options);

            uint bits = (uint)BitConverter.SingleToInt32Bits(-0.0f);
            Assert.AreEqual(1u, floatComparer.GetSignBit(bits));
        }

        #endregion

        #region GetExponentBits

        [TestMethod]
        public void TestGetExponentBits_Zero()
        {
            IOptions<FloatComparerSettings> options = Options.Create(new FloatComparerSettings()
            {
                MantissaBitsDropped = 1,
                MinimumExponent = -126,
            });
            InexactFloatComparer floatComparer = new(options);

            uint bits = (uint)BitConverter.SingleToInt32Bits(0.0f);
            Assert.AreEqual(0u, floatComparer.GetExponentBits(bits));
        }

        [TestMethod]
        public void TestGetExponentBits_Infinity()
        {
            IOptions<FloatComparerSettings> options = Options.Create(new FloatComparerSettings()
            {
                MantissaBitsDropped = 1,
                MinimumExponent = -126,
            });
            InexactFloatComparer floatComparer = new(options);

            uint bits = (uint)BitConverter.SingleToInt32Bits(float.PositiveInfinity);
            Assert.AreEqual(255u, floatComparer.GetExponentBits(bits));
        }

        [TestMethod]
        public void TestGetExponentBits_NaN()
        {
            IOptions<FloatComparerSettings> options = Options.Create(new FloatComparerSettings()
            {
                MantissaBitsDropped = 1,
                MinimumExponent = -126,
            });
            InexactFloatComparer floatComparer = new(options);

            uint bits = (uint)BitConverter.SingleToInt32Bits(float.NaN);
            Assert.AreEqual(255u, floatComparer.GetExponentBits(bits));
        }

        #endregion

        #region GetMantissaBits

        [TestMethod]
        public void TestGetMantissaBits_Zero()
        {
            IOptions<FloatComparerSettings> options = Options.Create(new FloatComparerSettings()
            {
                MantissaBitsDropped = 1,
                MinimumExponent = -126,
            });
            InexactFloatComparer floatComparer = new(options);

            uint bits = (uint)BitConverter.SingleToInt32Bits(0.0f);
            Assert.AreEqual(0u, floatComparer.GetMantissaBits(bits));
        }

        [TestMethod]
        public void TestGetMantissaBits_Infinity()
        {
            IOptions<FloatComparerSettings> options = Options.Create(new FloatComparerSettings()
            {
                MantissaBitsDropped = 1,
                MinimumExponent = -126,
            });
            InexactFloatComparer floatComparer = new(options);

            uint bits = (uint)BitConverter.SingleToInt32Bits(float.PositiveInfinity);
            Assert.AreEqual(0u, floatComparer.GetMantissaBits(bits));
        }

        [TestMethod]
        public void TestGetMantissaBits_NaN()
        {
            IOptions<FloatComparerSettings> options = Options.Create(new FloatComparerSettings()
            {
                MantissaBitsDropped = 1,
                MinimumExponent = -126,
            });
            InexactFloatComparer floatComparer = new(options);

            uint bits = (uint)BitConverter.SingleToInt32Bits(float.NaN);
            Assert.AreNotEqual(0u, floatComparer.GetMantissaBits(bits));
        }

        #endregion

        #endregion

        #region AssembleBits

        [TestMethod]
        public void TestAssembleBits_Zero()
        {
            uint actual = InexactFloatComparer.AssembleBits(0u, 0u, 0u);
            uint expected = 0u;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestAssembleBits_ZeroNegative()
        {
            uint actual = InexactFloatComparer.AssembleBits(1u, 0u, 0u);
            uint expected = 0x8000_0000u;
            Assert.AreEqual(expected, actual);
        }

        #endregion

    }
}
