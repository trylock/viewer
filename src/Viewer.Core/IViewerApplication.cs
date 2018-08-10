using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer
{
    public interface IViewerApplication
    {
        /// <summary>
        /// Initialize components of the application
        /// </summary>
        void InitializeLayout();

        /// <summary>
        /// Add an option to the main application menu
        /// </summary>
        /// <param name="menuPath">Name of the menu where to put this item (last item is the name of the new menu item)</param>
        /// <param name="action">Function executed when user clicks on the item</param>
        /// <param name="icon">Icon shown next to the name</param>
        void AddMenuItem(IReadOnlyList<string> menuPath, Action action, Image icon);

        /// <summary>
        /// Add layout deserializa callback.
        /// </summary>
        /// <param name="callback">
        ///     The callback gets a persist string to deserialize.
        ///     If it does not recognize the string, it has to return null.
        ///     If it returns null, the deserializer will try to use another deserialization function.
        /// </param>
        void AddLayoutDeserializeCallback(DeserializeDockContent callback);

        /// <summary>
        /// Run the application
        /// </summary>
        void Run();
    }
}
