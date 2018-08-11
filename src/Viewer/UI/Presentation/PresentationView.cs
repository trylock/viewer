using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Data;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Core.UI;
using Viewer.UI.Images;

namespace Viewer.UI.Presentation
{
    [Export(typeof(IPresentationView))]
    public partial class PresentationView : WindowView, IPresentationView
    {
        public PresentationView()
        {
            InitializeComponent();
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

        public event EventHandler PlayPausePresentation
        {
            add => PresentationControl.PlayPausePresentation += value;
            remove => PresentationControl.PlayPausePresentation -= value;
        }

        public event EventHandler ToggleFullscreen
        {
            add => PresentationControl.ToggleFullscreen += value;
            remove => PresentationControl.ToggleFullscreen -= value;
        }

        public event EventHandler ExitFullscreen
        {
            add => PresentationControl.ExitFullscreen += value;
            remove => PresentationControl.ExitFullscreen -= value;
        }

        public event EventHandler TimerTick
        {
            add => PresentationControl.TimerTick += value;
            remove => PresentationControl.TimerTick -= value;
        }

        public Image Picture
        {
            get => PresentationControl.Picture;
            set => PresentationControl.Picture = value;
        }

        public bool IsFullscreen
        {
            get => PresentationControl.IsFullscreen;
            set => PresentationControl.IsFullscreen = value;
        }

        public bool IsPlaying
        {
            get => PresentationControl.IsPlaying;
            set => PresentationControl.IsPlaying = value;
        }

        public int Speed
        {
            get => PresentationControl.Speed;
            set => PresentationControl.Speed = value;
        }
        
        public void UpdateImage()
        {
            PresentationControl.Refresh();
        }

        public event EventHandler ZoomIn
        {
            add => PresentationControl.ZoomIn += value;
            remove => PresentationControl.ZoomIn -= value;
        }

        public event EventHandler ZoomOut
        {
            add => PresentationControl.ZoomOut += value;
            remove => PresentationControl.ZoomOut -= value;
        }

        public double Zoom
        {
            get => PresentationControl.Zoom;
            set => PresentationControl.Zoom = value;
        }

        #endregion
    }
}
