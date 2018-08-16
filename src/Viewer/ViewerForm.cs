﻿using System;
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

namespace Viewer
{
    [Export]
    public partial class ViewerForm : Form
    {
        public DockPanel Panel { get; }

        public EventHandler Shutdown;

        private MenuStrip _viewerMenu;
        private StatusStrip _statusBar;
        private ToolStrip _toolBar;

        public ViewerForm()
        {
            Panel = new DockPanel
            {
                Theme = new VS2015LightTheme()
            };
            Panel.UpdateDockWindowZOrder(DockStyle.Right, true);
            Panel.UpdateDockWindowZOrder(DockStyle.Left, true);
            Controls.Add(Panel);
            
            InitializeComponent();
            InitializeToolBar();
            InitializeMenu();
            InitializeStatusbar();

            Panel.Location = new Point(0, _viewerMenu.Height + _toolBar.Height);
            Panel.Size = new Size(
                ClientSize.Width, 
                ClientSize.Height - _viewerMenu.Height - _statusBar.Height - _toolBar.Height);
            Panel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
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

        private void InitializeToolBar()
        {
            _toolBar = new ToolStrip();
            Panel.Theme.ApplyTo(_toolBar);
            Controls.Add(_toolBar);
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

            // all items afther this will be pushed to the right
            _statusBar.Items.Add(new ToolStripStatusLabel()
            {
                Spring = true,
                Tag = "separator"
            });

            Panel.Theme.ApplyTo(_statusBar);
            Controls.Add(_statusBar);
        }

        #region ToolBar API
        
        /// <summary>
        /// ToolStripButton wrapper used as a return value from <see cref="ViewerForm.AddToolBarButton"/>
        /// </summary>
        private class ToolStripButtonAdapter : IToolBarItem
        {
            private readonly ToolStripButton _button;

            public Image Image
            {
                get => _button.Image;
                set => _button.Image = value;
            }

            public string ToolTipText
            {
                get => _button.ToolTipText;
                set => _button.ToolTipText = value;
            }

            public bool Enabled
            {
                get => _button.Enabled;
                set => _button.Enabled = value;
            }

            public ToolStripButtonAdapter(ToolStripButton button)
            {
                _button = button;
            }
        }

        private class ToolStripDropDownAdapter : IToolBarDropDown
        {
            private readonly ToolStripDropDownButton _dropDown;

            public event EventHandler<SelectedEventArgs> ItemSelected;

            public Image Image
            {
                get => _dropDown.Image;
                set => _dropDown.Image = value;
            }

            public string ToolTipText
            {
                get => _dropDown.ToolTipText;
                set => _dropDown.ToolTipText = value;
            }

            public bool Enabled
            {
                get => _dropDown.Enabled;
                set => _dropDown.Enabled = value;
            }

            public ICollection<string> Items
            {
                get
                {
                    var items = new List<string>();
                    foreach (ToolStripItem item in _dropDown.DropDownItems)
                    {
                        items.Add(item.Text);
                    }

                    return items;
                }
                set
                {
                    if (value == null)
                        throw new ArgumentNullException(nameof(value));
                    
                    _dropDown.DropDownItems.Clear();
                    foreach (var item in value)
                    {
                        var menuItem = new ToolStripMenuItem(item);
                        menuItem.Click += (sender, args) => ItemSelected?.Invoke(sender, new SelectedEventArgs(menuItem.Text));
                        _dropDown.DropDownItems.Add(menuItem);
                    }
                }
            }

            public ToolStripDropDownAdapter(ToolStripDropDownButton dropDown)
            {
                _dropDown = dropDown ?? throw new ArgumentNullException(nameof(dropDown));
            }
        }

        private void AddItemToToolBar(string groupName, string toolName, ToolStripItem newItem)
        {
            if (groupName == null)
                throw new ArgumentNullException(nameof(groupName));
            if (toolName == null)
                throw new ArgumentNullException(nameof(toolName));

            groupName = groupName.ToLowerInvariant();
            toolName = toolName.ToLowerInvariant();

            newItem.Tag = groupName;
            newItem.Name = toolName;

            // find the last item in the group
            var index = 0;
            var lastGroupIndex = -1;
            foreach (ToolStripItem item in _toolBar.Items)
            {
                if ((string)item.Tag == groupName)
                {
                    lastGroupIndex = index + 1;
                    if (item.Name == toolName)
                    {
                        throw new ArgumentException($"Tool name '{toolName}' is not unique in '{groupName}'");
                    }
                }

                ++index;
            }

            // if the group does not exist, create it
            if (lastGroupIndex < 0)
            {
                if (_toolBar.Items.Count > 0)
                {
                    _toolBar.Items.Add(new ToolStripSeparator());
                }

                lastGroupIndex = _toolBar.Items.Count;
            }
            _toolBar.Items.Insert(lastGroupIndex, newItem);
        }
        
        public IToolBarItem AddToolBarButton(string groupName, string toolName, Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            // add the tool
            var button = new ToolStripButton();
            button.Click += (sender, e) => action();
            AddItemToToolBar(groupName, toolName, button);

            return new ToolStripButtonAdapter(button);
        }

        public IToolBarDropDown AddToolBarSelect(string groupName, string toolName, string toolTipText, Image image)
        {
            var dropDown = new ToolStripDropDownButton()
            {
                ToolTipText = toolTipText,
                Image = image
            };
            AddItemToToolBar(groupName, toolName, dropDown);

            return new ToolStripDropDownAdapter(dropDown);
        }

        #endregion
        
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
