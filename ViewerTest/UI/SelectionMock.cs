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

        private ISet<int> _selection = new HashSet<int>();

        public IEnumerator<int> GetEnumerator()
        {
            return _selection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public event EventHandler Changed;
        public int Count => _selection.Count;
        public IEntityManager Items { get; private set; } 

        public void Replace(IEntityManager entityManager, IEnumerable<int> newSelection)
        {
            Items = entityManager;
            _selection.Clear();
            _selection.UnionWith(newSelection);
        }

        public bool Contains(int item)
        {
            return _selection.Contains(item);
        }

        public void Clear()
        {
            _selection.Clear();
            Items = null;
        }
    }
}
