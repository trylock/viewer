using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Core;
using Viewer.Data.Storage;

namespace Viewer.Properties
{
    [Export(typeof(IStorageConfiguration))]
    internal sealed class Settings : ApplicationSettingsBase, IStorageConfiguration
    {
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
        [DefaultSettingValue("03:00:00")]
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
    }
}
