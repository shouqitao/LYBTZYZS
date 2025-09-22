using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace LYBT.Desktop.Core.Services
{
    /// <summary>
    /// Basic in-memory implementation of ISmartLoadingManager.
    /// Thread-safe via a private lock on the operations list.
    /// </summary>
    public class SmartLoadingManager : ISmartLoadingManager
    {
        private readonly object _gate = new();
        private readonly List<LoadingOperation> _operations = new();

        private int _activeCount;
        private bool _isGlobalLoading;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsGlobalLoading
        {
            get => _isGlobalLoading;
            private set
            {
                if (_isGlobalLoading != value)
                {
                    _isGlobalLoading = value;
                    OnPropertyChanged(nameof(IsGlobalLoading));
                }
            }
        }

        public int ActiveLoadingCount
        {
            get => _activeCount;
            private set
            {
                if (_activeCount != value)
                {
                    _activeCount = value;
                    OnPropertyChanged(nameof(ActiveLoadingCount));
                    IsGlobalLoading = _activeCount > 0;
                }
            }
        }

        public ILoadingOperation StartLoading(string operationId, string message, int layer, bool supportsProgress, CancellationToken cancellationToken = default)
        {
            var op = new LoadingOperation(this, operationId, message, layer, supportsProgress, cancellationToken);

            lock (_gate)
            {
                _operations.Add(op);
                ActiveLoadingCount = _operations.Count(o => !o.IsCompleted);
            }

            return op;
        }

        public bool IsLoadingAtLayer(int layer)
        {
            lock (_gate)
            {
                return _operations.Any(o => !o.IsCompleted && o.Layer == layer);
            }
        }

        public string? GetCurrentLoadingMessage(int layer)
        {
            lock (_gate)
            {
                // Prefer the most recently started non-completed operation on the layer
                return _operations
                    .Where(o => !o.IsCompleted && o.Layer == layer)
                    .LastOrDefault()?.Message;
            }
        }

        public void CancelAllOperations()
        {
            List<LoadingOperation> snapshot;
            lock (_gate)
            {
                snapshot = _operations.Where(o => !o.IsCompleted).ToList();
            }

            foreach (var op in snapshot)
            {
                op.Cancel();
            }
        }

        private void Remove(LoadingOperation operation)
        {
            lock (_gate)
            {
                // Keep completed ops for a short time if needed; here we remove directly
                _operations.Remove(operation);
                ActiveLoadingCount = _operations.Count(o => !o.IsCompleted);
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private sealed class LoadingOperation : ILoadingOperation
        {
            private readonly SmartLoadingManager _owner;
            private readonly CancellationTokenSource _cts;

            public string OperationId { get; }
            public int Layer { get; }
            public bool SupportsProgress { get; }
            public int Progress { get; private set; }
            public string? Message { get; private set; }
            public bool IsCompleted { get; private set; }

            public CancellationToken CancellationToken => _cts.Token;

            public LoadingOperation(SmartLoadingManager owner, string operationId, string message, int layer, bool supportsProgress, CancellationToken cancellationToken)
            {
                _owner = owner;
                OperationId = operationId;
                Message = message;
                Layer = layer;
                SupportsProgress = supportsProgress;
                _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            }

            public void UpdateProgress(int progress, string? message = null)
            {
                if (IsCompleted) return;
                Progress = Math.Max(0, Math.Min(100, progress));
                if (!string.IsNullOrEmpty(message))
                {
                    Message = message;
                }
            }

            public void Complete()
            {
                if (IsCompleted) return;
                IsCompleted = true;
                _cts.Cancel(); // mark as finished for any waiters
                Dispose();
            }

            public void Cancel()
            {
                if (IsCompleted) return;
                _cts.Cancel();
                IsCompleted = true;
                Dispose();
            }

            public void Dispose()
            {
                _cts.Dispose();
                _owner.Remove(this);
            }
        }
    }
}
