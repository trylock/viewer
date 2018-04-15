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

    public interface IPresentationView : IWindowView
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
