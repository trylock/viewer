using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.UI
{
    public class SelectionEventArgs : EventArgs
    {
        /// <summary>
        /// Current selection
        /// </summary>
        public ISelection Selection { get; }

        public SelectionEventArgs(ISelection selection)
        {
            Selection = selection;
        }
    }

    public interface ISelection : IEnumerable<AttributeCollection>
    {
        /// <summary>
        /// Event called when this selection changes
        /// </summary>
        event EventHandler<SelectionEventArgs> Changed;
        
        /// <summary>
        /// Replace items in selection with new items.
        /// This will trigger the Changed event exactly once.
        /// </summary>
        /// <param name="newSelectionItems">New selection</param>
        void Replace(IEnumerable<AttributeCollection> newSelectionItems);
    }

    public class Selection : ISelection
    {
        public event EventHandler<SelectionEventArgs> Changed;

        private List<AttributeCollection> _selection = new List<AttributeCollection>();

        public IEnumerator<AttributeCollection> GetEnumerator()
        {
            return _selection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Replace(IEnumerable<AttributeCollection> newSelectionItems)
        {
            _selection.Clear();
            _selection.AddRange(newSelectionItems);
            Changed?.Invoke(this, new SelectionEventArgs(this));
        }
    }
}
