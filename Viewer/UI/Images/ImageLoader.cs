using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Primitives;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.UI.Images
{
    public interface IImageLoader
    {
        /// <summary>
        /// Get size of the image of entity without reading the whole image
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Size GetImageSize(IEntity entity);

        /// <summary>
        /// Load image of an entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>Full image of the entity</returns>
        Image LoadImage(IEntity entity);

        /// <summary>
        /// Load thumbnail of an entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="thumbnailAreaSize">Size of an area for the thumbnail</param>
        /// <returns>Thumbnail of the entity</returns>
        Image LoadThumbnail(IEntity entity, Size thumbnailAreaSize);
    }

    [Export(typeof(IImageLoader))]
    public class ImageLoader : IImageLoader
    {
        private const string OrientationAttrName = "orientation";
        private const string WidthAttrName = "ImageWidth";
        private const string HeightAttrName = "ImageHeight";
        private const string ThumbnailAttrName = "thumbnail";

        /// <summary>
        /// Rotate/flip transformation which fixes image orientation for each possible orientation value.
        /// Index in this array is a value of the orientation tag as defined in Exif 2.2
        /// </summary>
        private readonly RotateFlipType[] _orientationFixTransform =
        {
            RotateFlipType.RotateNoneFlipNone,  // invalid orientation value

            RotateFlipType.RotateNoneFlipNone,  // top left
            RotateFlipType.RotateNoneFlipX,     // top right
            RotateFlipType.Rotate180FlipNone,   // bottom right
            RotateFlipType.Rotate180FlipX,      // bottom left
            RotateFlipType.Rotate90FlipX,       // left top
            RotateFlipType.Rotate90FlipNone,    // right top
            RotateFlipType.Rotate270FlipX,      // right bottom
            RotateFlipType.Rotate270FlipNone,   // left bottom
        };

        private readonly IThumbnailGenerator _thumbnailGenerator;

        [ImportingConstructor]
        public ImageLoader(IThumbnailGenerator generator)
        {
            _thumbnailGenerator = generator;
        }

        /// <summary>
        /// Get orientation value from entity 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>Orientation of the entity or 0</returns>
        private static int GetOrientation(IEntity entity)
        {
            var orientationAttr = entity.GetAttribute(OrientationAttrName) as IntAttribute;
            return orientationAttr == null ? 0 : orientationAttr.Value;
        }

        /// <summary>
        /// Get image transformation which will fix the image orientation
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>Transformation which fixes the image orientation</returns>
        private RotateFlipType GetTransformation(IEntity entity)
        {
            var orientation = GetOrientation(entity);
            if (orientation < 0 || orientation >= _orientationFixTransform.Length)
            {
                return RotateFlipType.RotateNoneFlipNone;
            }

            return _orientationFixTransform[orientation];
        }

        /// <summary>
        /// Fix image orientation based on the orientation attribute (loaded from the exif orientation tag)
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="image"></param>
        private void FixImageOrientation(IEntity entity, Image image)
        {
            var fix = GetTransformation(entity);
            if (fix != RotateFlipType.RotateNoneFlipNone)
            {
                image.RotateFlip(fix);
            }
        }

        public Size GetImageSize(IEntity entity)
        {
            var widthAttr = entity.GetAttribute(WidthAttrName) as IntAttribute;
            var heightAttr = entity.GetAttribute(HeightAttrName) as IntAttribute;
            if (widthAttr == null || heightAttr == null)
            {
                return new Size(1, 1);
            }

            var orientation = GetOrientation(entity);
            return orientation < 5 ? 
                new Size(widthAttr.Value, heightAttr.Value) : 
                new Size(heightAttr.Value, widthAttr.Value);
        }

        public Image LoadImage(IEntity entity)
        {
            var image = Image.FromStream(new MemoryStream(File.ReadAllBytes(entity.Path)));
            FixImageOrientation(entity, image);
            return image;
        }

        public Image LoadThumbnail(IEntity entity, Size thumbnailAreaSize)
        {
            var attr = entity.GetAttribute(ThumbnailAttrName) as ImageAttribute;
            if (attr == null)
            {
                return null;
            }

            using (var image = Image.FromStream(new MemoryStream(attr.Value)))
            {
                FixImageOrientation(entity, image);
                return _thumbnailGenerator.GetThumbnail(image, thumbnailAreaSize);
            }
        }
    }
}
