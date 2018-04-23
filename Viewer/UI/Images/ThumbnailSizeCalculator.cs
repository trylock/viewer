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
        /// Remove all entities
        /// </summary>
        void Reset();

        /// <summary>
        /// Compute the new thumbnail size after adding given entity
        /// </summary>
        /// <param name="entity">New entity</param>
        /// <returns>New minimal thumbnail size</returns>
        Size AddEntity(IEntity entity);
    }

    public class FrequentRatioThumbnailSizeCalculator : IThumbnailSizeCalculator
    {
        private int _minimalSize;
        private readonly IImageLoader _loader;
        private readonly Dictionary<Fraction, int> _frequency = new Dictionary<Fraction, int>();

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

        public void Reset()
        {
            _frequency.Clear();
        }
        
        public Size AddEntity(IEntity entity)
        {
            // update frequency of ratios
            var size = _loader.GetImageSize(entity);
            var ratio = new Fraction(size.Width, size.Height);
            if (_frequency.ContainsKey(ratio))
            {
                _frequency[ratio]++;
            }
            else
            {
                _frequency.Add(ratio, 1);
            }

            // Find the most common aspect ratio.
            // Number of aspect ratios should be low in a typical situaltion so it should be fine 
            // to iterate over the whole collection every time.
            var maxFrequency = 0;
            var maxRatio = new Fraction(4, 3);
            foreach (var pair in _frequency)
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
