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
        private ISet<Entity> _selection = new HashSet<Entity>();

        public event EventHandler Changed;
        public int Count => _selection.Count;

        public IEnumerator<Entity> GetEnumerator()
        {
            return _selection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Replace(IEnumerable<Entity> newSelection)
        {
            _selection.Clear();
            _selection.UnionWith(newSelection);
        }

        public bool Contains(Entity item)
        {
            return _selection.Contains(item);
        }

        public void Clear()
        {
            _selection.Clear();
        }
    }
}
