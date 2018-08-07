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
using Viewer.Images;
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

        public event EventHandler ZoomIn;
        public event EventHandler ZoomOut;

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

        private double _zoom = 1.0;
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
            }
        } 

        public int CursorHideDelay { get; set; } = 1000;

        private readonly Form _fullscreenForm;
        private const int _controlPanelMargin = 5;

        public PresentationControl()
        {
            InitializeComponent();

            MouseWheel += PresentationControl_MouseWheel;

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

        /// <summary>
        /// Find a screen on which lies most area of this component
        /// </summary>
        /// <returns>Screen of the component</returns>
        private Screen GetCurrentScreen()
        {
            var screenBounds = new Rectangle(PointToScreen(Location), Bounds.Size);
            Screen currentScreen = null;
            int maxIntersection = -1;
            foreach (var screen in Screen.AllScreens)
            {
                var intersectionArea = Rectangle.Intersect(screen.Bounds, screenBounds).Area();
                if (intersectionArea > maxIntersection)
                {
                    maxIntersection = intersectionArea;
                    currentScreen = screen;
                }
            }

            return currentScreen;
        }

        private Control _windowParent;

        private void ToFullscreen()
        {
            var screen = GetCurrentScreen();
            _windowParent = Parent;
            Parent = _fullscreenForm;
            Invalidate();
            Focus();

            // in fullscreen state, we won't be able to modify location
            _fullscreenForm.WindowState = FormWindowState.Normal;
            _fullscreenForm.Location = screen.WorkingArea.Location;
            _fullscreenForm.WindowState = FormWindowState.Maximized;
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
            
            // figure out the draw area bounds
            var scaledSize = ThumbnailGenerator.GetThumbnailSize(Picture.Size, ClientSize);
            var drawSize = new Size(
                (int) (scaledSize.Width * Zoom),
                (int) (scaledSize.Height * Zoom)
            );
            var drawLocation = new Point(
                ClientSize.Width / 2 - drawSize.Width / 2,
                ClientSize.Height / 2 - drawSize.Height / 2
            );
            var drawBounds = new Rectangle(drawLocation, drawSize);

            // draw the image
            e.Graphics.DrawImage(Picture, drawBounds);
        }

        private void PresentationControl_Resize(object sender, EventArgs e)
        {
            ControlPanel.Left = (Width - ControlPanel.Width) / 2;
            ControlPanel.Top = Height - ControlPanel.Height - _controlPanelMargin;
            Invalidate();
        }
        
        private bool _isCursorHidden = false;
        private DateTime _lastCursorMove;
        private Point _lastCursorLocation;

        private void ShowCursor()
        {
            _lastCursorMove = DateTime.Now;
            if (_isCursorHidden)
            {
                Cursor.Show();
                _isCursorHidden = false;
            }
        }

        private void PresentationControl_MouseWheel(object sender, MouseEventArgs e)
        {
            // move to the next/previous image
            if (e.Delta >= 120)
            {
                if (ModifierKeys.HasFlag(Keys.Control))
                {
                    ZoomIn?.Invoke(sender, e);
                }
                else
                {
                    NextImage?.Invoke(sender, e);
                }
            }
            else if (e.Delta <= -120)
            {
                if (ModifierKeys.HasFlag(Keys.Control))
                {
                    ZoomOut?.Invoke(sender, e);
                }
                else
                {
                    PrevImage?.Invoke(sender, e);
                }
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

        private void HideCursorTimer_Tick(object sender, EventArgs e)
        {
            if (IsFullscreen)
            {
                HideCursor();
            }
        }

        private void PresentationControl_MouseMove(object sender, MouseEventArgs e)
        {
            PrevButton.Visible = e.Location.X - Location.X <= PrevButton.Width;
            NextButton.Visible = Location.X + Width - e.Location.X <= NextButton.Width;
            ControlPanel.Visible = Height - (e.Location.Y - Location.Y) - _controlPanelMargin <= ControlPanel.Height;

            if (e.Location != _lastCursorLocation)
            {
                ShowCursor();
            }
            _lastCursorLocation = e.Location;
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

        private void PresentationControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.XButton1))
            {
                PrevImage?.Invoke(sender, e);
            }
            else if (e.Button.HasFlag(MouseButtons.XButton2))
            {
                NextImage?.Invoke(sender, e);
            }
        }

        private void PresentationControl_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Left))
            {
                ToggleFullscreen?.Invoke(sender, e);
            }
        }

        private void PresentationControl_MouseLeave(object sender, EventArgs e)
        {
            var point = PointToClient(Cursor.Position);
            if (ClientRectangle.Contains(point))
            {
                return;
            }
            PrevButton.Visible = false;
            NextButton.Visible = false;
            ControlPanel.Visible = false;
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
