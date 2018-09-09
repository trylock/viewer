using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.UI.Presentation
{
    /// <summary>
    /// Facade of the presentation component
    /// </summary>
    public interface IPresentation
    {
        [Obsolete("Use OpenAsync. It has almost the same implementation but returns a task.")]
        void Open(IEnumerable<IEntity> entities, int activeIndex);

        /// <summary>
        /// Open presentation of <paramref name="entities"/> where <paramref name="activeIndex"/>
        /// is the index of an active entity in <paramref name="entities"/>..
        /// </summary>
        /// <param name="entities">Entities in the presentation</param>
        /// <param name="activeIndex">Index of an entity to open</param>
        /// <remarks>Task finished when the image is loaded</remarks>
        Task OpenAsync(IEnumerable<IEntity> entities, int activeIndex);

        /// <summary>
        /// Preview will only show the image if there is a presentation window opened. Moreover,
        /// the presentation window will not get focus even if it is open.
        /// </summary>
        /// <param name="entities">Entities to show in the presentation</param>
        /// <param name="activeIndex">Index of image to show</param>
        /// <returns>Task finished when the image is loaded</returns>
        Task PreviewAsync(IEnumerable<IEntity> entities, int activeIndex);
    }
}
