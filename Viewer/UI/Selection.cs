using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.UI
{
    public interface ISelection : IEnumerable<Entity>
    {
        /// <summary>
        /// Event called when the selection changes
        /// </summary>
        event EventHandler Changed;

        int Count { get; }

        /// <summary>
        /// Replace current selection with <paramref name="newSelection"/>
        /// </summary>
        /// <param name="newSelection">Newly selection items</param>
        void Replace(IEnumerable<Entity> newSelection);
        
        /// <summary>
        /// Check whether given item is in selection
        /// </summary>
        /// <param name="item"></param>
        /// <returns>true iff <paramref name="item"/> is in selection</returns>
        bool Contains(Entity item);

        /// <summary>
        /// Remove all items from the selection and call the Changed event.
        /// </summary>
        void Clear();
    }

    public class Selection : ISelection
    {
        private ISet<Entity> _currentSelection = new HashSet<Entity>();

        public event EventHandler Changed;

        public int Count => _currentSelection.Count;

        public IEnumerator<Entity> GetEnumerator()
        {
            return _currentSelection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Replace(IEnumerable<Entity> newSelection)
        {
            _currentSelection.Clear();
            _currentSelection.UnionWith(newSelection);

            // notify listeners
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public bool Contains(Entity item)
        {
            return _currentSelection.Contains(item);
        }

        public void Clear()
        {
            _currentSelection.Clear();
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
