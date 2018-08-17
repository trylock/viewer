using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScintillaNET;

namespace Viewer.UI.QueryEditor
{
    public class EditorControl : Scintilla
    {
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (e.KeyChar < 32)
            {
                e.Handled = true;
            }
            base.OnKeyPress(e);
        }
    }
}
