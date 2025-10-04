using Birds.Application.DTOs;
using Birds.UI.Services.Factories.BirdViewModelFactory;
using Birds.UI.ViewModels;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace Birds.UI.Converters
{
    /// <summary>
    /// Конвертер, преобразующий объект <see cref="BirdDTO"/> в <see cref="BirdViewModel"/>.
    /// Используется в <see cref="DataTemplate"/> внутри списка птиц, чтобы при отрисовке элементов
    /// создавать экземпляры <see cref="BirdViewModel"/> только для реально видимых элементов
    /// (ленивая инициализация с поддержкой виртуализации).
    /// </summary>
    /// <remarks>
    /// Этот конвертер необходим, потому что в коллекции во <see cref="BirdListViewModel"/> хранятся DTO-объекты,
    /// но для взаимодействия с UI используется полноценная <see cref="BirdViewModel"/>.
    /// <para>
    /// С помощью фабрики <see cref="IBirdViewModelFactory"/> происходит инъекция зависимостей в App.xaml.cs
    /// и правильное создание экземпляров <see cref="BirdViewModel"/>.
    /// </para>
    /// </remarks>
    public class BirdVmConverter : IValueConverter
    {
        /// <summary>
        /// Фабрика для создания экземпляров <see cref="BirdViewModel"/> на основе <see cref="BirdDTO"/>.
        /// Обязательно должна быть установлена (через ресурсы XAML или вручную в коде).
        /// <para>
        /// Задается в App.xaml.cs.
        /// </para>
        /// </summary>
        public IBirdViewModelFactory? Factory { get; set; }

        /// <summary>
        /// Конструктор без параметров необходим для корректной работы XAML.
        /// <para>
        /// В случае данного конвертера определен явно, хотя это не обязательно. Главное, чтобы был пустой конструктор.
        /// </para>
        /// </summary>
        public BirdVmConverter() { }

        /// <summary>
        /// Выполняет преобразование <see cref="BirdDTO"/> в <see cref="BirdViewModel"/>.
        /// </summary>
        /// <param name="value">Объект <see cref="BirdDTO"/>, переданный из ItemsSource.</param>
        /// <param name="targetType">Целевой тип (не используется).</param>
        /// <param name="parameter">Дополнительный параметр (не используется).</param>
        /// <param name="culture">Информация о культуре (не используется).</param>
        /// <returns>Созданный <see cref="BirdViewModel"/> или null, если value не <see cref="BirdDTO"/>.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is BirdDTO dto)
            {
                if (Factory != null)
                    return Factory.Create(dto);

                Debug.WriteLine("⚠ BirdVmConverter: Factory is not set!");
            }
            else
            {
                Debug.WriteLine($"⚠ BirdVmConverter: unexpected value {value?.GetType().Name}");
            }

            return Binding.DoNothing; // лучше чем null!
        }

        /// <summary>
        /// Обратное преобразование не поддерживается.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
