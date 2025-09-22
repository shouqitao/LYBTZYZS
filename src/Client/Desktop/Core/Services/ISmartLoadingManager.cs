using System;
using System.ComponentModel;
using System.Threading;

namespace LYBT.Desktop.Core.Services
{
    /// <summary>
    /// Contract for tracking and exposing smart loading operations across the app.
    /// </summary>
    public interface ISmartLoadingManager : INotifyPropertyChanged
    {
        /// <summary>
        /// True when there is at least one active loading operation.
        /// </summary>
        bool IsGlobalLoading { get; }

        /// <summary>
        /// Total active loading operations.
        /// </summary>
        int ActiveLoadingCount { get; }

        /// <summary>
        /// Starts a loading operation tracked by the manager.
        /// </summary>
        /// <param name="operationId">A caller-provided identifier for diagnostics.</param>
        /// <param name="message">User-facing message for the operation.</param>
        /// <param name="layer">Layer to display the indicator on (1 = default).</param>
        /// <param name="supportsProgress">Whether the operation reports progress.</param>
        /// <param name="cancellationToken">Optional external cancellation token.</param>
        /// <returns>An operation handle to update progress and complete.</returns>
        ILoadingOperation StartLoading(string operationId, string message, int layer, bool supportsProgress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Whether there is active loading on a specific layer.
        /// </summary>
        bool IsLoadingAtLayer(int layer);

        /// <summary>
        /// Gets current message for a specific layer, if any.
        /// </summary>
        string? GetCurrentLoadingMessage(int layer);

        /// <summary>
        /// Cancels all active operations (best effort).
        /// </summary>
        void CancelAllOperations();
    }

    /// <summary>
    /// A handle representing an in-progress loading operation.
    /// </summary>
    public interface ILoadingOperation : IDisposable
    {
        string OperationId { get; }
        int Layer { get; }
        bool SupportsProgress { get; }
        int Progress { get; }
        string? Message { get; }
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Updates progress and optionally the user-facing message.
        /// </summary>
        void UpdateProgress(int progress, string? message = null);

        /// <summary>
        /// Marks the operation as completed and removes it from the manager.
        /// </summary>
        void Complete();
    }
}

