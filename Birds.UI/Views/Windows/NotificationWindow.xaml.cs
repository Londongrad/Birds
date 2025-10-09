using Birds.UI.Services.Notification;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Birds.UI.Views.Windows
{
    public partial class NotificationWindow : Window
    {
        private readonly DispatcherTimer _timer;

        public NotificationWindow(string message, NotificationOptions options)
        {
            InitializeComponent();
            MessageText.Text = options.Title != null
                ? $"{options.Title}\n{message}"
                : message;

            // Настраиваем цвет и иконку
            switch (options.Type)
            {
                case NotificationType.Success:
                    RootBorder.Background = Brushes.Green;
                    IconImage.Source = (ImageSource)FindResource("SuccessIconSource");
                    break;
                case NotificationType.Error:
                    RootBorder.Background = Brushes.Red;
                    IconImage.Source = (ImageSource)FindResource("ErrorIconSource");
                    break;
                case NotificationType.Info:
                    RootBorder.Background = Brushes.Blue;
                    IconImage.Source = (ImageSource)FindResource("InfoIconSource");
                    break;
                case NotificationType.Warning:
                    RootBorder.Background = Brushes.Orange;
                    IconImage.Source = (ImageSource)FindResource("WarningIconSource");
                    break;
            }

            // Автозакрытие по таймеру
            _timer = new DispatcherTimer { Interval = options.Duration };
            _timer.Tick += (s, e) =>
            {
                _timer.Stop();
                var anim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
                anim.Completed += (_, _) => Close();
                this.BeginAnimation(OpacityProperty, anim);
            };

            this.Closed += (_, _) => _timer.Stop();

            _timer.Start();
        }

        private void OnClickClose(object sender, MouseButtonEventArgs e) => Close();
    }
}
