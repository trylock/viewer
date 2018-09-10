using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.UI.Images
{
    internal interface ISelectionStrategy<T>
    {
        /// <summary>
        /// Change current selection at the start.
        /// </summary>
        /// <param name="currentSelection"></param>
        void Initialize(HashSet<T> currentSelection);

        /// <summary>
        /// Compute a new selection using this strategy. Items are added to/removed from
        /// <paramref name="currentSelection"/>.
        /// </summary>
        /// <param name="currentSelection">New items in the selection</param>
        /// <param name="previousSelection">Previous selection</param>
        /// <param name="allItems">Áll available items</param>
        void Set(HashSet<T> currentSelection, HashSet<T> previousSelection, IReadOnlyList<T> allItems);
    }

    internal class ReplaceSelectionStrategy<T> : ISelectionStrategy<T>
    {
        public static ReplaceSelectionStrategy<T> Default { get; } = new ReplaceSelectionStrategy<T>();

        public void Initialize(HashSet<T> currentSelection)
        {
            currentSelection.Clear();
        }

        public void Set(HashSet<T> currentSelection, HashSet<T> previousSelection, IReadOnlyList<T> allItems)
        {
            // no-op, leave the current selection be as it is.
        }
    }

    internal class UnionSelectionStrategy<T> : ISelectionStrategy<T>
    {
        public static UnionSelectionStrategy<T> Default { get; } = new UnionSelectionStrategy<T>();

        public void Initialize(HashSet<T> currentSelection)
        {
        }

        public void Set(HashSet<T> currentSelection, HashSet<T> previousSelection, IReadOnlyList<T> allItems)
        {
            currentSelection.UnionWith(previousSelection);
        }
    }

    internal class SymetricDifferenceSelectionStrategy<T> : ISelectionStrategy<T>
    {
        public static SymetricDifferenceSelectionStrategy<T> Default { get; } = new SymetricDifferenceSelectionStrategy<T>();

        public void Initialize(HashSet<T> currentSelection)
        {
        }

        public void Set(HashSet<T> currentSelection, HashSet<T> previousSelection, IReadOnlyList<T> allItems)
        {
            currentSelection.SymmetricExceptWith(previousSelection);
        }
    }

    internal class RangeSelectionStrategy<T> : ISelectionStrategy<T>
    {
        public static RangeSelectionStrategy<T> Default { get; } = new RangeSelectionStrategy<T>();

        public void Initialize(HashSet<T> currentSelection)
        {
        }

        public void Set(HashSet<T> currentSelection, HashSet<T> previousSelection, IReadOnlyList<T> allItems)
        {
            if (currentSelection.Count == 0)
            {
                currentSelection.UnionWith(previousSelection);
                return;
            }
            else if (previousSelection.Count == 0)
            {
                return;
            }

            var currentStart = -1;
            var currentEnd = -1;
            var prevStart = -1;
            var prevEnd = -1;

            // find ranges of previous and current selection
            for (var i = 0; i < allItems.Count; ++i)
            {
                var item = allItems[i];
                if (currentSelection.Contains(item))
                {
                    if (currentStart < 0)
                    {
                        currentStart = i;
                    }

                    currentEnd = i;
                }

                if (previousSelection.Contains(item))
                {
                    if (prevStart < 0)
                    {
                        prevStart = i;
                    }

                    prevEnd = i;
                }
            }

            Trace.Assert(currentStart >= 0);
            Trace.Assert(prevStart >= 0);
            Trace.Assert(currentStart <= currentEnd);
            Trace.Assert(prevStart <= prevEnd);

            // Always select the whole current selection range. The previous range is only selected
            // as a whole if it is before the end of current selection.
            currentStart = Math.Min(currentStart, prevStart);
            currentEnd = Math.Max(currentEnd, prevStart);

            if (currentStart < 0)
            {
                // reset the selection
                currentSelection.Clear();
                currentSelection.UnionWith(previousSelection);
                return;
            }

            Trace.Assert(currentStart <= currentEnd);

            currentSelection.Clear();
            currentSelection.UnionWith(Range(allItems, currentStart, currentEnd + 1));
        }

        private static IEnumerable<T> Range(IReadOnlyList<T> list, int begin, int end)
        {
            for (; begin < end; ++begin)
            {
                yield return list[begin];
            }
        }
    }

    internal class RectangleSelection<T> : IEnumerable<T>
    {
        private readonly HashSet<T> _previousSelection;
        private readonly HashSet<T> _currentSelection;

        /// <summary>
        /// Current selection strategy
        /// </summary>
        public ISelectionStrategy<T> Strategy { get; private set; }

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
        public void Begin(Point location, ISelectionStrategy<T> strategy)
        {
            IsActive = true;
            StartPoint = location;
            Strategy = strategy;

            // make current selection the previous selection
            _previousSelection.Clear();
            _previousSelection.UnionWith(_currentSelection);

            Strategy.Initialize(_currentSelection);
        }

        /// <summary>
        /// End current selection (e.g. when user releases a mouse button)
        /// </summary>
        public void End()
        {
            IsActive = false;
            Strategy = null;
        }

        /// <summary>
        /// Add <paramref name="newlySelectedItems"/> to selection. Whether or not an item will be
        /// added depends on current selection strategy and previous selection. Current selection
        /// strategy is set in the <see cref="Begin"/> method.
        /// </summary>
        /// <param name="newlySelectedItems"></param>
        /// <param name="allItems"></param>
        /// <returns>true iff the selection has changed</returns>
        /// <exception cref="InvalidOperationException">
        /// The selection is not active (i.e., <see cref="IsActive"/> is false, call
        /// <see cref="Begin"/> before calling <see cref="Set"/>)
        /// </exception>
        public bool Set(IEnumerable<T> newlySelectedItems, IReadOnlyList<T> allItems)
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("Start the selection by calling the Being() method");
            }

            var oldSelection = _currentSelection.ToArray();

            _currentSelection.Clear();
            _currentSelection.UnionWith(newlySelectedItems);

            Strategy.Set(_currentSelection, _previousSelection, allItems);

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
