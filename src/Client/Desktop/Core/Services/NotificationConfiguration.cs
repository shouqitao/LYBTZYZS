using System;

namespace LYBT.Desktop.Core.Services
{
    /// <summary>
    /// Configuration for displaying notifications.
    /// </summary>
    public class NotificationConfiguration
    {
        /// <summary>
        /// How long the notification stays visible.
        /// </summary>
        public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// For critical errors, whether to show a dialog instead of a toast.
        /// </summary>
        public bool ShowInDialog { get; set; } = false;
    }
}

