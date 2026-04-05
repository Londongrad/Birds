using System.Windows;
using System.Windows.Input;

namespace Birds.UI.Views.Windows
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += (_, _) => UpdateWindowStateGlyph();
            StateChanged += (_, _) => UpdateWindowStateGlyph();
        }

        private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && ResizeMode == ResizeMode.CanResize)
            {
                ToggleWindowState();
                return;
            }

            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void OnMinimizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnMaximizeRestoreClick(object sender, RoutedEventArgs e)
        {
            ToggleWindowState();
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ToggleWindowState()
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void UpdateWindowStateGlyph()
        {
            if (MaximizeRestoreGlyph == null)
            {
                return;
            }

            MaximizeRestoreGlyph.Text = WindowState == WindowState.Maximized
                ? "❐"
                : "□";
        }
    }
}
