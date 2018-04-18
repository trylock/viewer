using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;
using Viewer.UI.Images;

namespace ViewerTest.UI.Images
{
    class ImageLoaderMock : IImageLoader
    {
        public Size GetImageSize(IEntity entity)
        {
            return new Size(1, 1);
        }

        public Image LoadImage(IEntity entity)
        {
            return null;
        }

        public Image LoadThumbnail(IEntity entity, Size thumbnailAreaSize)
        {
            return null;
        }
    }
}
