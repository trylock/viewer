using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.UI.Images
{
    public interface IThumbnailGenerator
    {
        /// <summary>
        /// Generate thumbnail image of <paramref name="original"/>.
        /// The thumbnail is scaled down/up to fill in <paramref name="thumbnailArea"/> 
        /// in one direction.
        /// </summary>
        /// <param name="original">Original image</param>
        /// <param name="thumbnailArea">Size of an area for the thumbnail</param>
        /// <returns>Thumbnail image</returns>
        Image GetThumbnail(Image original, Size thumbnailArea);
    }
}
