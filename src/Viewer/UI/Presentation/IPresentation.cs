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
        /// <summary>
        /// Open presentation of <paramref name="entities"/> where <paramref name="activeIndex"/>
        /// is the index of an active entity in <paramref name="entities"/>..
        /// </summary>
        /// <param name="entities">Entities in the presentation</param>
        /// <param name="activeIndex">Index of an entity to open</param>
        void Open(IEnumerable<IEntity> entities, int activeIndex);
    }
}
