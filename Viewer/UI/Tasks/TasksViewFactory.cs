using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Tasks
{
    public class TasksViewFactory
    {
        private readonly ViewerForm _appForm;

        [ImportingConstructor]
        public TasksViewFactory(ViewerForm appForm)
        {
            _appForm = appForm;
        }
        
        [Export(typeof(TasksView))]
        public TasksView CreateView
        {
            get
            {
                var tasksView = new TasksView("Background Tasks");
                tasksView.Show(_appForm.Panel, DockState.DockBottom);
                return tasksView;
            }
        }
    }
}
