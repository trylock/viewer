using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Viewer.Core.UI
{
    /// <summary>
    /// Inherited event keeps a path in a control tree of control's parents.
    /// <see cref="OnSubscribe"/> method is called whenever a new control is added to this path.
    /// <see cref="OnUnsubscribe"/> method is called whenever a control is removed from this path.
    /// This class maintains the path automatically.
    /// </summary>
    /// <remarks>
    /// Call the <see cref="Dispose"/> to unsubscribe all controls in the path from event handlers.
    /// </remarks>
    public abstract class InheritedEvent : IDisposable
    {
        private readonly List<Control> _controls = new List<Control>();

        public InheritedEvent(Control control)
        {
            Subscribe(control);
        }

        private void Control_ParentChanged(object sender, EventArgs e)
        {
            var control = (Control) sender;
            var index = _controls.IndexOf(control) + 1;
            UnsubscribeAllFrom(index);
            Subscribe(control.Parent);
        }

        private void Subscribe(Control control)
        {
            while (control != null)
            {
                control.ParentChanged += Control_ParentChanged;
                _controls.Add(control);
                OnSubscribe(control);
                control = control.Parent;
            }
        }

        private void UnsubscribeAllFrom(int firstIndex)
        {
            for (var i = firstIndex; i < _controls.Count; ++i)
            {
                var parentControl = _controls[i];
                parentControl.ParentChanged -= Control_ParentChanged;
                OnUnsubscribe(parentControl);
            }
            _controls.RemoveRange(firstIndex, _controls.Count - firstIndex);
        }

        public abstract void OnUnsubscribe(Control control);

        public abstract void OnSubscribe(Control control);

        public void Dispose()
        {
            UnsubscribeAllFrom(0);
        }
    }

    public class MovedOnScreenEvent : InheritedEvent
    {
        /// <summary>
        /// Event occurs whenever given control moves on screen
        /// </summary>
        public event EventHandler MovedOnScreen;

        public MovedOnScreenEvent(Control control) : base(control)
        {
        }

        public override void OnUnsubscribe(Control control)
        {
            control.Move -= Control_Move;
        }

        public override void OnSubscribe(Control control)
        {
            control.Move += Control_Move;
        }

        private void Control_Move(object sender, EventArgs e)
        {
            MovedOnScreen?.Invoke(sender, e);
        }

        public static MovedOnScreenEvent operator +(MovedOnScreenEvent self, EventHandler handler)
        {
            self.MovedOnScreen += handler;
            return self;
        }

        public static MovedOnScreenEvent operator -(MovedOnScreenEvent self, EventHandler handler)
        {
            self.MovedOnScreen -= handler;
            return self;
        }
    }

    public class ParentFormDeactivated : InheritedEvent
    {
        public event EventHandler Deactivated;

        public ParentFormDeactivated(Control control) : base(control)
        {
        }

        public override void OnUnsubscribe(Control control)
        {
            if (control is Form form)
            {
                form.Deactivate -= Form_Deactivate;
            }
        }

        public override void OnSubscribe(Control control)
        {
            var form = control as Form;
            if (form == null)
            {
                return;
            }

            if (form.Parent == null)
            {
                form.Deactivate += Form_Deactivate;
            }
        }

        private void Form_Deactivate(object sender, EventArgs e)
        {
            Deactivated?.Invoke(sender, e);
        }

        public static ParentFormDeactivated operator +(ParentFormDeactivated self, EventHandler handler)
        {
            self.Deactivated += handler;
            return self;
        }

        public static ParentFormDeactivated operator -(ParentFormDeactivated self, EventHandler handler)
        {
            self.Deactivated -= handler;
            return self;
        }
    }

    public static class ControlExtensions
    {
        /// <summary>
        /// Creates an event which is raised whenever control position changes on screen (i.e.,
        /// position of any control in the parent chain changes)
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        public static MovedOnScreenEvent CreateMovedOnScreenEvent(this Control control)
        {
            return new MovedOnScreenEvent(control);
        }

        /// <summary>
        /// Creates an event which is raised whenever control's parent form is deactivated. 
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        public static ParentFormDeactivated CreateParentFormDeactivatedEvent(this Control control)
        {
            return new ParentFormDeactivated(control);
        }
    }
}
