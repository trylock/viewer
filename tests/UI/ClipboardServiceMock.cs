using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.UI;

namespace ViewerTest.UI
{
    public class ClipboardServiceMock : IClipboardService
    {
        private List<string> _data = new List<string>();
        private DragDropEffects _effect;

        public IEnumerable<string> GetFiles()
        {
            return _data;
        }

        public void SetFiles(IEnumerable<string> files)
        {
            _data.Clear();
            _data.AddRange(files);
        }

        public void SetPreferredEffect(DragDropEffects effect)
        {
            _effect = effect;
        }

        public DragDropEffects GetPreferredEffect()
        {
            return _effect;
        }
    }
}
