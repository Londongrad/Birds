using System.Windows;
using System.Windows.Controls;

namespace Birds.UI.Views.UserControls;

public partial class BirdItemControl : UserControl
{
    public static readonly DependencyProperty DisplayIndexProperty = DependencyProperty.Register(
        nameof(DisplayIndex),
        typeof(string),
        typeof(BirdItemControl),
        new PropertyMetadata(string.Empty));

    public BirdItemControl()
    {
        InitializeComponent();
    }

    public string DisplayIndex
    {
        get => (string)GetValue(DisplayIndexProperty);
        set => SetValue(DisplayIndexProperty, value);
    }
}
