using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Viewer.UI.Attributes
{
    internal partial class UnsavedAttributesMessageBox : Form
    {
        /// <summary>
        /// Result picked by the user
        /// </summary>
        public UnsavedDecision Result { get; private set; } = UnsavedDecision.Cancel;

        public UnsavedAttributesMessageBox()
        {
            InitializeComponent();

            IconPictureBox.Image = SystemIcons.Question.ToBitmap();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            var image = IconPictureBox.Image;
            IconPictureBox.Image = null;
            image?.Dispose();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            Result = UnsavedDecision.Save;
            Close();
        }

        private void RevertButton_Click(object sender, EventArgs e)
        {
            Result = UnsavedDecision.Revert;
            Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            Result = UnsavedDecision.Cancel;
            Close();
        }
    }
}
