using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.UI
{
    public class ProgressViewFactory : IProgressViewFactory
    {
        public IProgressView Create()
        {
            return new ProgressViewForm();
        }
    }
}
