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
        /// Add a new tool named <paramref name="toolName"/> to the group named <paramref name="groupName"/>.
        /// </summary>
        /// <param name="groupName">Name of the group to put the tool into. A new group will be created if there are no tools in this group. This name is case insensitive.</param>
        /// <param name="toolName">Name of the tool within the group <paramref name="groupName"/>. This name is case insensitive.</param>
        /// <param name="toolTipText">Text shown in a tool tip when user places mouse cursor over this item</param>
        /// <param name="image">Image of the tool shown in the tool bar</param>
        /// <param name="action">Action triggered whenever a user clicks on the tool.</param>
        /// <returns>Tool handle which lets the caller change the tool properties on the fly.</returns>
        /// <exception cref="ArgumentNullException">An argument is null</exception>
        /// <exception cref="ArgumentException"><paramref name="toolName"/> is not unique within group <paramref name="groupName"/></exception>
        IToolBarItem CreateToolBarItem(string groupName, string toolName, string toolTipText, Image image, Action action);

        /// <summary>
        /// Create a new tool bar select box named <paramref name="toolName"/> to the group named
        /// <paramref name="groupName"/>.
        /// </summary>
        /// <param name="groupName">Name of the group</param>
        /// <param name="toolName">Name of the tool</param>
        /// <param name="toolTipText">Text shown in the tooltip when user points to this item.</param>
        /// <param name="image">Tool bar icon</param>
        /// <returns>Select box item</returns>
        IToolBarDropDown CreateToolBarSelect(string groupName, string toolName, string toolTipText, Image image);

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
