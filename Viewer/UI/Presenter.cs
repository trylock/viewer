using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI
{
    public abstract class Presenter
    {
        public struct AutoEventSubscription
        {
            /// <summary>
            /// Name of the event
            /// </summary>
            public string EventName { get; set; }

            /// <summary>
            /// Handler method name or null if handler for this event was not found
            /// </summary>
            public string HandlerName { get; set; }
        }

        [Import]
        private ViewerForm _appForm;
        
        /// <summary>
        /// Main view of the presenter
        /// </summary>
        public abstract IWindowView MainView { get; }

        /// <summary>
        /// Show presenter's view
        /// </summary>
        /// <param name="dockState">Dock state of the view</param>
        public virtual void ShowView(DockState dockState)
        {
            MainView.Show(_appForm.Panel, dockState);
        }

        /// <summary>
        /// Automatically subscribe to all view events.
        /// For each EventName in <paramref name="view"/> find method <paramref name="eventHandlerPrefix"/>_EventName 
        /// in this presenter and subsribe this method to the event.
        /// </summary>
        /// <typeparam name="TView"></typeparam>
        /// <param name="view"></param>
        /// <param name="eventHandlerPrefix"></param>
        /// <returns></returns>
        public List<AutoEventSubscription> SubscribeTo<TView>(
            TView view,
            string eventHandlerPrefix)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));
            if (eventHandlerPrefix == null)
                throw new ArgumentNullException(nameof(eventHandlerPrefix));

            var result = new List<AutoEventSubscription>();

            var presenterType = GetType();
            foreach (var eventInfo in view.GetType().GetEvents())
            {
                // find event handler method in the presenter
                var handlerName = eventHandlerPrefix + "_" + eventInfo.Name;
                var method = presenterType.GetMethod(handlerName, BindingFlags.NonPublic | BindingFlags.Instance);
                if (method == null)
                {
                    result.Add(new AutoEventSubscription
                    {
                        EventName = eventInfo.Name,
                        HandlerName = null
                    });
                }
                else
                {
                    // subscribe to that event
                    eventInfo.AddEventHandler(view, Delegate.CreateDelegate(eventInfo.EventHandlerType, this, method));
                    result.Add(new AutoEventSubscription
                    {
                        EventName = eventInfo.Name,
                        HandlerName = handlerName
                    });
                }
            }

            return result;
        }
    }
}
