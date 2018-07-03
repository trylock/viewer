using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Query
{
    public interface IQueryViewRepository : IDictionary<string, string>
    {
    }

    [Export(typeof(IQueryViewRepository))]
    public class QueryViewRepository : IQueryViewRepository
    {
        private readonly Dictionary<string, string> _views = new Dictionary<string, string>();

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _views.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<string, string> item)
        {
            ((IDictionary<string, string>) _views).Add(item);
        }

        public void Clear()
        {
            _views.Clear();
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            return _views.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            ((IDictionary<string, string>) _views).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            return ((IDictionary<string, string>)_views).Remove(item);
        }

        public int Count => _views.Count;

        public bool IsReadOnly => false;

        public bool ContainsKey(string key)
        {
            return _views.ContainsKey(key);
        }

        public void Add(string key, string value)
        {
            _views.Add(key, value);
        }

        public bool Remove(string key)
        {
            return _views.Remove(key);
        }

        public bool TryGetValue(string key, out string value)
        {
            return _views.TryGetValue(key, out value);
        }

        public string this[string key]
        {
            get => _views[key];
            set => _views[key] = value;
        }

        public ICollection<string> Keys => _views.Keys;
        public ICollection<string> Values => _views.Values;
    }
}
