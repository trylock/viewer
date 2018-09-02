using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SkiaSharp;
using Viewer.Core;
using Viewer.Images;
using Viewer.Properties;
using Viewer.UI.Images;

namespace Viewer.UI.Presentation
{
    internal partial class PresentationControl : UserControl
    {
        public event EventHandler NextImage;
        public event EventHandler PrevImage;
        public event EventHandler ToggleFullscreen;
        public event EventHandler ExitFullscreen;
        public event EventHandler PlayPausePresentation;
        public event EventHandler TimerTick
        {
            add => UpdateTimer.Tick += value;
            remove => UpdateTimer.Tick -= value;
        }

        public event EventHandler ZoomIn;
        public event EventHandler ZoomOut;

        public SKBitmap Picture
        {
            get => Preview.Picture;
            set => Preview.Picture = value;
        }
        
        private bool _isPlaying = false;

        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                _isPlaying = value;
                PlayPauseButton.Icon = _isPlaying ? Resources.Pause : Resources.Play;
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

                ToggleFullscreenButton.Icon = value ? Resources.Windowed : Resources.Fullscreen;

                if (value)
                    ToFullscreen();
                else
                    ToWindow();
            }
        }
        
        public double Zoom
        {
            get => Preview.Zoom;
            set => Preview.Zoom = value;
        } 

        public int CursorHideDelay { get; set; } = 1000;

        private readonly Form _fullscreenForm;

        public PresentationControl()
        {
            InitializeComponent();

            Preview.MouseWheel += Preview_MouseWheel;
            RegisterShortcutHandler(this);
            RegisterHideControlHandler(this);

            MinDelayLabel.Text = SpeedTrackBar.Minimum + "s";
            MaxDelayLabel.Text = SpeedTrackBar.Maximum + "s";
            PlayPauseButton.IconColor = Color.FromArgb(0, 120, 215);
            
            _fullscreenForm = new Form
            {
                Text = Text,
                FormBorderStyle = FormBorderStyle.None,
                WindowState = FormWindowState.Maximized,
                Visible = false
            };
            _fullscreenForm.FormClosing += FullscreenForm_FormClosing;
        }

        private void RegisterShortcutHandler(Control control)
        {
            foreach (Control child in control.Controls)
            {
                child.KeyDown += ShortcutHandler;
                RegisterShortcutHandler(child);
            }
        }

        private void RegisterHideControlHandler(Control control)
        {
            foreach (Control child in control.Controls)
            {
                child.MouseLeave += HideControlsHandler;
                RegisterHideControlHandler(child);
            }
        }

        #region Fullscreen

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

        #endregion

        #region Drag picture

        private bool _isDrag = false;
        private PointF _lastMouseLocation;

        private void BeginDrag(Point location)
        {
            _isDrag = true;
            _lastMouseLocation = location;
        }

        private void EndDrag()
        {
            _isDrag = false;
            Invalidate();
        }

        private void Drag(Point location)
        {
            if (_isDrag)
            {
                Preview.Origin = new SKPoint(
                    (float)(Preview.Origin.X + (location.X - _lastMouseLocation.X) / Preview.Zoom),
                    (float)(Preview.Origin.Y + (location.Y - _lastMouseLocation.Y) / Preview.Zoom)
                );
                _lastMouseLocation = location;
                Invalidate();
            }
        }

        #endregion

        #region Cursor
        
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

        private void HideCursor()
        {
            var delay = DateTime.Now - _lastCursorMove;
            if (delay.TotalMilliseconds >= CursorHideDelay && !_isCursorHidden)
            {
                Cursor.Hide();
                _isCursorHidden = true;
            }
        }

        #endregion 

        #region Events

        private void FullscreenForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                // don't close the fullscreen form
                e.Cancel = true;
                ToWindow();
            }
        }

        private void Preview_MouseWheel(object sender, MouseEventArgs e)
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

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (IsFullscreen)
            {
                var cursor = PointToClient(MousePosition);
                var child = GetChildAtPoint(cursor);
                if (child?.GetType() == typeof(PreviewControl))
                {
                    // only hide the cursor if it is over the image
                    HideCursor();
                }
            }
        }
        
        private void ShortcutHandler(object sender, KeyEventArgs e)
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
            else if (e.KeyCode == Keys.Space)
            {
                PlayPausePresentation?.Invoke(sender, e);
            }
        }
        
        private void HideControlsHandler(object sender, EventArgs e)
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

        private void Preview_MouseMove(object sender, MouseEventArgs e)
        {
            // show cursor if it moved
            if (e.Location != _lastCursorLocation)
            {
                ShowCursor();
            }
            _lastCursorLocation = e.Location;

            // handle drag
            if (_isDrag)
            {
                Drag(e.Location);
                PrevButton.Visible = false;
                NextButton.Visible = false;
                ControlPanel.Visible = false;
            }
            else
            {
                // show/hide presentation controls
                PrevButton.Visible = e.Location.X - Location.X <= PrevButton.Width;
                NextButton.Visible = Location.X + Width - e.Location.X <= NextButton.Width;
                ControlPanel.Visible = ControlPanel.Location.Y <= e.Location.Y;
            }
        }

        private void Preview_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Left))
            {
                BeginDrag(e.Location);
            }
            else if (e.Button.HasFlag(MouseButtons.XButton1))
            {
                PrevImage?.Invoke(sender, e);
            }
            else if (e.Button.HasFlag(MouseButtons.XButton2))
            {
                NextImage?.Invoke(sender, e);
            }
        }
        
        private void Preview_MouseUp(object sender, MouseEventArgs e)
        {
            EndDrag();
        }
        
        private void Preview_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Left))
            {
                ToggleFullscreen?.Invoke(sender, e);
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

        private void PlayPauseButton_Click(object sender, EventArgs e)
        {
            PlayPausePresentation?.Invoke(sender, e);
        }

        private void ZoomOutButton_Click(object sender, EventArgs e)
        {
            ZoomOut?.Invoke(sender, e);
        }

        private void ZoomInButton_Click(object sender, EventArgs e)
        {
            ZoomIn?.Invoke(sender, e);
        }

        private void ToggleFullscreenButton_Click(object sender, EventArgs e)
        {
            IsFullscreen = !IsFullscreen;
        }

        #endregion
    }
}
