using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.UI.Attributes
{
    public interface IAttributeView
    {
        /// <summary>
        /// Show attribute in the view.
        /// If it is already there, update its value.
        /// </summary>
        /// <param name="attr">Attribute to show</param>
        void ShowAttribute(Attribute attr);
    }
}
