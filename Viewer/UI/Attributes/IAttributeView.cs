using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Attribute = Viewer.Data.Attribute;

namespace Viewer.UI.Attributes
{
    public class AttributeChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Changed attribute identifier in the view
        /// </summary>
        public int Index { get; set; } = -1;

        /// <summary>
        /// Old attribute
        /// </summary>
        public AttributeView OldValue { get; set; }

        /// <summary>
        /// New attribute
        /// </summary>
        public AttributeView NewValue { get; set; }
    }

    public class AttributeDeletedEventArgs : EventArgs
    {
        /// <summary>
        /// Deleted indices
        /// </summary>
        public IEnumerable<int> Deleted { get; set; }
    }

    public class SortEventArgs : EventArgs
    {
        /// <summary>
        /// Column by which we should sort the values
        /// </summary>
        public SortColumn Column { get; set; }
    }

    public enum SortColumn
    {
        None,
        Name,
        Value,
        Type
    }

    public enum SortDirection
    {
        Ascending,
        Descending
    }

    public enum AttributeType
    {
        Int,
        Double,
        String,
        DateTime,
    }

    public class AttributeView
    {
        /// <summary>
        /// Actual attribute the view represents
        /// </summary>
        public Attribute Data { get; set; }

        /// <summary>
        /// true iff value of this attribute is mixed (i.e. multiple entities have different value)
        /// </summary>
        public bool IsMixed { get; set; }
    }

    public interface IAttributeView
    {
        /// <summary>
        /// User requested to save all attributes to selected files.
        /// </summary>
        event EventHandler SaveAttributes;

        /// <summary>
        /// Event called when attribute name, value or type changes.
        /// </summary>
        event EventHandler<AttributeChangedEventArgs> AttributeChanged;

        /// <summary>
        /// Event called when an attribute has been removed.
        /// </summary>
        event EventHandler<AttributeDeletedEventArgs> AttributeDeleted;

        /// <summary>
        /// Event called when user requests to sort attributes byt given column
        /// </summary>
        event EventHandler<SortEventArgs> SortAttributes; 

        /// <summary>
        /// true iff it is enabled to edit and add attributes
        /// </summary>
        bool EditingEnabled { get; set; }

        /// <summary>
        /// Attributes shown in the view
        /// </summary>
        List<AttributeView> Attributes { get; set; }

        /// <summary>
        /// Update all attributes
        /// </summary>
        void UpdateAttributes();

        /// <summary>
        /// Update attribute at given position.
        /// If <paramref name="index"/> is out of range, no attribute will be updated.
        /// </summary>
        /// <param name="index">Index of an attribute</param>
        void UpdateAttribute(int index);
        
        /// <summary>
        /// Show the "attribute name is not unique" error dialog.
        /// </summary>
        /// <param name="name">Name of the attribute</param>
        void AttributeNameIsNotUnique(string name);

        /// <summary>
        /// Show the attribute name must not be empty error message.
        /// </summary>
        void AttributeNameIsEmpty();
    }
}
