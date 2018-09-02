using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Core;

namespace Viewer
{
    public abstract class Component : IComponent
    {
        public IViewerApplication Application { get; set; }

        public virtual void OnStartup(IViewerApplication app)
        {

        }
    }
}
