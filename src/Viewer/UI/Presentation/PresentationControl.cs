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
using Viewer.Core;
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
            add => UpdateTimer.Tick += value;
            remove => UpdateTimer.Tick -= value;
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
            DoubleBuffered = true;
            InitializeComponent();

            MouseWheel += PresentationControl_MouseWheel;

            MinDelayLabel.Text = SpeedTrackBar.Minimum + "s";
            MaxDelayLabel.Text = SpeedTrackBar.Maximum + "s";
            PlayPauseButton.IconColorTint = Color.FromArgb(0, 120, 215);
            
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
        private PointF _pictureTranslation = Point.Empty;

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
                _pictureTranslation = new PointF(
                    (float) (_pictureTranslation.X + (location.X - _lastMouseLocation.X) / Zoom),
                    (float) (_pictureTranslation.Y + (location.Y - _lastMouseLocation.Y) / Zoom)
                );
                _lastMouseLocation = location;
                Invalidate();
            }
        }

        private PointF ClampPictureTranslation(PointF translation)
        {
            var scaledSize = ThumbnailGenerator.GetThumbnailSize(Picture.Size, ClientSize);
            var zoomedSize = new Size(
                (int)(scaledSize.Width * Zoom),
                (int)(scaledSize.Height * Zoom)
            );
            var dragArea = new Size(
                (int) (Math.Max(zoomedSize.Width - ClientSize.Width, 0) / 2 / Zoom),
                (int) (Math.Max(zoomedSize.Height - ClientSize.Height, 0) / 2 / Zoom)
            );
            return new PointF(
                (float) MathUtils.Clamp(translation.X, -dragArea.Width, dragArea.Width),
                (float) MathUtils.Clamp(translation.Y, -dragArea.Height, dragArea.Height)
            );
        }

        #endregion

        #region Events

        private Matrix _lastTransformation;
        private bool _needsRepaint = false;
        private DateTime _repaintSchedule = DateTime.Now;

        /// <summary>
        /// Using the fast version of the paint method (due to recent changes to draw parameters) will
        /// schedule a repaint event triggered by the UpdateTimer tick. This is the minimal time after
        /// which this repaint event will be triggered.
        /// </summary>
        private static readonly TimeSpan RepaintDelay = new TimeSpan(0, 0, 0, 0, 200);

        private void PresentationControl_Paint(object sender, PaintEventArgs e)
        {
            if (Picture == null)
            {
                return;
            }
            
            // clear background
            e.Graphics.FillRectangle(Brushes.Black, e.ClipRectangle);
            
            var scaledSize = ThumbnailGenerator.GetThumbnailSize(Picture.Size, ClientSize);

            // make sure picture translation is within limits of the zoomed picture
            _pictureTranslation = ClampPictureTranslation(_pictureTranslation);

            // apply transformations 
            var zoom = (float)Zoom;
            e.Graphics.TranslateTransform(
                ClientSize.Width / 2.0f, 
                ClientSize.Height / 2.0f);
            e.Graphics.ScaleTransform(zoom, zoom);
            e.Graphics.TranslateTransform(
                _pictureTranslation.X - scaledSize.Width / 2.0f, 
                _pictureTranslation.Y - scaledSize.Height / 2.0f);

            // If we have made changes to draw parameters (e.g. due to resizing, zooming, dragging etc.),
            // use a fast version of the draw method and schedule a repaint event. This event is triggered by
            // the Update timer tick.
            var useFastPaint = 
                _lastTransformation != null &&
                !_lastTransformation.Elements.SequenceEqual(e.Graphics.Transform.Elements);
            _needsRepaint = useFastPaint;
            _lastTransformation = e.Graphics.Transform;
            _repaintSchedule = DateTime.Now + RepaintDelay;

            // draw the picture
            e.Graphics.InterpolationMode = useFastPaint ? 
                InterpolationMode.NearestNeighbor : 
                InterpolationMode.HighQualityBicubic;
            e.Graphics.DrawImage(Picture, new Rectangle(Point.Empty, scaledSize));
            e.Graphics.ResetTransform();
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

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (IsFullscreen)
            {
                HideCursor();
            }

            if (_needsRepaint && DateTime.Now >= _repaintSchedule)
            {
                Invalidate();
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
            else if (e.KeyCode == Keys.P)
            {
                PlayPausePresentation?.Invoke(sender, e);
            }
        }

        private void PresentationControl_MouseMove(object sender, MouseEventArgs e)
        {
            // show/hide presentation controls
            if (_isDrag)
            {
                PrevButton.Visible = false;
                NextButton.Visible = false;
                ControlPanel.Visible = false;
            }
            else
            {
                PrevButton.Visible = e.Location.X - Location.X <= PrevButton.Width;
                NextButton.Visible = Location.X + Width - e.Location.X <= NextButton.Width;
                ControlPanel.Visible = Height - (e.Location.Y - Location.Y) - _controlPanelMargin <= ControlPanel.Height;
            }

            // show cursor if it moved
            if (e.Location != _lastCursorLocation)
            {
                ShowCursor();
            }
            _lastCursorLocation = e.Location;
            
            // handle the drag operation
            Drag(e.Location);
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
            else if (e.Button.HasFlag(MouseButtons.Left))
            {
                BeginDrag(e.Location);
            }
        }

        private void PresentationControl_MouseUp(object sender, MouseEventArgs e)
        {
            EndDrag();
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
            EndDrag();
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
