using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.UI.Images
{
    public enum EntityViewState
    {
        None,
        Active,
        Selected,
    }

    public sealed class EntityView : IDisposable
    {
        public string Name
        {
            get
            {
                if (Data is FileEntity)
                {
                    return Path.GetFileNameWithoutExtension(Data.Path);
                }

                return Path.GetFileName(Data.Path);
            }
        }

        public string FullPath => Data.Path;
        public EntityViewState State { get; set; } = EntityViewState.None;
        public ILazyThumbnail Thumbnail { get; }
        public IEntity Data { get; }

        public EntityView(IEntity data, ILazyThumbnail thumbnail)
        {
            Data = data;
            Thumbnail = thumbnail;
        }

        public void Dispose()
        {
            Thumbnail?.Dispose();
        }
    }

    public class EntityViewPathComparer : IEqualityComparer<EntityView>
    {
        public bool Equals(EntityView x, EntityView y)
        {
            if (x == null && y == null)
                return true;
            if (x == null || y == null)
                return false;
            return x.Data.Path == y.Data.Path;
        }

        public int GetHashCode(EntityView obj)
        {
            return obj.FullPath.GetHashCode();
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// EntityView comparer which compares entity views by their underlying entity.
    /// </summary>
    public class EntityViewComparer : IComparer<EntityView>
    {
        private readonly IComparer<IEntity> _entityComparer;

        public EntityViewComparer(IComparer<IEntity> entityComparer)
        {
            _entityComparer = entityComparer ?? throw new ArgumentNullException(nameof(entityComparer));
        }

        public int Compare(EntityView x, EntityView y)
        {
            if (x == null && y == null)
            {
                return 0;
            }
            else if (x == null)
            {
                return -1;
            }
            else if (y == null)
            {
                return 1;
            }
            return _entityComparer.Compare(x.Data, y.Data);
        }
    }

}
