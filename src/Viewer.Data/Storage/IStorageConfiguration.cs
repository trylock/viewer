using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data.Storage
{
    /// <summary>
    /// Provides options used by the attribute storage.
    /// </summary>
    public interface IStorageConfiguration
    {
        /// <summary>
        /// Maximum age of an entry in the cache. Age of a cache entry here means the difference
        /// between the current time and the last access time of the entry.
        /// </summary>
        TimeSpan CacheLifespan { get; }

        /// <summary>
        /// Maximum number of files kept in the cache at a time.
        /// </summary>
        int CacheMaxFileCount { get; }
    }
}
