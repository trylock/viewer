using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Viewer.UI.Forms
{
    public partial class IconButton : Control
    {
        private Image _icon;
        private Size _iconSize = Size.Empty;
        private Color _iconColorTint = Color.White;
        private readonly ImageAttributes _imageAttributes = new ImageAttributes();

        public new event EventHandler Click;

        /// <summary>
        /// Icon of this button
        /// </summary>
        public Image Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Size of the icon.
        /// If this is empty, size of the original image will be used instead.
        /// </summary>
        public Size IconSize
        {
            get => _iconSize;
            set
            {
                _iconSize = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Modify icon color.
        /// The color will be multiplied with this tint color to get the result.
        /// That is, if the icon is black, this won't have any effect.
        /// If the icon is white, this will effectively change the color of this icon.
        /// </summary>
        public Color IconColorTint
        {
            get => _iconColorTint;
            set
            {
                _iconColorTint = value;
                UpdateColorMatrix();
                Invalidate();
            }
        }
        
        public IconButton()
        {
            InitializeComponent();
            
            SetStyle(ControlStyles.DoubleBuffer, true);
            SetStyle(ControlStyles.StandardClick, false);
            
            MouseDown += OnMouseDown;
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Left))
            {
                Click?.Invoke(sender, e);
            }
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            _imageAttributes?.Dispose();
            base.Dispose(disposing);
        }

        private void UpdateColorMatrix()
        {
            var matrix = new ColorMatrix(new[]
            {
                new[]{ _iconColorTint.R / 255.0f, 0, 0, 0, 0 },
                new[]{ 0, _iconColorTint.G / 255.0f, 0, 0, 0 },
                new[]{ 0, 0, _iconColorTint.B / 255.0f, 0, 0 },
                new[]{ 0, 0, 0, _iconColorTint.A / 255.0f, 0 },
                new[]{ 0, 0, 0, 0, 1.0f },
            });
            _imageAttributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Icon == null)
            {
                return;
            }
            
            var size = IconSize.IsEmpty ? Icon.Size : IconSize;
            var location = new Point(
                ClientSize.Width / 2 - size.Width / 2,
                ClientSize.Height / 2 - size.Height / 2
            );
            e.Graphics.DrawImage(Icon, new Rectangle(location, size), 0, 0, Icon.Width, Icon.Height, GraphicsUnit.Pixel, _imageAttributes);
        }
    }
}
