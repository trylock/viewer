using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.UI
{
    /// <inheritdoc />
    /// <summary>
    /// Collection of entities currently selected by the user.
    /// It contains file and directory entities.
    /// This type is *not* thread safe. All functions and properties have to be used from the UI thread.
    /// </summary>
    public interface ISelection : IEnumerable<IEntity>
    {
        /// <summary>
        /// Event occurs when the selection changes.
        /// </summary>
        event EventHandler Changed;

        /// <summary>
        /// Number of items currently in selection.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Replace current selection with <paramref name="newSelection"/>
        /// </summary>
        /// <param name="newSelection">Newly selected items</param>
        /// <exception cref="ArgumentNullException"><paramref name="newSelection"/> is null</exception>
        void Replace(IEnumerable<IEntity> newSelection);
        
        /// <summary>
        /// Remove all items from the selection and call the Changed event.
        /// </summary>
        void Clear();
    }

    [Export(typeof(ISelection))]
    public class Selection : ISelection
    {
        private readonly List<IEntity> _currentSelection = new List<IEntity>();

        public event EventHandler Changed;

        public int Count => _currentSelection.Count;

        public IEnumerator<IEntity> GetEnumerator()
        {
            return _currentSelection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        public void Replace(IEnumerable<IEntity> newSelection)
        {
            if (newSelection == null)
            {
                throw new ArgumentNullException(nameof(newSelection));
            }
            
            _currentSelection.Clear();
            _currentSelection.AddRange(newSelection);

            // notify listeners
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void Clear()
        {
            _currentSelection.Clear();
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
