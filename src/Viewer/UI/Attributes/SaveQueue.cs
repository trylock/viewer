using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Data;
using Viewer.Properties;
using Viewer.UI.Errors;
using Viewer.UI.Tasks;

namespace Viewer.UI.Attributes
{
    public interface ISaveQueue
    {
        /// <summary>
        /// Save <paramref name="entities"/> to their file. If there is a save job running already,
        /// saving <paramref name="entities"/> will be queued after it.
        /// </summary>
        /// <remarks>
        /// This class is **not** thread safe and it should be called from the UI thread.
        /// </remarks>
        /// <param name="entities">Entities to save</param>
        /// <returns>
        /// Task finished once all <paramref name="entities"/> have been saved to their file or the
        /// save task has been canceled. If it has been canceled, the task throws the
        /// <see cref="OperationCanceledException"/>.
        /// </returns>
        Task SaveAsync(IReadOnlyList<IModifiedEntity> entities);

        /// <summary>
        /// Cancel all pending save operations. <see cref="SaveAsync"/> calls after the call to
        /// this method will not be canceled.
        /// </summary>
        void Cancel();
    }

    internal class SaveState : IDisposable
    {
        /// <summary>
        /// Cancellation token source used to cancel the save operation
        /// </summary>
        public CancellationTokenSource Cancellation { get; } = new CancellationTokenSource();

        /// <summary>
        /// Current progress controller
        /// </summary>
        public IProgressController ProgressController { get; }

        public SaveState(ITaskLoader taskLoader)
        {
            ProgressController = taskLoader.CreateLoader(Resources.SavingChanges_Label, Cancellation);
        }

        public void Dispose()
        {
            Cancellation.Cancel();
            ProgressController?.Close();
            Cancellation?.Dispose();
        }
    }

    /// <summary>
    /// Save job takes entities from the saving list of <see cref="IEntityManager"/> and saves them
    /// to their file.
    /// </summary>
    [Export(typeof(ISaveQueue))]
    internal class SaveQueue : ISaveQueue
    {
        private readonly ITaskLoader _taskLoader;

        private int _saveTaskId = 0;
        private Task _lastSaveTask = Task.CompletedTask;
        private SaveState _state;

        [ImportingConstructor]
        public SaveQueue(ITaskLoader taskLoader)
        {
            _taskLoader = taskLoader;
        }

        public Task SaveAsync(IReadOnlyList<IModifiedEntity> entities)
        {
            if (entities.Count <= 0)
            {
                return Task.CompletedTask;
            }

            var id = ++_saveTaskId;
            if (_state == null)
            {
                _state = new SaveState(_taskLoader);
            }

            _state.ProgressController.TotalTaskCount += entities.Count;
            
            // save all entities on background
            _lastSaveTask = _lastSaveTask.ContinueWith((_, state) =>
            {
                var localEntities = (IReadOnlyList<IModifiedEntity>) state;
                foreach (var entity in localEntities)
                {
                    if (_state.Cancellation.IsCancellationRequested)
                    {
                        entity.Return();
                        continue;
                    }

                    _state.ProgressController.Report(new LoadingProgress(entity.Path));
                    try
                    {
                        entity.Save();
                    }
                    catch (FileNotFoundException)
                    {
                        entity.Return();
                    }
                    catch (IOException)
                    {
                        entity.Return();
                    }
                    catch (UnauthorizedAccessException)
                    {
                        entity.Return();
                    }
                    catch (SecurityException)
                    {
                        entity.Return();
                    }
                }
            }, entities, CancellationToken.None, TaskContinuationOptions.LongRunning, TaskScheduler.Default);

            // check if we should dispose current state (i.e., this has been the last save task)
            _lastSaveTask = _lastSaveTask.ContinueWith(_ =>
            {
                if (id == _saveTaskId) // this has been the last save operation
                {
                    _state.Dispose();
                    _state = null;
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());

            return _lastSaveTask;
        }

        public void Cancel()
        {
            _state?.Cancellation.Cancel();
        }
    }
}
