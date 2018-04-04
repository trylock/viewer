using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.UI
{
    public class ViewEvent : System.Attribute
    {
        /// <summary>
        /// View which triggers the event
        /// </summary>
        public Type ViewType { get; set; }

        /// <summary>
        /// Name of the event
        /// </summary>
        public string Name { get; set; }

        public ViewEvent(Type view, string name)
        {
            ViewType = view;
            Name = name;
        }
    }

    public static class PresenterUtils
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

        /// <summary>
        /// Automatically subscribe to all view events.
        /// For each EventName in <paramref name="view"/> find method <paramref name="eventHandlerPrefix"/>_EventName 
        /// in <paramref name="presenter"/> and subsribe this method to the event.
        /// </summary>
        /// <typeparam name="TView"></typeparam>
        /// <typeparam name="TPresenter"></typeparam>
        /// <param name="view"></param>
        /// <param name="presenter"></param>
        /// <param name="eventHandlerPrefix"></param>
        /// <returns></returns>
        public static List<AutoEventSubscription> SubscribeTo<TView, TPresenter>(
            TView view, 
            TPresenter presenter, 
            string eventHandlerPrefix)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));
            if (presenter == null)
                throw new ArgumentNullException(nameof(presenter));
            if (eventHandlerPrefix == null)
                throw new ArgumentNullException(nameof(eventHandlerPrefix));

            var result = new List<AutoEventSubscription>();
            
            var presenterType = presenter.GetType();
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
                    eventInfo.AddEventHandler(view, Delegate.CreateDelegate(eventInfo.EventHandlerType, presenter, method));
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
