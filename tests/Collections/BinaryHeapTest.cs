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
    public class BinaryHeapTest
    {
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Dequeue_EmptyQueue()
        {
            var heap = new BinaryHeap<int>();
            heap.Dequeue();
        }

        [TestMethod]
        public void Enqueue_ItemsInAscedingOrder()
        {
            var heap = new BinaryHeap<int>();
            for (var i = 0; i < 10; ++i)
            {
                heap.Enqueue(i);
            }

            var items = new List<int>();
            while (heap.Count > 0)
            {
                items.Add(heap.Dequeue());
            }
            CollectionAssert.AreEqual(new[]{ 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, items);
        }

        [TestMethod]
        public void Enqueue_ItemsInDescendingOrder()
        {
            var heap = new BinaryHeap<int>();
            for (var i = 9; i >= 0; --i)
            {
                heap.Enqueue(i);
            }

            var items = new List<int>();
            while (heap.Count > 0)
            {
                items.Add(heap.Dequeue());
            }
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, items);
        }

        [TestMethod]
        public void Enqueue_ItemsInRandomOrder()
        {
            var heap = new BinaryHeap<int>();

            // generate a random permutation
            var random = new Random();
            var enqueuedItems = new List<int>{ 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            for (var i = 0; i < enqueuedItems.Count; ++i)
            {
                var newPosition = random.Next(i, enqueuedItems.Count - 1);

                var tmp = enqueuedItems[i];
                enqueuedItems[i] = enqueuedItems[newPosition];
                enqueuedItems[newPosition] = tmp;
            }

            // enqueue the items
            for (var i = 0; i < 10; ++i)
            {
                heap.Enqueue(enqueuedItems[i]);
            }
            
            var dequeuedItems = new List<int>();
            while (heap.Count > 0)
            {
                dequeuedItems.Add(heap.Dequeue());
            }
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, dequeuedItems);
        }
    }
}
