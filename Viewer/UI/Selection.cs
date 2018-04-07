using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.UI
{
    public interface ISelection : IEnumerable<string>
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
        void Replace(IEnumerable<string> newSelection);
        
        /// <summary>
        /// Check whether given item is in selection
        /// </summary>
        /// <param name="item"></param>
        /// <returns>true iff <paramref name="item"/> is in selection</returns>
        bool Contains(string item);

        /// <summary>
        /// Remove all items from the selection and call the Changed event.
        /// </summary>
        void Clear();
    }

    public class Selection : ISelection
    {
        private ISet<string> _currentSelection = new HashSet<string>();

        public event EventHandler Changed;

        public int Count => _currentSelection.Count;

        public IEnumerator<string> GetEnumerator()
        {
            return _currentSelection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Replace(IEnumerable<string> newSelection)
        {
            _currentSelection.Clear();
            _currentSelection.UnionWith(newSelection);

            // notify listeners
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public bool Contains(string item)
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
