using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PhotoSauce.MagicScaler;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using Viewer.Core;
using Viewer.Images;

namespace Viewer.UI.Presentation
{
    /// <inheritdoc />
    /// <summary>
    /// Preview control draws <see cref="Picture" /> using <see cref="Zoom" /> to zoom it with
    /// <see cref="Origin" /> as a center point.
    /// </summary>
    internal class PreviewControl : SKControl
    {
        private SKBitmap _picture;
        private double _zoom = 1.0;
        private SKPoint _origin = SKPoint.Empty;
        private SKMatrix _lastTransform = SKMatrix.MakeIdentity();
        private bool _needsRepaint = false;
        private DateTime _repaintSchedule = DateTime.Now;

        /// <summary>
        /// Using the fast version of the paint method (due to recent changes to draw parameters)
        /// will schedule a repaint event triggered by the _timer tick. This is the minimal time
        /// after which this repaint event will be triggered.
        /// </summary>
        private static readonly TimeSpan RepaintDelay = new TimeSpan(0, 0, 0, 0, 200);

        private readonly Timer _timer;

        /// <summary>
        /// Picture to draw in this preview control
        /// </summary>
        public SKBitmap Picture
        {
            get => _picture;
            set
            {
                _picture = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Zooms <see cref="Picture"/> relative to the sacled image to the size of this control.
        /// (i.e, zoom of 1.0 means this image scaled to the size of this window)
        /// </summary>
        public double Zoom
        {
            get => _zoom;
            set
            {
                if (value < 0.0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _zoom = value;
                _origin = ClampPictureTranslation(_origin);
                Invalidate();
            }
        }

        /// <summary>
        /// Translation of the center of the zoomed image. [0, 0] is the center of the image.
        /// </summary>
        public SKPoint Origin
        {
            get => _origin;
            set
            {
                _origin = ClampPictureTranslation(value);
                Invalidate();
            }
        }

        public PreviewControl()
        {
            _timer = new Timer();
            _timer.Interval = (int) (RepaintDelay.TotalMilliseconds / 2);
            _timer.Tick += TimerOnTick;
            _timer.Enabled = true;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _timer.Dispose();
        }

        private void TimerOnTick(object sender, EventArgs e)
        {
            if (_needsRepaint && DateTime.Now >= _repaintSchedule)
            {
                Invalidate();
                _needsRepaint = false;
            }
        }

        private SKPoint ClampPictureTranslation(SKPoint translation)
        {
            if (Picture == null)
                return SKPoint.Empty;
            
            var originalSize = new Size(Picture.Width, Picture.Height);
            var scaledSize = Thumbnail.GetThumbnailSize(originalSize, ClientSize);
            var zoomedSize = new Size(
                (int) (scaledSize.Width * Zoom),
                (int) (scaledSize.Height * Zoom)
            );
            var dragArea = new Size(
                (int) (Math.Max(zoomedSize.Width - ClientSize.Width, 0) / 2.0f / Zoom),
                (int) (Math.Max(zoomedSize.Height - ClientSize.Height, 0) / 2.0f / Zoom)
            );
            return new SKPoint(
                (float) MathUtils.Clamp(translation.X, -dragArea.Width, dragArea.Width),
                (float) MathUtils.Clamp(translation.Y, -dragArea.Height, dragArea.Height)
            );
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            base.OnPaintSurface(e);

            var surface = e.Surface;
            var canvas = surface.Canvas;

            // clear the canvas
            canvas.Clear(SKColors.Black);

            if (Picture == null)
            {
                return;
            }

            // calculate scaled image size
            var pictureSize = new Size(Picture.Width, Picture.Height);
            var pictureDrawSize = Thumbnail
                .GetThumbnailSize(pictureSize, ClientSize)
                .ToSKSize();

            // apply transformations
            var zoomScale = (float) Zoom;
            canvas.Translate(
                ClientSize.Width / 2.0f, 
                ClientSize.Height / 2.0f);
            canvas.Scale(zoomScale, zoomScale);
            canvas.Translate(
                Origin.X - pictureDrawSize.Width / 2.0f,
                Origin.Y - pictureDrawSize.Height / 2.0f);

            // draw the image
            using (var paint = new SKPaint())
            {
                // If the draw parameters have changed recently (e.g. because the control has been
                // resized, zoomed, dragged etc.), use a fast version of the draw method and 
                // schedule a repaint event triggered by the _timer.Tick event handler.
                var currentTransform = canvas.TotalMatrix;
                _needsRepaint = !_lastTransform.Values.SequenceEqual(currentTransform.Values);
                _lastTransform = currentTransform;
                _repaintSchedule = DateTime.Now + RepaintDelay;
                
                paint.FilterQuality = _needsRepaint ? SKFilterQuality.None : SKFilterQuality.Medium;

                canvas.DrawBitmap(Picture, 
                    SKRect.Create(pictureSize.ToSKSize()), 
                    SKRect.Create(pictureDrawSize), 
                    paint);
                canvas.Flush();
            }
        }
    }
}
