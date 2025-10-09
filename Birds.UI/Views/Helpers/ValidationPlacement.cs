using System.Windows;
using System.Windows.Controls;

namespace Birds.UI.Views.Helpers
{
    /// <summary>
    /// Предоставляет прикрепляемое свойство, позволяющее выбрать расположение
    /// визуализации ошибок валидации (<see cref="Validation.ErrorTemplate"/>)
    /// для отдельных элементов управления.
    /// </summary>
    /// <remarks>
    /// Данное свойство позволяет динамически подменять шаблон ошибки
    /// в зависимости от значения <see cref="PlacementProperty"/> —
    /// например, отображать сообщения об ошибках справа или снизу от контрола,
    /// не дублируя стили и шаблоны.
    ///
    /// <para>
    /// По умолчанию, если свойство <see cref="PlacementProperty"/> не задано,
    /// используется стандартный шаблон <c>FieldErrorTemplate</c> (расположение справа).
    /// </para>
    /// </remarks>
    public static class ValidationPlacement
    {
        #region [ PlacementProperty ]

        /// <summary>
        /// Определяет расположение отображения ошибок валидации —
        /// справа от контрола или под ним.
        /// </summary>
        /// <remarks>
        /// Значение по умолчанию — <see cref="PlacementMode.Right"/>.
        /// 
        /// <para>
        /// При изменении значения свойство автоматически подменяет
        /// используемый шаблон ошибки, устанавливая в качестве ресурса
        /// либо <c>FieldErrorTemplate</c>, либо <c>FieldErrorBottomTemplate</c>.
        /// </para>
        /// </remarks>
        public static readonly DependencyProperty PlacementProperty =
            DependencyProperty.RegisterAttached(
                "Placement",
                typeof(PlacementMode),
                typeof(ValidationPlacement),
                new PropertyMetadata(PlacementMode.Right, OnPlacementChanged));

        /// <summary>
        /// Задаёт расположение ошибок валидации для указанного элемента управления.
        /// </summary>
        /// <param name="element">Элемент, к которому применяется прикреплённое свойство.</param>
        /// <param name="value">Новое значение расположения ошибок валидации.</param>
        public static void SetPlacement(DependencyObject element, PlacementMode value)
            => element.SetValue(PlacementProperty, value);

        /// <summary>
        /// Возвращает текущее расположение ошибок валидации
        /// для указанного элемента управления.
        /// </summary>
        /// <param name="element">Элемент, из которого считывается значение.</param>
        /// <returns>Значение перечисления <see cref="PlacementMode"/>.</returns>
        public static PlacementMode GetPlacement(DependencyObject element)
            => (PlacementMode)element.GetValue(PlacementProperty);

        /// <summary>
        /// Вызывается при изменении значения свойства <see cref="Placement"/>.
        /// Устанавливает соответствующий шаблон ошибки (<see cref="ControlTemplate"/>)
        /// в свойство <see cref="Validation.ErrorTemplate"/>.
        /// </summary>
        /// <param name="d">Элемент, к которому прикреплено свойство.</param>
        /// <param name="e">Аргументы изменения значения.</param>
        private static void OnPlacementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement fe) return;

            // 1) Всегда подключаем универсальный шаблон
            fe.SetResourceReference(Validation.ErrorTemplateProperty, "FieldErrorTemplate_Universal");

            // 2) Направление задаём через Dock — его читают триггеры внутри шаблона
            var mode = (PlacementMode)e.NewValue;
            var dock = mode switch
            {
                PlacementMode.Right => Dock.Right,
                PlacementMode.Left => Dock.Left,
                PlacementMode.Top => Dock.Top,
                PlacementMode.Bottom => Dock.Bottom,
                _ => Dock.Right
            };
            SetDock(fe, dock);
        }

        #endregion [ PlacementProperty ]

        #region [ DockProperty ]

        /// <summary>
        /// Определяет прикрепляемое свойство <see cref="DockProperty"/>,
        /// позволяющее задать расположение области отображения ошибок валидации
        /// относительно элемента управления.
        /// </summary>
        /// <remarks>
        /// Это свойство используется внутри шаблона ошибок (<see cref="ControlTemplate"/>)
        /// для определения стороны, с которой отображаются сообщения об ошибках:
        /// слева, справа, сверху или снизу от контрола.
        ///
        /// <para>
        /// <br/><b>⚠️ Не предназначено для задания в XAML вручную. Использовать <see cref="PlacementProperty"/>.</b>
        /// </para>
        /// </remarks>
        public static readonly DependencyProperty DockProperty =
            DependencyProperty.RegisterAttached(
                "Dock",
                typeof(Dock),
                typeof(ValidationPlacement),
                new PropertyMetadata(Dock.Right));

        /// <summary>
        /// Задаёт значение прикрепляемого свойства <see cref="DockProperty"/>
        /// для указанного элемента управления.
        /// </summary>
        /// <param name="element">
        /// Элемент, к которому применяется свойство <see cref="DockProperty"/>.
        /// </param>
        /// <param name="value">
        /// Новое значение перечисления <see cref="Dock"/>,
        /// определяющее сторону отображения ошибок.
        /// </param>
        public static void SetDock(DependencyObject element, Dock value)
            => element.SetValue(DockProperty, value);

        /// <summary>
        /// Возвращает текущее значение прикрепляемого свойства <see cref="DockProperty"/>
        /// для указанного элемента управления.
        /// </summary>
        /// <param name="element">
        /// Элемент, из которого извлекается значение свойства.
        /// </param>
        /// <returns>
        /// Значение перечисления <see cref="Dock"/>,
        /// указывающее, где должны отображаться сообщения об ошибках
        /// относительно элемента управления.
        /// </returns>
        public static Dock GetDock(DependencyObject element)
            => (Dock)element.GetValue(DockProperty);

        #endregion [ DockProperty ]

        #region [ MaxErrorWidthProperty ]

        /// <summary>
        /// Определяет максимальную ширину блока сообщений об ошибках валидации.
        /// </summary>
        /// <remarks>
        /// Значение задаётся в пикселях и применяется к элементу визуализации ошибки,
        /// обычно к <see cref="TextBlock"/>, который выводит текст ошибки внутри шаблона
        /// (<see cref="Validation.ErrorTemplate"/>).
        ///
        /// <para>
        /// Если свойство не указано, используется значение по умолчанию — <c>270</c>.
        /// </para>
        ///
        /// <para>
        /// Это свойство особенно полезно, если для разных представлений требуется
        /// различная ширина текста ошибок — например, более узкая колонка в форме
        /// добавления и более широкая в списке.
        /// </para>
        /// </remarks>
        public static readonly DependencyProperty MaxErrorWidthProperty =
            DependencyProperty.RegisterAttached(
                "MaxErrorWidth",
                typeof(double),
                typeof(ValidationPlacement),
                new PropertyMetadata(270.0));

        /// <summary>
        /// Устанавливает максимальную ширину блока сообщений об ошибках валидации
        /// для указанного элемента управления.
        /// </summary>
        /// <param name="element">Элемент, к которому применяется прикреплённое свойство.</param>
        /// <param name="value">Максимальная ширина блока ошибок (в пикселях).</param>
        public static void SetMaxErrorWidth(DependencyObject element, double value)
            => element.SetValue(MaxErrorWidthProperty, value);

        /// <summary>
        /// Возвращает максимальную ширину блока сообщений об ошибках валидации
        /// для указанного элемента управления.
        /// </summary>
        /// <param name="element">Элемент, из которого считывается значение свойства.</param>
        /// <returns>Текущее значение максимальной ширины блока ошибок (в пикселях).</returns>
        public static double GetMaxErrorWidth(DependencyObject element)
            => (double)element.GetValue(MaxErrorWidthProperty);

        #endregion [ MaxErrorWidthProperty ]
    }

    /// <summary>
    /// Определяет возможные варианты расположения сообщений об ошибках валидации.
    /// </summary>
    public enum PlacementMode
    {
        Right,
        Bottom,
        Left,
        Top
    }
}
