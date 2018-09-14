using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Images;

namespace Viewer.UI.Forms
{
    internal class VectorStyles 
    {
        /// <summary>
        /// Color which is used to draw the outline of a vector image.
        /// </summary>
        public Color StrokeColor { get; set; }

        /// <summary>
        /// Width of the drawn border
        /// </summary>
        public int StrokeWidth { get; set; }

        /// <summary>
        /// Line join 
        /// </summary>
        public LineJoin LineJoin { get; set; }

        /// <summary>
        /// Color which is used to draw
        /// </summary>
        public Color FillColor { get; set; }

        /// <summary>
        /// If true, the object will be filled using <see cref="FillColor"/>
        /// </summary>
        public bool IsFillEnabled { get; set; }

        /// <summary>
        /// Create brush from these settings. This will ignore <see cref="IsFillEnabled"/>.
        /// </summary>
        /// <returns></returns>
        public Brush CreateBrush()
        {
            return new SolidBrush(FillColor);
        }

        /// <summary>
        /// Create a new pen from these settings
        /// </summary>
        /// <returns></returns>
        public Pen CreatePen()
        {
            var pen = new Pen(StrokeColor, StrokeWidth);
            pen.LineJoin = LineJoin;
            return pen;
        }
    }
    
    internal class VectorStylesGroup
    {
        private VectorStyles _hover;
        private VectorStyles _disabled;

        /// <summary>
        /// Styles used in a normal state
        /// </summary>
        public VectorStyles Normal { get; set; } = new VectorStyles();

        /// <summary>
        /// Styles used when there is a mouse cursor above this object. If you don't set this
        /// property or its value is null, <see cref="Normal"/> will be returned.
        /// </summary>
        public VectorStyles Hover
        {
            get => _hover ?? Normal;
            set => _hover = value;
        }

        /// <summary>
        /// Styles used when the object is disabled. If you don't set this property or its value
        /// is null, <see cref="Normal"/> will be returned.
        /// </summary>
        public VectorStyles Disabled
        {
            get => _disabled ?? Normal;
            set => _disabled = value;
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Vector button is a button with a vector icon in it. The icon is defined by a path
    /// <see cref="Image"/>. 
    /// </summary>
    internal class VectorButton : Control
    {
        private VectorStylesGroup _imageStyles;
        private VectorStylesGroup _buttonStyles;

        /// <summary>
        /// Vector image outline. The button does not take ownership of the path. It is up to the
        /// caller to dispose it.
        /// </summary>
        public GraphicsPath Image { get; set; }

        /// <summary>
        /// Styles which are used to draw the image in the button 
        /// </summary>
        public VectorStylesGroup ImageStyles
        {
            get => _imageStyles;
            set
            {
                _imageStyles = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Styles which are used to draw the button background and border
        /// </summary>
        public VectorStylesGroup ButtonStyles
        {
            get => _buttonStyles;
            set
            {
                _buttonStyles = value;
                Invalidate();
            }
        }
        
        public new event EventHandler Click;

        public VectorButton()
        {
            SetStyle(ControlStyles.DoubleBuffer, true);
            SetStyle(ControlStyles.StandardClick, false);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            
            // setup the control with default values
            Width = 16;
            Height = 16;
            ImageStyles = new VectorStylesGroup
            {
                Normal = new VectorStyles
                {
                    StrokeColor = Color.Black,
                    StrokeWidth = 1
                }
            };
            ButtonStyles = new VectorStylesGroup
            {
                Normal = new VectorStyles()
            };
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button.HasFlag(MouseButtons.Left))
            {
                Click?.Invoke(this, e);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            VectorStyles imageStyles = ImageStyles.Normal;
            VectorStyles buttonStyles = ButtonStyles.Normal;

            if (!Enabled)
            {
                imageStyles = ImageStyles.Disabled;
                buttonStyles = ButtonStyles.Disabled;
            }
            else if (ClientRectangle.Contains(PointToClient(MousePosition))) 
            {
                // mouse cursor is over the control
                imageStyles = ImageStyles.Hover;
                buttonStyles = ButtonStyles.Hover;
            }

            e.Graphics.Clear(BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;

            // draw the button
            if (buttonStyles.IsFillEnabled)
            {
                using (var brush = buttonStyles.CreateBrush())
                {
                    e.Graphics.FillRectangle(brush, ClientRectangle);
                }
            }

            if (buttonStyles.StrokeWidth > 0)
            {
                using (var pen = buttonStyles.CreatePen())
                {
                    var rect = ClientRectangle;
                    rect.Inflate((int) -pen.Width, (int) -pen.Width);
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }

            // draw the icon
            if (Image == null)
            {
                return;
            }
            
            var bounds = Image.GetBounds();
            var iconAreaSize = new Size(
                (int) Math.Max(Width - (Padding.Left + Padding.Right), 0),
                (int) Math.Max(Height - (Padding.Top + Padding.Bottom), 0));
            var iconSize = ThumbnailGenerator.GetThumbnailSize(
                new Size((int) bounds.Width, (int)bounds.Height), 
                iconAreaSize);

            // translate icon center to the origin
            e.Graphics.TranslateTransform(
                -bounds.X - bounds.Width / 2,
                -bounds.Y - bounds.Height / 2, MatrixOrder.Append);
            // rescale the icon
            e.Graphics.ScaleTransform(
                iconSize.Width / bounds.Width,
                iconSize.Height / bounds.Height, MatrixOrder.Append);
            // translate it to the center of this control
            e.Graphics.TranslateTransform(
                Width / 2f,
                Height / 2f, MatrixOrder.Append);

            if (imageStyles.IsFillEnabled)
            {
                using (var brush = imageStyles.CreateBrush())
                {
                    e.Graphics.FillPath(brush, Image);
                }
            }

            if (imageStyles.StrokeWidth > 0)
            {
                using (var pen = imageStyles.CreatePen())
                {
                    e.Graphics.DrawPath(pen, Image);
                }
            }

            e.Graphics.ResetTransform();
        }
    }
}
