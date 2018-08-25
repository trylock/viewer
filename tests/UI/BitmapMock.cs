using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;

namespace ViewerTest.UI
{
    internal sealed class BitmapMock : SKBitmap
    {
        public bool IsDisposed { get; private set; }

        public BitmapMock() : base(1, 1)
        {
        }

        public BitmapMock(int width, int height) : base(width, height)
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            IsDisposed = true;
        }
    }
}
