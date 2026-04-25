using System.Windows;
using System.Windows.Controls;

namespace Birds.UI.Views.Helpers;

public static class PasswordBoxBinding
{
    public static readonly DependencyProperty BoundPasswordProperty =
        DependencyProperty.RegisterAttached(
            "BoundPassword",
            typeof(string),
            typeof(PasswordBoxBinding),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnBoundPasswordChanged));

    public static readonly DependencyProperty BindPasswordProperty =
        DependencyProperty.RegisterAttached(
            "BindPassword",
            typeof(bool),
            typeof(PasswordBoxBinding),
            new PropertyMetadata(false, OnBindPasswordChanged));

    private static readonly DependencyProperty UpdatingPasswordProperty =
        DependencyProperty.RegisterAttached(
            "UpdatingPassword",
            typeof(bool),
            typeof(PasswordBoxBinding),
            new PropertyMetadata(false));

    public static string GetBoundPassword(DependencyObject dependencyObject)
    {
        return (string)dependencyObject.GetValue(BoundPasswordProperty);
    }

    public static void SetBoundPassword(DependencyObject dependencyObject, string value)
    {
        dependencyObject.SetValue(BoundPasswordProperty, value);
    }

    public static bool GetBindPassword(DependencyObject dependencyObject)
    {
        return (bool)dependencyObject.GetValue(BindPasswordProperty);
    }

    public static void SetBindPassword(DependencyObject dependencyObject, bool value)
    {
        dependencyObject.SetValue(BindPasswordProperty, value);
    }

    private static bool GetUpdatingPassword(DependencyObject dependencyObject)
    {
        return (bool)dependencyObject.GetValue(UpdatingPasswordProperty);
    }

    private static void SetUpdatingPassword(DependencyObject dependencyObject, bool value)
    {
        dependencyObject.SetValue(UpdatingPasswordProperty, value);
    }

    private static void OnBindPasswordChanged(DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not PasswordBox passwordBox)
            return;

        if ((bool)e.OldValue)
            passwordBox.PasswordChanged -= OnPasswordChanged;

        if ((bool)e.NewValue)
            passwordBox.PasswordChanged += OnPasswordChanged;
    }

    private static void OnBoundPasswordChanged(DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not PasswordBox passwordBox
            || !GetBindPassword(passwordBox)
            || GetUpdatingPassword(passwordBox))
            return;

        passwordBox.Password = e.NewValue as string ?? string.Empty;
    }

    private static void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox passwordBox)
            return;

        SetUpdatingPassword(passwordBox, true);
        SetBoundPassword(passwordBox, passwordBox.Password);
        SetUpdatingPassword(passwordBox, false);
    }
}
