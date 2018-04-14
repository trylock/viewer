using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.UI.Images;

namespace Viewer.UI.Presentation
{
    public partial class PresentationView : WindowView, IPresentationView
    {
        public PresentationView(string name)
        {
            InitializeComponent();
            
            Text = name;
        }

        #region View

        public event EventHandler NextImage;
        public event EventHandler PrevImage;

        public ImageView Data { get; set; }

        public bool IsFullscreen { get; set; } = false;

        public void UpdateImage()
        {
            Invalidate();
        }

        #endregion

        private void PresentationView_Paint(object sender, PaintEventArgs e)
        {
            if (Data?.Photo == null)
            {
                return;
            }

            using (var brush = new SolidBrush(BackColor))
            {
                e.Graphics.FillRectangle(brush, e.ClipRectangle);
            }

            // find out the image size
            var scaledSize = ThumbnailGenerator.GetThumbnailSize(Data.Photo.Size, ClientSize);
            var size = new Size(
                Math.Min(scaledSize.Width, Data.Photo.Size.Width), 
                Math.Min(scaledSize.Height, Data.Photo.Size.Height));
            var location = new Point(
                (ClientSize.Width - size.Width) / 2,
                (ClientSize.Height - size.Height) / 2);

            // draw the image
            e.Graphics.DrawImage(Data.Photo, new Rectangle(location, size));
        }

        private void PresentationView_Resize(object sender, EventArgs e)
        {
            Invalidate();
        }

        private void PresentationView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Right)
            {
                NextImage?.Invoke(sender, e);
            }
            else if (e.Control && e.KeyCode == Keys.Left)
            {
                PrevImage?.Invoke(sender, e);
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
