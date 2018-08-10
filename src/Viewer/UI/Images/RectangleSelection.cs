using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.UI.Images
{
    public enum SelectionStrategy
    {
        /// <summary>
        /// Previous selection will be replaced with the new selection.
        /// </summary>
        Replace,

        /// <summary>
        /// Compute union with the old selection.
        /// </summary>
        Union,

        /// <summary>
        /// Compute symetric difference with the previous selection.
        /// </summary>
        SymetricDifference,
    }

    public class RectangleSelection<T> : IEnumerable<T>
    {
        private readonly HashSet<T> _previousSelection;
        private readonly HashSet<T> _currentSelection;

        /// <summary>
        /// Current selection strategy
        /// </summary>
        public SelectionStrategy Strategy { get; private set; }

        /// <summary>
        /// First point of the selection
        /// </summary>
        public Point StartPoint { get; private set; }

        /// <summary>
        /// Is the selection currently active
        /// </summary>
        public bool IsActive { get; private set; }

        public RectangleSelection(IEqualityComparer<T> comparer)
        {
            _previousSelection = new HashSet<T>(comparer);
            _currentSelection = new HashSet<T>(comparer);
        }

        /// <summary>
        /// Start a new range selection at <paramref name="location"/>
        /// </summary>
        /// <param name="location"></param>
        /// <param name="strategy"></param>
        public void Begin(Point location, SelectionStrategy strategy)
        {
            IsActive = true;
            StartPoint = location;
            Strategy = strategy;

            // make current selection the previous selection
            _previousSelection.Clear();
            _previousSelection.UnionWith(_currentSelection);

            // reset current selection
            if (Strategy == SelectionStrategy.Replace)
            {
                _currentSelection.Clear();
            }
        }

        /// <summary>
        /// End current selection (e.g. when user releases a mouse button)
        /// </summary>
        public void End()
        {
            IsActive = false;
            Strategy = SelectionStrategy.Replace;
        }

        /// <summary>
        /// Add <paramref name="items"/> to selection.
        /// Whether or not an item will be added depends on current selection strategy and previous selection.
        /// </summary>
        /// <param name="items"></param>
        /// <returns>true iff the selection has changed</returns>
        public bool Set(IEnumerable<T> items)
        {
            var oldSelection = _currentSelection.ToArray();

            _currentSelection.Clear();
            _currentSelection.UnionWith(items);

            switch (Strategy)
            {
                case SelectionStrategy.Union:
                    _currentSelection.UnionWith(_previousSelection);
                    break;
                case SelectionStrategy.SymetricDifference:
                    _currentSelection.SymmetricExceptWith(_previousSelection);
                    break;
            }

            return !_currentSelection.SetEquals(oldSelection);
        }

        /// <summary>
        /// Check whether <paramref name="item"/> is in current selection
        /// </summary>
        /// <param name="item"></param>
        /// <returns>true iff <paramref name="item"/> is in current selection</returns>
        public bool Contains(T item)
        {
            return _currentSelection.Contains(item);
        }

        /// <summary>
        /// Remove all items from current and previous selection
        /// </summary>
        public void Clear()
        {
            _currentSelection.Clear();
            _previousSelection.Clear();
        }

        /// <summary>
        /// Get selection bounds 
        /// </summary>
        /// <param name="endLocation"></param>
        /// <returns>Selection bounds if the selection is active or an empty rectangle</returns>
        public Rectangle GetBounds(Point endLocation)
        {
            if (!IsActive)
            {
                return Rectangle.Empty;
            }
            var minX = Math.Min(StartPoint.X, endLocation.X);
            var maxX = Math.Max(StartPoint.X, endLocation.X);
            var minY = Math.Min(StartPoint.Y, endLocation.Y);
            var maxY = Math.Max(StartPoint.Y, endLocation.Y);
            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _currentSelection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
