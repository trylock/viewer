using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Core;
using EventArgs = System.EventArgs;

namespace Viewer.UI.Forms
{
    /// <inheritdoc />
    /// <summary>
    /// More compact, custom drawn version of <see cref="T:System.Windows.Forms.TrackBar" />.
    /// </summary>
    public partial class SliderControl : Control
    {
        private static readonly Color DefaultInactiveColor = Color.FromArgb(125, 255, 255, 255);
        private static readonly Color DefaultActiveColor = Color.White;

        private int _value = 0;
        private int _minValue = 0;
        private int _maxValue = 100;
        private int _thickness = 2;
        private int _handleRadius = 5;
        private SolidBrush _activeBrush;
        private SolidBrush _inactiveBrush;
        
        private int SidePadding => _handleRadius + 1;

        /// <summary>
        /// Color of the inactive part of the slider.
        /// </summary>
        public Color InactiveColor
        {
            get => _inactiveBrush.Color;
            set
            {
                _inactiveBrush.Dispose();
                _inactiveBrush = new SolidBrush(value);
                Invalidate();
            }
        }

        /// <summary>
        /// Color of the active part of the slider
        /// </summary>
        public Color ActiveColor
        {
            get => _activeBrush.Color;
            set
            {
                _activeBrush.Dispose();
                _activeBrush = new SolidBrush(value);
                Invalidate();
            }
        }

        /// <summary>
        /// Minimal value of the slider. It has to be at most <see cref="MaximalValue"/>.
        /// Otherwise, it will throw <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        public int MinimalValue
        {
            get => _minValue;
            set
            {
                if (value > MaximalValue)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _minValue = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Maximal value of the slider. It has to be at lest <see cref="MinimalValue"/>.
        /// Otherwise, it will throw <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        public int MaximalValue
        {
            get => _maxValue;
            set
            {
                if (value < MinimalValue)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _maxValue = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Current value of the slider. It has to be between <see cref="MinimalValue"/> and <see cref="MaximalValue"/>.
        /// Otherwise, it throws <see cref="ArgumentOutOfRangeException"/>.
        /// Setting the value will trigger the <see cref="ValueChanged"/> event.
        /// </summary>
        public int Value
        {
            get => _value;
            set
            {
                if (value < MinimalValue || value > MaximalValue)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _value = value;
                ValueChanged?.Invoke(this, EventArgs.Empty);
                Refresh();
            }
        }

        /// <summary>
        /// Event occurs whenever <see cref="Value"/> changes.
        /// </summary>
        public event EventHandler ValueChanged;

        public SliderControl()
        {
            InitializeComponent();

            _activeBrush = new SolidBrush(DefaultActiveColor);
            _inactiveBrush = new SolidBrush(DefaultInactiveColor);
            
            SetStyle(ControlStyles.DoubleBuffer, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;

            MouseDown += OnMouseEvent;
            MouseMove += OnMouseEvent;
            MouseWheel += OnMouseWheel;
        }

        /// <inheritdoc />
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
            _activeBrush.Dispose();
            _activeBrush = null;
            _inactiveBrush.Dispose();
            _inactiveBrush = null;
            base.Dispose(disposing);
        }

        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            var delta = SystemInformation.MouseWheelScrollLines;
            if (e.Delta >= 120)
            {
                Value = Math.Min(Value + delta, MaximalValue);
            }
            else if (e.Delta <= -120)
            {
                Value = Math.Max(Value - delta, MinimalValue);
            }
        }

        private void OnMouseEvent(object sender, MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Left))
            {
                SetValueFromLocation(e.Location);
            }
        }

        private void SetValueFromLocation(Point location)
        {
            var maxBarWidth = ClientSize.Width - 2 * SidePadding;
            var horizontalLocation = MathUtils.Clamp(location.X - SidePadding, 0, maxBarWidth);
            var normalizedLocation = horizontalLocation / (double)maxBarWidth;

            Value = (int)Math.Round(MathUtils.Lerp(MinimalValue, MaximalValue, normalizedLocation));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var scale = new PointF(e.Graphics.DpiX / 96f, e.Graphics.DpiY / 96f);

            // maximal bar with (excluding padding)
            var maxBarWidth = ClientSize.Width - 2 * SidePadding;
            var barHeight = (int) (_thickness * scale.Y);

            // slider location normalized to the [0, 1] interval
            var normalizedPosition = (Value - MinimalValue) / (double) (MaximalValue - MinimalValue);
            var activeWidth = (int) MathUtils.Lerp(0, maxBarWidth, normalizedPosition);
            var inactiveWidth = (int) MathUtils.Lerp(0, maxBarWidth, 1 - normalizedPosition);
            
            var activeLocation = new Point(
                SidePadding,
                ClientSize.Height / 2 - barHeight / 2
            );
            var activeSize = new Size(
                activeWidth,
                barHeight
            );

            var inactiveLocation = new Point(
                SidePadding + activeSize.Width,
                activeLocation.Y
            );
            var inactiveSize = new Size(
                inactiveWidth,
                barHeight
            );

            var handleSize = new Size(
                (int)(_handleRadius * 2 * scale.X),
                (int)(_handleRadius * 2 * scale.Y));
            var handleLocation = new Point(
                SidePadding + activeWidth - handleSize.Width / 2,
                ClientSize.Height / 2 - barHeight / 2 - handleSize.Height / 2
            );

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.FillRectangle(_activeBrush, new Rectangle(activeLocation, activeSize));
            e.Graphics.FillRectangle(_inactiveBrush, new Rectangle(inactiveLocation, inactiveSize));
            e.Graphics.FillEllipse(_activeBrush, new Rectangle(handleLocation, handleSize));
        }
    }
}
