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
        private readonly Form _fullscreenForm;

        public PresentationView(string name)
        {
            InitializeComponent();
            
            Text = name;

            PresentationControl.ToggleFullscreen += (sender, e) => IsFullscreen = !IsFullscreen;
            PresentationControl.ExifFullscreen += (sender, e) => IsFullscreen = false;

            _fullscreenForm = new Form
            {
                Text = name,
                FormBorderStyle = FormBorderStyle.None,
                WindowState = FormWindowState.Maximized,
                Visible = false
            };
            _fullscreenForm.FormClosing += FullscreenForm_FormClosing;
        }

        private void FullscreenForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                // don't close the fullscreen form
                e.Cancel = true;
                ToWindow();
            }
        }
        
        private void ToFullscreen()
        {
            PresentationControl.Parent = _fullscreenForm;
            PresentationControl.Invalidate();
            PresentationControl.Focus();
            _fullscreenForm.Visible = true;
        }

        private void ToWindow()
        {
            PresentationControl.Parent = this;
            PresentationControl.Invalidate();
            PresentationControl.Focus();
            _fullscreenForm.Visible = false;
        }

        #region View

        public event EventHandler NextImage
        {
            add => PresentationControl.NextImage += value;
            remove => PresentationControl.NextImage -= value;
        }

        public event EventHandler PrevImage
        {
            add => PresentationControl.PrevImage += value;
            remove => PresentationControl.PrevImage -= value;
        }

        public ImageView Data { get; set; }

        public bool IsFullscreen
        {
            get => PresentationControl.Parent != this;
            set
            {
                if (IsFullscreen == value)
                    return;

                if (value)
                    ToFullscreen();
                else 
                    ToWindow();
            }
        }

        public void UpdateImage()
        {
            PresentationControl.Picture = Data.Picture;
            PresentationControl.Refresh();
        }

        #endregion
    }
}
