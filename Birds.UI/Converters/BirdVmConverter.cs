using Birds.Application.DTOs;
using Birds.UI.Services.Factories.BirdViewModelFactory;
using Birds.UI.ViewModels;
using System.Globalization;
using System.Windows.Data;

namespace Birds.UI.Converters
{
    /// <summary>
    /// Converter that transforms a <see cref="BirdDTO"/> object into a <see cref="BirdViewModel"/>.
    /// Used in a <see cref="DataTemplate"/> within the bird list to create instances of
    /// <see cref="BirdViewModel"/> only for actually visible items during rendering
    /// (lazy initialization with virtualization support).
    /// </summary>
    /// <remarks>
    /// This converter is necessary because the collection in <see cref="BirdListViewModel"/> stores DTO objects,
    /// but interaction with the UI requires a full <see cref="BirdViewModel"/>.
    /// <para>
    /// Through the <see cref="IBirdViewModelFactory"/>, dependencies are injected in App.xaml.cs,
    /// ensuring proper creation of <see cref="BirdViewModel"/> instances.
    /// </para>
    /// </remarks>
    public class BirdVmConverter : IValueConverter
    {
        /// <summary>
        /// Factory for creating <see cref="BirdViewModel"/> instances based on <see cref="BirdDTO"/>.
        /// Must be set (via XAML resources or manually in code).
        /// <para>
        /// Configured in App.xaml.cs.
        /// </para>
        /// </summary>
        public IBirdViewModelFactory? Factory { get; set; }

        /// <summary>
        /// Parameterless constructor required for correct XAML operation.
        /// <para>
        /// Explicitly defined here, although not strictly necessary. The main requirement is to have a parameterless constructor.
        /// </para>
        /// </summary>
        public BirdVmConverter() { }

        /// <summary>
        /// Converts a <see cref="BirdDTO"/> to a <see cref="BirdViewModel"/>.
        /// </summary>
        /// <param name="value">A <see cref="BirdDTO"/> object passed from ItemsSource.</param>
        /// <param name="targetType">The target type (not used).</param>
        /// <param name="parameter">An additional parameter (not used).</param>
        /// <param name="culture">Culture information (not used).</param>
        /// <returns>The created <see cref="BirdViewModel"/>, or null if value is not a <see cref="BirdDTO"/>.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is BirdDTO dto)
            {
                if (Factory != null)
                    return Factory.Create(dto);
            }

            return Binding.DoNothing; // better than null!
        }

        /// <summary>
        /// Reverse conversion is not supported.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
