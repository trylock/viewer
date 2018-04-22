using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;
using Viewer.Images;

namespace Viewer.UI.Images
{
    public interface IThumbnailSizeCalculator
    {
        /// <summary>
        /// Minimal size along each axis
        /// </summary>
        int MinimalSize { get; set; }
        
        /// <summary>
        /// Compute thumbnail size of given entities
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        Size ComputeMinimalSize(IEnumerable<IEntity> entities);
    }

    public class FrequentRatioThumbnailSizeCalculator : IThumbnailSizeCalculator
    {
        private int _minimalSize;
        private readonly IImageLoader _loader;

        public int MinimalSize
        {
            get => _minimalSize;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _minimalSize = value;
            }
        }
        
        /// <summary>
        /// Construct thumbnail size calculator
        /// </summary>
        /// <param name="loader">Image loader</param>
        /// <param name="minimalSize">Minimal size along each axis</param>
        public FrequentRatioThumbnailSizeCalculator(IImageLoader loader, int minimalSize)
        {
            MinimalSize = minimalSize;

            _loader = loader;
            _minimalSize = minimalSize;
        }

        /// <inheritdoc />
        /// <summary>
        /// Compute the size from the most common aspect ratio
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        public Size ComputeMinimalSize(IEnumerable<IEntity> entities)
        {
            var frequency = new Dictionary<Fraction, int>();
            foreach (var entity in entities)
            {
                var size = _loader.GetImageSize(entity);
                var ratio = new Fraction(size.Width, size.Height);
                if (frequency.ContainsKey(ratio))
                {
                    frequency[ratio]++;
                }
                else
                {
                    frequency.Add(ratio, 1);
                }
            }
            
            // find the most common aspect ratio
            var maxFrequency = 0;
            var maxRatio = new Fraction(4, 3);
            foreach (var pair in frequency)
            {
                if (pair.Value > maxFrequency)
                {
                    maxFrequency = pair.Value;
                    maxRatio = pair.Key;
                }
            }

            // determine the size
            var aspectRatio = (double)maxRatio;
            if (aspectRatio > 1)
            {
                return new Size((int)(_minimalSize * aspectRatio), _minimalSize);
            }
            return new Size(_minimalSize, (int)(_minimalSize / aspectRatio));
        }
    }
}
