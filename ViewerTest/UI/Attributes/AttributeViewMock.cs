using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.UI.Attributes;

namespace ViewerTest.UI.Attributes
{
    class AttributeViewMock : IAttributeView
    {
        // mock interface

        /// <summary>
        /// List of updated indices
        /// </summary>
        public List<int> Updated { get; set; } = new List<int>();

        /// <summary>
        /// Last error type or null
        /// </summary>
        public string LastError = null;

        public void TriggerAttributeChanges(int index, AttributeView oldView, AttributeView newView)
        {
            AttributeChanged?.Invoke(this, new AttributeChangedEventArgs
            {
                Index = index,
                OldValue = oldView,
                NewValue = newView
            });
        }

        // mocked interface
        public event EventHandler SaveAttributes;
        public event EventHandler<AttributeChangedEventArgs> AttributeChanged;
        public event EventHandler<AttributeDeletedEventArgs> AttributeDeleted;
        public event EventHandler<SortEventArgs> SortAttributes;

        public bool EditingEnabled { get; set; }

        public List<AttributeView> Attributes { get; set; } = new List<AttributeView>();

        public void UpdateAttributes()
        {
            Updated.AddRange(Enumerable.Range(0, Attributes.Count));
        }

        public void UpdateAttribute(int index)
        {
            Updated.Add(index);
        }

        public void AttributeNameIsNotUnique(string name)
        {
            LastError = "not unique: " + name;
        }

        public void AttributeNameIsEmpty()
        {
            LastError = "empty";
        }
    }
}
