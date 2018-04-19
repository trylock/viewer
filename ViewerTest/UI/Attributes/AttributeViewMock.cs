using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.UI.Attributes;
using WeifenLuo.WinFormsUI.Docking;

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

        public void TriggerAttributeChanges(int index, AttributeGroup oldGroup, AttributeGroup newGroup)
        {
            AttributeChanged?.Invoke(this, new AttributeChangedEventArgs
            {
                Index = index,
                OldValue = oldGroup,
                NewValue = newGroup
            });
        }

        public void TriggerSortAttributes(SortEventArgs args)
        {
            SortAttributes?.Invoke(this, args);
        }

        // mocked interface
        public event EventHandler SaveAttributes;
        public event EventHandler<AttributeChangedEventArgs> AttributeChanged;
        public event EventHandler<AttributeDeletedEventArgs> AttributeDeleted;
        public event EventHandler<SortEventArgs> SortAttributes;
        public event EventHandler<FilterEventArgs> FilterAttributes;

        public bool EditingEnabled { get; set; }
        public string Text { get; set; }

        public List<AttributeGroup> Attributes { get; set; } = new List<AttributeGroup>();

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

        public event EventHandler CloseView;
        public event EventHandler ViewGotFocus;

        public void EnsureVisible()
        {
            throw new NotImplementedException();
        }

        public IAsyncResult BeginInvoke(Delegate method)
        {
            throw new NotImplementedException();
        }

        public void Show(DockPanel dockPanel, DockState dockState)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }
    }
}
