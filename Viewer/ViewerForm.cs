using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Data;
using Viewer.UI;

namespace Viewer
{
    public partial class ViewerForm : Form
    {
        private QueryResultPresenter _resultPresenter;
        private DirectoryTreePresenter _treePresenter;

        public ViewerForm()
        {
            InitializeComponent();

            var factory = new AttributeStorageFactory();
            var storage = factory.Create();

            _treePresenter = new DirectoryTreePresenter(directoryTreeControl1);
            _treePresenter.UpdateRootDirectories();

            _resultPresenter = new QueryResultPresenter(thumbnailGridControl1, storage);
            _resultPresenter.LoadDirectory("C:/tmp");
        }
    }
}
