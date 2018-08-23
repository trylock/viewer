using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Core;
using Viewer.Data.Storage;

namespace Viewer.Properties
{
    internal sealed class Settings : ApplicationSettingsBase, IStorageConfiguration
    {
        [Export(typeof(IStorageConfiguration))]
        public static Settings Default { get; } = (Settings)Synchronized(new Settings());

        [UserScopedSetting]
        [DefaultSettingValue("%userprofile%/Documents/Viewer/Views")]
        public string QueryViewDirectoryPath
        {
            get => (string) this["QueryViewDirectoryPath"];
            set => this["QueryViewDirectoryPath"] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue(@"<?xml version=""1.0"" encoding=""utf-16""?>
<ArrayOfExternalApplication xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <ExternalApplication>
    <Name>Open in Explorer</Name>
    <Command>explorer.exe</Command>
    <Arguments>/select,""{0}""</Arguments>
  </ExternalApplication>
</ArrayOfExternalApplication>")]
        public ExternalApplication[] ExternalApplications
        {
            get => (ExternalApplication[]) this["ExternalApplications"];
            set => this["ExternalApplications"] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("1.00:00:00")]
        public TimeSpan CacheLifespan
        {
            get => (TimeSpan) this["CacheLifespan"];
            set => this["CacheLifespan"] = value;
        }

        [UserScopedSetting()]
        [DefaultSettingValue("2147483647")]
        public int CacheMaxFileCount
        {
            get => (int) this["CacheMaxFileCount"];
            set => this["CacheMaxFileCount"] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("%userprofile%/Documents/Viewer/Viewer.log")]
        public string LogFilePath
        {
            get => (string) this["LogFilePath"];
            set => this["LogFilePath"] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue(@"1200, 600")]
        public Size FormSize
        {
            get => (Size) this["FormSize"];
            set => this["FormSize"] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("false")]
        public bool FormIsMaximized
        {
            get => (bool) this["FormIsMaximized"];
            set => this["FormIsMaximized"] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("0.0")]
        public double ThumbnailSize
        {
            get => (double) this["ThumbnailSize"];
            set => this["ThumbnailSize"] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue(@"<?xml version=""1.0"" encoding=""utf-8""?>
<!--DockPanel configuration file. Author: Weifen Luo, all rights reserved.-->
<!--!!! AUTOMATICALLY GENERATED FILE. DO NOT MODIFY !!!-->
<DockPanel FormatVersion=""1.0"" DockLeftPortion=""0.201849134188416"" DockRightPortion=""0.203116659575951"" DockTopPortion=""0.25"" DockBottomPortion=""0.15016935998746"" ActiveDocumentPane=""4"" ActivePane=""4"">
  <Contents Count=""4"">
    <Content ID=""0"" PersistString=""Viewer.UI.Explorer.DirectoryTreeView"" AutoHidePortion=""0.13202958099576"" IsHidden=""False"" IsFloat=""False"" />
    <Content ID=""1"" PersistString=""Viewer.UI.Attributes.AttributeTableView;exif"" AutoHidePortion=""0.25"" IsHidden=""False"" IsFloat=""False"" />
    <Content ID=""2"" PersistString=""Viewer.UI.Attributes.AttributeTableView;attributes"" AutoHidePortion=""0.25"" IsHidden=""False"" IsFloat=""False"" />
    <Content ID=""3"" PersistString=""Viewer.UI.QueryEditor.QueryEditorView;;"" AutoHidePortion=""0.25"" IsHidden=""False"" IsFloat=""False"" />
  </Contents>
  <Panes Count=""5"">
    <Pane ID=""0"" DockState=""DockRight"" ActiveContent=""2"">
      <Contents Count=""1"">
        <Content ID=""0"" RefID=""2"" />
      </Contents>
    </Pane>
    <Pane ID=""1"" DockState=""DockRight"" ActiveContent=""1"">
      <Contents Count=""1"">
        <Content ID=""0"" RefID=""1"" />
      </Contents>
    </Pane>
    <Pane ID=""2"" DockState=""Float"" ActiveContent=""-1"">
      <Contents Count=""1"">
        <Content ID=""0"" RefID=""0"" />
      </Contents>
    </Pane>
    <Pane ID=""3"" DockState=""DockLeft"" ActiveContent=""0"">
      <Contents Count=""1"">
        <Content ID=""0"" RefID=""0"" />
      </Contents>
    </Pane>
    <Pane ID=""4"" DockState=""Document"" ActiveContent=""3"">
      <Contents Count=""1"">
        <Content ID=""0"" RefID=""3"" />
      </Contents>
    </Pane>
  </Panes>
  <DockWindows>
    <DockWindow ID=""0"" DockState=""Document"" ZOrderIndex=""0"">
      <NestedPanes Count=""1"">
        <Pane ID=""0"" RefID=""4"" PrevPane=""-1"" Alignment=""Right"" Proportion=""0.5"" />
      </NestedPanes>
    </DockWindow>
    <DockWindow ID=""1"" DockState=""DockLeft"" ZOrderIndex=""2"">
      <NestedPanes Count=""1"">
        <Pane ID=""0"" RefID=""3"" PrevPane=""-1"" Alignment=""Bottom"" Proportion=""0.5"" />
      </NestedPanes>
    </DockWindow>
    <DockWindow ID=""2"" DockState=""DockRight"" ZOrderIndex=""1"">
      <NestedPanes Count=""2"">
        <Pane ID=""0"" RefID=""0"" PrevPane=""-1"" Alignment=""Bottom"" Proportion=""0.5"" />
        <Pane ID=""1"" RefID=""1"" PrevPane=""0"" Alignment=""Top"" Proportion=""0.433528205201842"" />
      </NestedPanes>
    </DockWindow>
    <DockWindow ID=""3"" DockState=""DockTop"" ZOrderIndex=""4"">
      <NestedPanes Count=""0"" />
    </DockWindow>
    <DockWindow ID=""4"" DockState=""DockBottom"" ZOrderIndex=""3"">
      <NestedPanes Count=""0"" />
    </DockWindow>
  </DockWindows>
  <FloatWindows Count=""1"">
    <FloatWindow ID=""0"" Bounds=""2244, 1079, 300, 300"" ZOrderIndex=""0"">
      <NestedPanes Count=""1"">
        <Pane ID=""0"" RefID=""2"" PrevPane=""-1"" Alignment=""Right"" Proportion=""0.5"" />
      </NestedPanes>
    </FloatWindow>
  </FloatWindows>
</DockPanel>")]
        public string Layout
        {
            get => (string) this["Layout"];
            set => this["Layout"] = value;
        }
    }
}
