using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;
using Viewer.UI;

namespace ViewerTest.UI
{
    public class SelectionMock : ISelection
    {
        public void TriggerChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private ISet<string> _selection = new HashSet<string>();

        public event EventHandler Changed;
        public int Count => _selection.Count;

        public IEnumerator<string> GetEnumerator()
        {
            return _selection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Replace(IEnumerable<string> newSelection)
        {
            _selection.Clear();
            _selection.UnionWith(newSelection);
        }

        public bool Contains(string item)
        {
            return _selection.Contains(item);
        }

        public void Clear()
        {
            _selection.Clear();
        }
    }
}
