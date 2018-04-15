using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.UI.Images;

namespace Viewer.UI.Presentation
{
    public partial class PresentationControl : UserControl
    {
        /// <summary>
        /// Event called when user clicks the next button
        /// </summary>
        public event EventHandler NextImage;

        /// <summary>
        /// Event called when user clicks the prev button
        /// </summary>
        public event EventHandler PrevImage;

        /// <summary>
        /// Event called when user tries to toggle fullscreen mode
        /// </summary>
        public event EventHandler ToggleFullscreen;

        /// <summary>
        /// Event called when user tries to exit from the fullscreen mode
        /// </summary>
        public event EventHandler ExifFullscreen;

        /// <summary>
        /// Shown picture
        /// </summary>
        public Image Picture { get; set; }
        
        public PresentationControl()
        {
            InitializeComponent();
        }

        private void PresentationControl_Paint(object sender, PaintEventArgs e)
        {
            if (Picture == null)
            {
                return;
            }

            using (var brush = new SolidBrush(BackColor))
            {
                e.Graphics.FillRectangle(brush, e.ClipRectangle);
            }

            // find out the image size
            var clientSize = ClientSize;
            var scaledSize = ThumbnailGenerator.GetThumbnailSize(Picture.Size, clientSize);
            var size = new Size(
                Math.Min(scaledSize.Width, Picture.Size.Width),
                Math.Min(scaledSize.Height, Picture.Size.Height));
            var location = new Point(
                (clientSize.Width - size.Width) / 2,
                (clientSize.Height - size.Height) / 2);

            // draw the image
            e.Graphics.DrawImage(Picture, new Rectangle(location, size));
        }

        private void PresentationControl_Resize(object sender, EventArgs e)
        {
            Invalidate();
        }

        private void PresentationControl_MouseMove(object sender, MouseEventArgs e)
        {
            PrevButton.Visible = e.Location.X - Location.X <= PrevButton.Width;
            NextButton.Visible = Location.X + Width - e.Location.X <= NextButton.Width;
        }

        private void PresentationControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left)
            {
                PrevImage?.Invoke(sender, e);
            }
            else if (e.KeyCode == Keys.Right)
            {
                NextImage?.Invoke(sender, e);
            }
            else if (e.KeyCode == Keys.F5 || e.KeyCode == Keys.F)
            {
                ToggleFullscreen?.Invoke(sender, e);
            }
            else if (e.KeyCode == Keys.Escape)
            {
                ExifFullscreen?.Invoke(sender, e);
            }
        }

        private void PrevButton_Click(object sender, EventArgs e)
        {
            PrevImage?.Invoke(sender, e);
        }

        private void NextButton_Click(object sender, EventArgs e)
        {
            NextImage?.Invoke(sender, e);
        }
    }
}
