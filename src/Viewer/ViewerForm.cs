using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Core;
using Viewer.Core.UI;
using Viewer.Properties;
using Viewer.UI.Forms;
using WeifenLuo.WinFormsUI.Docking;
using WeifenLuo.WinFormsUI.ThemeVS2013;

namespace Viewer
{
    [Export]
    public partial class ViewerForm : Form
    {
        /// <summary>
        /// Application theme
        /// </summary>
        public static readonly ThemeBase Theme;

        public DockPanel Panel { get; }

        public EventHandler Shutdown;

        private MenuStrip _viewerMenu;
        private StatusStrip _statusBar;

        #region High DPI DockPanelSuite splitter fix 

        private class SplitterControl : VS2013WindowSplitterControl
        {
            public SplitterControl(ISplitterHost host) : base(host)
            {
            }

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);

                // The splitter is coverring other windows on high DPI settings (anything above
                // 100 %). 
                if (Dock == DockStyle.Right || Dock == DockStyle.Left)
                    Width = SplitterSize;
                else if (Dock == DockStyle.Bottom || Dock == DockStyle.Top)
                    Height = SplitterSize;
            }
        }

        private class DecoratedSplitterControlFactory : DockPanelExtender.IWindowSplitterControlFactory
        {
            public SplitterBase CreateSplitterControl(ISplitterHost host)
            {
                return new SplitterControl(host);
            }
        }

        static ViewerForm()
        {
            Theme = new VS2015LightTheme();
            Theme.Extender.WindowSplitterControlFactory = new DecoratedSplitterControlFactory();
        }

        #endregion

        public ViewerForm()
        {
            Panel = new DockPanel{ Theme = Theme };
            Panel.UpdateDockWindowZOrder(DockStyle.Right, true);
            Panel.UpdateDockWindowZOrder(DockStyle.Left, true);
            Controls.Add(Panel);
            
            InitializeComponent();
            InitializeMenu();
            InitializeStatusbar();

            Panel.Dock = DockStyle.Fill;
        }
        
        public void AddMenuItem(IReadOnlyList<string> path, Action action, Image icon)
        {
            var items = _viewerMenu.Items;
            foreach (var name in path)
            {
                // find menu item with this name
                var menuItem = items
                    .OfType<ToolStripMenuItem>()
                    .FirstOrDefault(item => StringComparer.CurrentCultureIgnoreCase.Compare(item.Text, name) == 0);

                // if it does not exist, create it
                if (menuItem == null)
                {
                    menuItem = new ToolStripMenuItem(name);
                    if (name == path[path.Count - 1])
                    {
                        menuItem.Image = icon;
                        menuItem.Click += (sender, e) => action();
                    }
                    items.Add(menuItem);
                }

                items = menuItem.DropDownItems;
            }
        }

        private void ViewerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Shutdown?.Invoke(sender, e);
        }

        private void InitializeMenu()
        {
            _viewerMenu = new MenuStrip();
            _viewerMenu.Items.Add(new ToolStripMenuItem("File"));
            _viewerMenu.Items.Add(new ToolStripMenuItem("View"));
            _viewerMenu.Items.Add(new ToolStripMenuItem("Help"));
            Panel.Theme.ApplyTo(_viewerMenu);
            Controls.Add(_viewerMenu);
        }

        private void InitializeStatusbar()
        {
            _statusBar = new StatusStrip();
            var defaultFont = _statusBar.Font;
            _statusBar.Font = new Font(defaultFont.FontFamily, 9f, defaultFont.Style, defaultFont.Unit, defaultFont.GdiCharSet, defaultFont.GdiVerticalFont);
            Panel.Theme.ApplyTo(_statusBar);

            // all items afther this will be pushed to the right
            _statusBar.Items.Add(new ToolStripStatusLabel()
            {
                Spring = true,
                Tag = "separator"
            });

            Controls.Add(_statusBar);
        }
        
        #region StatusBar API

        private class ToolStripSliderAdapter : IStatusBarSlider
        {
            private readonly SliderControl _slider;
            private readonly ToolStripStatusLabel _labelItem;
            private readonly ToolStripControlHost _sliderItem;
            private bool _isDisposed;

            public string Text
            {
                get => _labelItem.Text;
                set => _labelItem.Text = value;
            }

            public Image Image
            {
                get => _labelItem.Image;
                set => _labelItem.Image = value;
            }

            public ToolStripItemAlignment Alignment => _sliderItem.Alignment;

            public event EventHandler ValueChanged
            {
                add => _slider.ValueChanged += value;
                remove => _slider.ValueChanged -= value;
            }

            public double Value
            {
                get => (_slider.Value - _slider.MinimalValue) / (double) (_slider.MaximalValue - _slider.MinimalValue);
                set => _slider.Value = (int) MathUtils.Lerp(_slider.MinimalValue, _slider.MaximalValue, value);
            }

            public ToolStripSliderAdapter(SliderControl slider, ToolStripStatusLabel labelItem, ToolStripControlHost sliderItem)
            {
                _slider = slider;
                _labelItem = labelItem;
                _sliderItem = sliderItem;
            }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    _isDisposed = true;

                    _labelItem.GetCurrentParent()?.Items.Remove(_labelItem);
                    _sliderItem.GetCurrentParent()?.Items.Remove(_sliderItem);

                    _slider.Dispose();
                    _labelItem.Dispose();
                    _sliderItem.Dispose();
                }
            }
        }

        private int IndexOfStatusBarSeparator()
        {
            int separatorIndex = -1;
            int index = 0;
            foreach (ToolStripItem item in _statusBar.Items)
            {
                if ((string)item.Tag == "separator")
                {
                    separatorIndex = index;
                }

                ++index;
            }

            if (separatorIndex < 0)
            {
                throw new InvalidOperationException("ToolBar is in invalid state. It's missing a separator item.");
            }

            return separatorIndex;
        }

        public IStatusBarSlider CreateStatusBarSlider(string text, Image image, ToolStripItemAlignment alignment)
        {
            // create slider
            var slider = new SliderControl();
            var sliderItem = new ToolStripControlHost(slider)
            {
                AutoSize = false,
                Size = new Size(100, _statusBar.Height),
                Alignment = alignment
            };
            var labelItem = new ToolStripStatusLabel()
            {
                Image = image,
                Text = text,
                Alignment = alignment
            };

            // add all components to the status bar
            var separatorIndex = _statusBar.Items.Count;
            if (alignment == ToolStripItemAlignment.Left)
            {
                separatorIndex = IndexOfStatusBarSeparator();
            }
            _statusBar.Items.Insert(separatorIndex, labelItem);
            _statusBar.Items.Insert(separatorIndex + 1, sliderItem);
            return new ToolStripSliderAdapter(slider, labelItem, sliderItem);
        }

        private class ToolStripLabelAdapter : IStatusBarItem
        {
            private readonly ToolStripStatusLabel _label;
            private bool _isDisposed;

            public string Text
            {
                get => _label.Text;
                set => _label.Text = value;
            }

            public Image Image
            {
                get => _label.Image;
                set => _label.Image = value;
            }

            public ToolStripItemAlignment Alignment => _label.Alignment;

            public ToolStripLabelAdapter(ToolStripStatusLabel label)
            {
                _label = label;
            }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    _isDisposed = true;
                    _label.GetCurrentParent()?.Items.Remove(_label);
                    _label.Dispose();
                }
            }
        }

        public IStatusBarItem CreateStatusBarItem(string text, Image image, ToolStripItemAlignment alignment)
        {
            var label = new ToolStripStatusLabel()
            {
                Text = text,
                Image = image,
                Alignment = alignment
            };

            var index = _statusBar.Items.Count;
            if (alignment == ToolStripItemAlignment.Left)
            {
                index = IndexOfStatusBarSeparator();
            }
            _statusBar.Items.Insert(index, label);
            return new ToolStripLabelAdapter(label);
        }

        #endregion 
    }
}
