using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Core.Collections;

namespace ViewerTest.Collections
{
    [TestClass]
    public class BitmapTest
    {
        [TestMethod]
        public void EmptyBitmap()
        {
            var bitmap = new Bitmap(0);
            Assert.AreEqual(0, bitmap.Count);
            CollectionAssert.AreEqual(new bool[0], bitmap.ToArray());
        }

        [TestMethod]
        public void Set_SetBitsInSingleBlockBitmap()
        {
            var bitmap = new Bitmap(50);
            Assert.AreEqual(50, bitmap.Count);

            bitmap.Set(0);
            bitmap.Set(49);
            bitmap.Set(10);

            var bits = new bool[bitmap.Count];
            bits[0] = true;
            bits[10] = true;
            bits[49] = true;

            CollectionAssert.AreEqual(bits, bitmap.ToArray());
        }
        
        [TestMethod]
        public void Set_SetBitsInMultipleBlockBitmap()
        {
            var bitmap = new Bitmap(70);
            Assert.AreEqual(70, bitmap.Count);

            bitmap.Set(0);
            bitmap.Set(63);
            bitmap.Set(64);
            bitmap.Set(69);

            var bits = new bool[bitmap.Count];
            bits[0] = true;
            bits[63] = true;
            bits[64] = true;
            bits[69] = true;

            CollectionAssert.AreEqual(bits, bitmap.ToArray());
        }

        [TestMethod]
        public void And_CombineEmptyBitmaps()
        {
            var bitmap = new Bitmap(0);
            var other = new Bitmap(0);

            Assert.AreEqual(0, bitmap.Count);
            Assert.AreEqual(0, other.Count);

            bitmap.And(other);

            Assert.AreEqual(0, bitmap.Count);
        }

        [TestMethod]
        public void And_CombineSingleBlockBitmaps()
        {
            var bitmap = new Bitmap(50);
            var other = new Bitmap(50);

            bitmap.Set(0);
            bitmap.Set(10);
            bitmap.Set(14);
            bitmap.Set(22);
            bitmap.Set(20);
            bitmap.Set(42);
            bitmap.Set(41);
            bitmap.Set(43);

            other.Set(42);
            other.Set(21);

            bitmap.And(other);

            Assert.AreEqual(50, bitmap.Count);

            var bits = new bool[bitmap.Count];
            bits[42] = true;

            CollectionAssert.AreEqual(bits, bitmap.ToArray());
        }

        [TestMethod]
        public void And_CombineMultipleBlockBitmaps()
        {
            var bitmap = new Bitmap(70);
            var other = new Bitmap(70);

            bitmap.Set(0);
            bitmap.Set(10);
            bitmap.Set(14);
            bitmap.Set(22);
            bitmap.Set(20);
            bitmap.Set(42);
            bitmap.Set(41);
            bitmap.Set(43);
            bitmap.Set(68);
            bitmap.Set(66);
            bitmap.Set(67);

            other.Set(42);
            other.Set(21);
            other.Set(66);
            other.Set(67);

            bitmap.And(other);

            Assert.AreEqual(70, bitmap.Count);

            var bits = new bool[bitmap.Count];
            bits[42] = true;
            bits[66] = true;
            bits[67] = true;

            CollectionAssert.AreEqual(bits, bitmap.ToArray());
        }
        
        [TestMethod]
        public void Or_CombineEmptyBitmaps()
        {
            var bitmap = new Bitmap(0);
            var other = new Bitmap(0);

            Assert.AreEqual(0, bitmap.Count);
            Assert.AreEqual(0, other.Count);

            bitmap.Or(other);

            Assert.AreEqual(0, bitmap.Count);
        }

        [TestMethod]
        public void Or_CombineSingleBlockBitmaps()
        {
            var bitmap = new Bitmap(50);
            var other = new Bitmap(50);

            bitmap.Set(0);
            bitmap.Set(14);
            bitmap.Set(49);

            other.Set(13);
            other.Set(24);
            other.Set(49);
            
            bitmap.Or(other);

            Assert.AreEqual(50, bitmap.Count);

            var bits = new bool[bitmap.Count];
            bits[0] = true;
            bits[13] = true;
            bits[14] = true;
            bits[24] = true;
            bits[49] = true;

            CollectionAssert.AreEqual(bits, bitmap.ToArray());
        }

        [TestMethod]
        public void Or_CombineMultipleBlockBitmaps()
        {
            var bitmap = new Bitmap(70);
            var other = new Bitmap(70);

            bitmap.Set(0);
            bitmap.Set(63);
            bitmap.Set(64);
            bitmap.Set(67);

            other.Set(63);
            other.Set(64);
            other.Set(68);

            bitmap.Or(other);

            Assert.AreEqual(70, bitmap.Count);

            var bits = new bool[bitmap.Count];
            bits[0] = true;
            bits[63] = true;
            bits[64] = true;
            bits[67] = true;
            bits[68] = true;

            CollectionAssert.AreEqual(bits, bitmap.ToArray());
        }

        [TestMethod]
        public void Not_EmptyBitmap()
        {
            var bitmap = new Bitmap(0);
            Assert.AreEqual(0, bitmap.Count);
            bitmap.Not();
            Assert.AreEqual(0, bitmap.Count);
            CollectionAssert.AreEqual(new bool[0], bitmap.ToArray());
        }

        [TestMethod]
        public void Not_SingleBlockBitmap()
        {
            var bitmap = new Bitmap(50);

            for (var i = 2; i < bitmap.Count - 2; ++i)
            {
                bitmap.Set(i);
            }

            bitmap.Not();

            Assert.AreEqual(50, bitmap.Count);

            var bits = new bool[bitmap.Count];
            bits[0] = true;
            bits[1] = true;
            bits[48] = true;
            bits[49] = true;

            CollectionAssert.AreEqual(bits, bitmap.ToArray());
        }

        [TestMethod]
        public void Not_MultipleBlockBitmap()
        {
            var bitmap = new Bitmap(180);

            for (var i = 2; i < bitmap.Count - 2; ++i)
            {
                if (i == 64)
                {
                    continue;
                }
                bitmap.Set(i);
            }

            bitmap.Not();

            Assert.AreEqual(180, bitmap.Count);

            var bits = new bool[bitmap.Count];
            bits[0] = true;
            bits[1] = true;
            bits[64] = true;
            bits[178] = true;
            bits[179] = true;

            CollectionAssert.AreEqual(bits, bitmap.ToArray());
        }
    }
}
