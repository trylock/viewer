using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.UI.Presentation
{
    public class ImageView
    {
        /// <summary>
        /// Loaded image 
        /// </summary>
        public Image Picture { get; set; }

        /// <summary>
        /// Entity of the image
        /// </summary>
        public IEntity Entity { get; set; }
    }

    public interface IZoomableView
    {
        /// <summary>
        /// Event triggered when user tries to zoom in by one step
        /// </summary>
        event EventHandler ZoomIn;

        /// <summary>
        /// Event triggered when user tries to zoom out by one step
        /// </summary>
        event EventHandler ZoomOut;

        /// <summary>
        /// Current zoom (1.0 is original size and it is also the default value).
        /// It has to be a non-negative number.
        /// </summary>
        double Zoom { get; set; }
    }

    public interface IPresentationView : IZoomableView, IWindowView
    {
        /// <summary>
        /// Event called when user requests to load next image
        /// </summary>
        event EventHandler NextImage;

        /// <summary>
        /// Event called when user requests to load previous image
        /// </summary>
        event EventHandler PrevImage;

        /// <summary>
        /// Event called when user starts/stops the presentation
        /// </summary>
        event EventHandler PlayPausePresentation;

        /// <summary>
        /// Event called when user tries to enter/leave a fullscreen mode
        /// </summary>
        event EventHandler ToggleFullscreen;

        /// <summary>
        /// Event called when user tries to leave a fullscreen mode
        /// </summary>
        event EventHandler ExitFullscreen;

        /// <summary>
        /// Event called periodically
        /// </summary>
        event EventHandler TimerTick;

        /// <summary>
        /// Currently loaded image
        /// </summary>
        Image Picture { get; set; }

        /// <summary>
        /// true iff the view is in fullscreen mode
        /// </summary>
        bool IsFullscreen { get; set; }

        /// <summary>
        /// true iff the presentation is active
        /// </summary>
        bool IsPlaying { get; set; }

        /// <summary>
        /// Presentation speed in milliseconds
        /// </summary>
        int Speed { get; set; }

        /// <summary>
        /// Update shown image
        /// </summary>
        void UpdateImage();
    }
}
