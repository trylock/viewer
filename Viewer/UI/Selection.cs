using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.UI
{
    public interface ISelection : IEnumerable<int>
    {
        /// <summary>
        /// Event called when the selection changes
        /// </summary>
        event EventHandler Changed;

        /// <summary>
        /// Number of items in selection
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Query result object of the selection.
        /// </summary>
        IEntityManager Items { get; }

        /// <summary>
        /// Replace current selection with <paramref name="newSelection"/>
        /// </summary>
        /// <param name="entityManager">Source of entities.</param>
        /// <param name="newSelection">Newly selected items</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     An index from <paramref name="newSelection"/> is out of range of <paramref name="entityManager"/>
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="entityManager"/> or <paramref name="newSelection"/> is null</exception>
        void Replace(IEntityManager entityManager, IEnumerable<int> newSelection);
        
        /// <summary>
        /// Check whether given item is in selection
        /// </summary>
        /// <param name="item">Index of an item</param>
        /// <returns>true iff <paramref name="item"/> is in selection</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="item"/> is out of range</exception>
        bool Contains(int item);

        /// <summary>
        /// Remove all items from the selection and call the Changed event.
        /// It will also set Items to null.
        /// </summary>
        void Clear();
    }

    public class Selection : ISelection
    {
        private ISet<int> _currentSelection = new HashSet<int>();

        public event EventHandler Changed;

        public int Count => _currentSelection.Count;
        public IEntityManager Items { get; private set; }

        public IEnumerator<int> GetEnumerator()
        {
            return _currentSelection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        public void Replace(IEntityManager entityManager, IEnumerable<int> newSelection)
        {
            if (newSelection == null)
            {
                throw new ArgumentNullException(nameof(newSelection));
            }

            Items = entityManager;
            _currentSelection.Clear();
            foreach (var item in newSelection)
            {
                if (item < 0 || item >= Items.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(item));
                }

                _currentSelection.Add(item);
            }

            // notify listeners
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public bool Contains(int item)
        {
            if (item < 0 || item >= Items.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(item));
            }
            return _currentSelection.Contains(item);
        }

        public void Clear()
        {
            Items = null;
            _currentSelection.Clear();
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
