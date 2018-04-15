using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Properties;
using Viewer.UI.Images;

namespace Viewer.UI.Presentation
{
    public partial class PresentationControl : UserControl
    {
        public event EventHandler NextImage;
        public event EventHandler PrevImage;
        public event EventHandler ToggleFullscreen;
        public event EventHandler ExitFullscreen;
        public event EventHandler PlayPausePresentation;
        public event EventHandler TimerTick
        {
            add => HideCursorTimer.Tick += value;
            remove => HideCursorTimer.Tick -= value;
        }

        public Image Picture { get; set; }
        
        private bool _isPlaying = false;

        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                _isPlaying = value;
                PlayPauseButton.BackgroundImage = _isPlaying ? Resources.PauseIcon : Resources.PlayIcon;
                PlayPauseButton.Invalidate();
            }
        }

        public int Speed
        {
            get => SpeedTrackBar.Value * 1000;
            set => SpeedTrackBar.Value = value / 1000;
        }
        
        public bool IsFullscreen
        {
            get => Parent == _fullscreenForm;
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

        public int CursorHideDelay { get; set; } = 1000;

        private readonly Form _fullscreenForm;
        private const int _controlPanelMargin = 5;

        public PresentationControl()
        {
            InitializeComponent();

            MinDelayLabel.Text = SpeedTrackBar.Minimum + "s";
            MaxDelayLabel.Text = SpeedTrackBar.Maximum + "s";
            
            _fullscreenForm = new Form
            {
                Text = "Presentation",
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

        private Control _windowParent;

        private void ToFullscreen()
        {
            _windowParent = Parent;
            Parent = _fullscreenForm;
            Invalidate();
            Focus();
            _fullscreenForm.Visible = true;
        }

        private void ToWindow()
        {
            Parent = _windowParent;
            Invalidate();
            Focus();
            _fullscreenForm.Visible = false;
        }

        #region Events

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
            ControlPanel.Left = (Width - ControlPanel.Width) / 2;
            ControlPanel.Top = Height - ControlPanel.Height - _controlPanelMargin;
            Invalidate();
        }
        
        private bool _isCursorHidden = false;
        private DateTime _lastCursorMove;

        private void ShowCursor()
        {
            _lastCursorMove = DateTime.Now;
            if (_isCursorHidden)
            {
                Cursor.Show();
                _isCursorHidden = false;
            }
        }

        private void HideCursor()
        {
            var delay = DateTime.Now - _lastCursorMove;
            if (delay.TotalMilliseconds >= CursorHideDelay && !_isCursorHidden)
            {
                Cursor.Hide();
                _isCursorHidden = true;
            }
        }

        private void PresentationControl_MouseMove(object sender, MouseEventArgs e)
        {
            PrevButton.Visible = e.Location.X - Location.X <= PrevButton.Width;
            NextButton.Visible = Location.X + Width - e.Location.X <= NextButton.Width;
            ControlPanel.Visible = Height - (e.Location.Y - Location.Y) - _controlPanelMargin <= ControlPanel.Height;
            
            ShowCursor();
        }
        
        private void HideCursorTimer_Tick(object sender, EventArgs e)
        {
            if (IsFullscreen)
            {
                HideCursor();
            }
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
                ExitFullscreen?.Invoke(sender, e);
            }
        }

        private void PresentationControl_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ToggleFullscreen?.Invoke(sender, e);
        }

        private void PrevButton_Click(object sender, EventArgs e)
        {
            PrevImage?.Invoke(sender, e);
        }

        private void NextButton_Click(object sender, EventArgs e)
        {
            NextImage?.Invoke(sender, e);
        }
        
        private void PausePlayButton_Click(object sender, EventArgs e)
        {
            PlayPausePresentation?.Invoke(sender, e);
        }

        #endregion
    }
}
