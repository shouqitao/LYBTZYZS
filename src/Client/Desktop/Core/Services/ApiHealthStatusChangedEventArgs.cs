using System;

namespace LYBT.Desktop.Core.Services
{
    /// <summary>
    /// Event args for API health status changes.
    /// </summary>
    public class ApiHealthStatusChangedEventArgs : EventArgs
    {
        public bool IsOnline { get; init; }
        public string StatusMessage { get; init; } = string.Empty;
        public DateTime CheckTime { get; init; } = DateTime.UtcNow;
        public int ConsecutiveFailures { get; init; }
    }
}

