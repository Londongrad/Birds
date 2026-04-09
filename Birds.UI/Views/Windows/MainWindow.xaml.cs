using Birds.UI.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Birds.UI.Views.Windows
{
    public partial class MainWindow : Window
    {
        private const double DefaultMinWindowWidth = 1080;
        private const double NotificationCenterMinWindowWidth = 1280;
        private INotifyPropertyChanged? _viewModelNotifier;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += (_, _) =>
            {
                UpdateWindowStateGlyph();
                UpdateQuickOperationIndicatorState(animate: false);
                UpdateAdaptiveMinWidth();
            };
            StateChanged += (_, _) => UpdateWindowStateGlyph();
            DataContextChanged += OnDataContextChanged;
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
            if (MaximizeGlyph == null || RestoreGlyph == null)
            {
                return;
            }

            var isMaximized = WindowState == WindowState.Maximized;
            MaximizeGlyph.Visibility = isMaximized
                ? Visibility.Collapsed
                : Visibility.Visible;
            RestoreGlyph.Visibility = isMaximized
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModelNotifier is not null)
                _viewModelNotifier.PropertyChanged -= OnViewModelPropertyChanged;

            _viewModelNotifier = e.NewValue as INotifyPropertyChanged;

            if (_viewModelNotifier is not null)
                _viewModelNotifier.PropertyChanged += OnViewModelPropertyChanged;

            UpdateQuickOperationIndicatorState(animate: false);
            UpdateAdaptiveMinWidth();
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(MainViewModel.HasRecentOperationStatus)
                or nameof(MainViewModel.IsRecentOperationSuccess)
                or nameof(MainViewModel.IsRecentOperationError))
            {
                UpdateQuickOperationIndicatorState(animate: true);
            }

            if (e.PropertyName == nameof(MainViewModel.IsNotificationCenterOpen))
            {
                UpdateAdaptiveMinWidth();
            }
        }

        private void UpdateQuickOperationIndicatorState(bool animate)
        {
            if (QuickOperationIndicator is null || DataContext is not MainViewModel viewModel)
                return;

            if (viewModel.HasRecentOperationStatus)
            {
                QuickOperationIndicator.Visibility = Visibility.Visible;

                if (animate)
                    AnimateQuickOperationIndicatorIn();
                else
                    ApplyQuickIndicatorVisibleState();

                return;
            }

            if (animate && QuickOperationIndicator.Visibility == Visibility.Visible)
            {
                AnimateQuickOperationIndicatorOut();
            }
            else
            {
                QuickOperationIndicator.Visibility = Visibility.Collapsed;
                ApplyQuickIndicatorHiddenState();
            }
        }

        private void AnimateQuickOperationIndicatorIn()
        {
            StopQuickIndicatorAnimations();

            ApplyQuickIndicatorHiddenState();
            QuickOperationIndicator.Visibility = Visibility.Visible;

            var easeOut = new BackEase
            {
                EasingMode = EasingMode.EaseOut,
                Amplitude = 0.42
            };

            QuickOperationIndicator.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(220))
            {
                EasingFunction = easeOut
            });

            if (TryGetQuickIndicatorTransforms(out var scale, out var translate))
            {
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(260))
                {
                    From = 0.45,
                    EasingFunction = easeOut
                });
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(260))
                {
                    From = 0.45,
                    EasingFunction = easeOut
                });
                translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(240))
                {
                    From = -7,
                    EasingFunction = easeOut
                });
            }

            AnimateQuickOperationPulse();
        }

        private void AnimateQuickOperationIndicatorOut()
        {
            StopQuickIndicatorAnimations();

            var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(220));
            fade.Completed += (_, _) =>
            {
                if (DataContext is MainViewModel viewModel && viewModel.HasRecentOperationStatus)
                    return;

                QuickOperationIndicator.Visibility = Visibility.Collapsed;
                ApplyQuickIndicatorHiddenState();
            };

            QuickOperationIndicator.BeginAnimation(OpacityProperty, fade);

            if (TryGetQuickIndicatorTransforms(out var scale, out var translate))
            {
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.82, TimeSpan.FromMilliseconds(220)));
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.82, TimeSpan.FromMilliseconds(220)));
                translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(-2, TimeSpan.FromMilliseconds(220)));
            }
        }

        private void StopQuickIndicatorAnimations()
        {
            QuickOperationIndicator.BeginAnimation(OpacityProperty, null);
            QuickOperationPulse?.BeginAnimation(OpacityProperty, null);

            if (TryGetQuickIndicatorTransforms(out var scale, out var translate))
            {
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                translate.BeginAnimation(TranslateTransform.YProperty, null);
            }

            if (QuickOperationPulse?.RenderTransform is ScaleTransform pulseScale)
            {
                pulseScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                pulseScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            }
        }

        private void ApplyQuickIndicatorVisibleState()
        {
            QuickOperationIndicator.Opacity = 1;

            if (TryGetQuickIndicatorTransforms(out var scale, out var translate))
            {
                scale.ScaleX = 1;
                scale.ScaleY = 1;
                translate.Y = 0;
            }
        }

        private void ApplyQuickIndicatorHiddenState()
        {
            QuickOperationIndicator.Opacity = 0;
            if (QuickOperationPulse is not null)
                QuickOperationPulse.Opacity = 0;

            if (TryGetQuickIndicatorTransforms(out var scale, out var translate))
            {
                scale.ScaleX = 0.72;
                scale.ScaleY = 0.72;
                translate.Y = -3;
            }

            if (QuickOperationPulse?.RenderTransform is ScaleTransform pulseScale)
            {
                pulseScale.ScaleX = 0.8;
                pulseScale.ScaleY = 0.8;
            }
        }

        private void AnimateQuickOperationPulse()
        {
            if (QuickOperationPulse?.RenderTransform is not ScaleTransform pulseScale)
                return;

            QuickOperationPulse.Opacity = 0.75;

            QuickOperationPulse.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(540))
            {
                BeginTime = TimeSpan.FromMilliseconds(40),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            });

            pulseScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1.85, TimeSpan.FromMilliseconds(540))
            {
                From = 0.8,
                BeginTime = TimeSpan.FromMilliseconds(40),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            });

            pulseScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1.85, TimeSpan.FromMilliseconds(540))
            {
                From = 0.8,
                BeginTime = TimeSpan.FromMilliseconds(40),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            });
        }

        private bool TryGetQuickIndicatorTransforms(out ScaleTransform scale, out TranslateTransform translate)
        {
            scale = null!;
            translate = null!;

            if (QuickOperationIndicator.RenderTransform is not TransformGroup group || group.Children.Count < 2)
                return false;

            if (group.Children[0] is not ScaleTransform scaleTransform || group.Children[1] is not TranslateTransform translateTransform)
                return false;

            scale = scaleTransform;
            translate = translateTransform;
            return true;
        }

        private void UpdateAdaptiveMinWidth()
        {
            if (DataContext is not MainViewModel viewModel)
            {
                MinWidth = DefaultMinWindowWidth;
                return;
            }

            var targetMinWidth = viewModel.IsNotificationCenterOpen
                ? NotificationCenterMinWindowWidth
                : DefaultMinWindowWidth;

            MinWidth = targetMinWidth;

            if (WindowState == WindowState.Normal && Width < targetMinWidth)
            {
                Width = targetMinWidth;
            }
        }
    }
}
