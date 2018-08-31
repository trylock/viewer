using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using Viewer.Core.UI;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.Core
{
    public delegate IWindowView DeserializeCallback(string persistString);

    public interface IViewerApplication
    {
        /// <summary>
        /// Main application dock panel
        /// </summary>
        DockPanel Panel { get; }
        
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
        /// Create a new status bar item. This item is added to the application status bar.
        /// </summary>
        /// <param name="text">Text shown to the user</param>
        /// <param name="image">Image of the item.</param>
        /// <param name="alignment">Alignment of the item in the status bar.</param>
        /// <returns>Newly created status bar item</returns>
        IStatusBarItem CreateStatusBarItem(string text, Image image, ToolStripItemAlignment alignment);

        /// <summary>
        /// Create a new status bar slider item. This item is added to the application status bar.
        /// </summary>
        /// <param name="text">Text shown to the user in the status bar</param>
        /// <param name="image">Image of the item</param>
        /// <param name="alignment">Alignment of the item in the status bar</param>
        /// <returns>Newly created status bar slider</returns>
        IStatusBarSlider CreateStatusBarSlider(string text, Image image, ToolStripItemAlignment alignment);
        
        /// <summary>
        /// Add layout deserialize callback.
        /// </summary>
        /// <param name="callback">
        /// The callback gets a persist string to deserialize.
        /// If it does not recognize the string, it has to return null.
        /// If it returns null, the deserializer will try to use another deserialization function.
        /// </param>
        void AddLayoutDeserializeCallback(DeserializeCallback callback);

        /// <summary>
        /// Run the application
        /// </summary>
        void Run();
    }
}
