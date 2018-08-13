using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Query
{
    public class QueryView : IEquatable<QueryView>
    {
        /// <summary>
        /// View name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Query text
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Path to a file where this view is stored.
        /// </summary>
        public string Path { get; }

        public QueryView(string name, string text, string path)
        {
            Name = name;
            Text = text;
            Path = path;
        }

        public bool Equals(QueryView other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return string.Equals(Name, other.Name) && string.Equals(Text, other.Text);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((QueryView) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ (Text != null ? Text.GetHashCode() : 0);
            }
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Query view repository is a thread-safe collection of <see cref="T:Viewer.Query.QueryView" />s.
    /// <see cref="T:Viewer.Query.QueryView" /> is a query which is used frequently. 
    /// </summary>
    public interface IQueryViewRepository : IEnumerable<QueryView>
    {
        /// <summary>
        /// Event occurs when a view in the repository changes (i.e., it is removed, added, updated).
        /// </summary>
        event EventHandler Changed;

        /// <summary>
        /// Add query view. If a view with the same name exists already, it will be replaced.
        /// </summary>
        /// <param name="view">View to add</param>
        void Add(QueryView view);

        /// <summary>
        /// Remove view with given name.
        /// </summary>
        /// <param name="viewName">Name of the view to remove</param>
        void Remove(string viewName);

        /// <summary>
        /// Replace current views with <paramref name="views"/>.
        /// This change will trigger one <see cref="Changed"/> event.
        /// </summary>
        /// <param name="views">New views</param>
        void Replace(IEnumerable<QueryView> views);

        /// <summary>
        /// Find a view with given name.
        /// </summary>
        /// <param name="viewName">Name of the view</param>
        /// <returns>View with given name or null if there is none</returns>
        QueryView Find(string viewName);
    }

    [Export(typeof(IQueryViewRepository))]
    public class QueryViewRepository : IQueryViewRepository
    {
        private readonly Dictionary<string, QueryView> _views = new Dictionary<string, QueryView>();

        public event EventHandler Changed;

        public void Add(QueryView view)
        {
            lock (_views)
            {
                _views[view.Name] = view;
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void Remove(string viewName)
        {
            var isRemoved = false;
            lock (_views)
            {
                isRemoved = _views.Remove(viewName);
            }

            if (isRemoved)
            {
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Replace(IEnumerable<QueryView> views)
        {
            lock (_views)
            {
                _views.Clear();
                foreach (var view in views)
                {
                    _views[view.Name] = view;
                }
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }

        public QueryView Find(string viewName)
        {
            lock (_views)
            {
                if (_views.TryGetValue(viewName, out var view))
                {
                    return view;
                }
            }

            return null;
        }

        public IEnumerator<QueryView> GetEnumerator()
        {
            lock (_views)
            {
                var views = _views.Values.ToList();
                return views.GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
