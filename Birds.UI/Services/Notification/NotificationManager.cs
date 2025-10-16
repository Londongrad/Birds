using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Birds.UI.Services.Notification
{
    using global::Birds.UI.Views.Windows;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;

    namespace Birds.UI.Services.Notification
    {
        /// <summary>
        /// Provides a centralized mechanism for displaying and managing multiple notification windows
        /// within the WPF user interface.
        /// </summary>
        /// <remarks>
        /// The <see cref="NotificationManager"/> handles the visual stacking and positioning of
        /// <see cref="NotificationWindow"/> instances on the screen.  
        /// Each new notification window appears below the previous one, maintaining a consistent margin.
        /// When a notification closes, the remaining ones are automatically repositioned.
        /// </remarks>
        /// <example>
        /// Example usage:
        /// <code>
        /// NotificationManager.ShowNotification(
        ///     "Operation completed successfully",
        ///     new NotificationOptions(NotificationType.Success));
        /// </code>
        /// </example>
        /// <seealso cref="NotificationWindow"/>
        /// <seealso cref="NotificationOptions"/>
        /// <seealso cref="NotificationType"/>
        public static class NotificationManager
        {
            private static readonly List<NotificationWindow> _activeNotifications = new();

            /// <summary>
            /// Displays a new notification window and arranges existing ones in a stacked layout.
            /// </summary>
            /// <param name="message">The text message to display in the notification window.</param>
            /// <param name="options">Notification appearance and behavior options.</param>
            /// <remarks>
            /// Notifications are displayed starting from the top-right corner of the user's
            /// working area (excluding the taskbar).  
            /// Each subsequent notification appears below the previous one with a small margin.
            /// </remarks>
            public static void ShowNotification(string message, NotificationOptions options, Window parent)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var window = new NotificationWindow(message, options)
                    {
                        Owner = parent,
                        WindowStartupLocation = WindowStartupLocation.Manual
                    };

                    _activeNotifications.Add(window);

                    // Once the window is loaded, its actual size is known — now we can position it correctly
                    window.Loaded += (_, _) => Rearrange(parent);

                    // Remove and reposition when closed
                    window.Closed += (_, _) =>
                    {
                        _activeNotifications.Remove(window);
                        Rearrange(parent);
                    };

                    window.Show();
                });
            }

            /// <summary>
            /// Rearranges all currently visible notifications to maintain consistent spacing
            /// after one or more have been closed.
            /// </summary>
            private static void Rearrange(Window parent)
            {
                const int margin = 10;
                double topOffset = parent.Top + 30; // distance from the window title bar

                foreach (var notif in _activeNotifications)
                {
                    notif.Left = parent.Left + parent.Width - notif.Width - margin;
                    notif.Top = topOffset;
                    topOffset += notif.Height + margin;
                }
            }
        }
    }
}