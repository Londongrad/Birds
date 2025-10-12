using System.Windows;
using System.Windows.Controls;

namespace Birds.UI.Views.Helpers
{
    /// <summary>
    /// Provides an attached property that allows selecting the placement
    /// of validation error visuals (<see cref="Validation.ErrorTemplate"/>)
    /// for individual UI controls.
    /// </summary>
    /// <remarks>
    /// This property enables dynamically switching the error template
    /// based on the <see cref="PlacementProperty"/> value —
    /// for example, showing validation messages either to the right or below a control
    /// without duplicating styles and templates.
    ///
    /// <para>
    /// By default, if <see cref="PlacementProperty"/> is not set,
    /// the standard template <c>FieldErrorTemplate</c> (right placement) is used.
    /// </para>
    /// </remarks>
    public static class ValidationPlacement
    {
        #region [ PlacementProperty ]

        /// <summary>
        /// Defines the placement of validation error messages —
        /// either to the right of or below the control.
        /// </summary>
        /// <remarks>
        /// The default value is <see cref="PlacementMode.Right"/>.
        /// 
        /// <para>
        /// When the value changes, the property automatically updates
        /// the applied error template by setting the corresponding resource
        /// (<c>FieldErrorTemplate</c> or <c>FieldErrorBottomTemplate</c>).
        /// </para>
        /// </remarks>
        public static readonly DependencyProperty PlacementProperty =
            DependencyProperty.RegisterAttached(
                "Placement",
                typeof(PlacementMode),
                typeof(ValidationPlacement),
                new PropertyMetadata(PlacementMode.Right, OnPlacementChanged));

        /// <summary>
        /// Sets the placement of validation errors for the specified control.
        /// </summary>
        /// <param name="element">The control to which the attached property is applied.</param>
        /// <param name="value">The new placement value for validation errors.</param>
        public static void SetPlacement(DependencyObject element, PlacementMode value)
            => element.SetValue(PlacementProperty, value);

        /// <summary>
        /// Gets the current placement of validation errors
        /// for the specified control.
        /// </summary>
        /// <param name="element">The control from which to read the value.</param>
        /// <returns>The <see cref="PlacementMode"/> enumeration value.</returns>
        public static PlacementMode GetPlacement(DependencyObject element)
            => (PlacementMode)element.GetValue(PlacementProperty);

        /// <summary>
        /// Called when the <see cref="Placement"/> property value changes.
        /// Sets the appropriate error template (<see cref="ControlTemplate"/>)
        /// in <see cref="Validation.ErrorTemplate"/>.
        /// </summary>
        /// <param name="d">The element to which the property is attached.</param>
        /// <param name="e">The property change event arguments.</param>
        private static void OnPlacementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement fe) return;

            // 1) Always apply the universal error template
            fe.SetResourceReference(Validation.ErrorTemplateProperty, "FieldErrorTemplate_Universal");

            // 2) Set the direction via Dock — triggers inside the template read this value
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
        /// Defines the <see cref="DockProperty"/> attached property,
        /// which determines where the validation error area is displayed
        /// relative to the control.
        /// </summary>
        /// <remarks>
        /// This property is used internally in the error template (<see cref="ControlTemplate"/>)
        /// to define the side (left, right, top, or bottom) on which the validation messages appear.
        ///
        /// <para>
        /// <b>⚠️ Not intended for manual use in XAML. Use <see cref="PlacementProperty"/> instead.</b>
        /// </para>
        /// </remarks>
        public static readonly DependencyProperty DockProperty =
            DependencyProperty.RegisterAttached(
                "Dock",
                typeof(Dock),
                typeof(ValidationPlacement),
                new PropertyMetadata(Dock.Right));

        /// <summary>
        /// Sets the <see cref="DockProperty"/> value
        /// for the specified control.
        /// </summary>
        /// <param name="element">
        /// The element to which the <see cref="DockProperty"/> is applied.
        /// </param>
        /// <param name="value">
        /// The <see cref="Dock"/> value specifying where the error messages should appear.
        /// </param>
        public static void SetDock(DependencyObject element, Dock value)
            => element.SetValue(DockProperty, value);

        /// <summary>
        /// Gets the current <see cref="DockProperty"/> value
        /// for the specified control.
        /// </summary>
        /// <param name="element">The element from which to retrieve the property value.</param>
        /// <returns>
        /// The <see cref="Dock"/> value indicating where
        /// the validation messages are displayed relative to the control.
        /// </returns>
        public static Dock GetDock(DependencyObject element)
            => (Dock)element.GetValue(DockProperty);

        #endregion [ DockProperty ]

        #region [ MaxErrorWidthProperty ]

        /// <summary>
        /// Defines the maximum width of the validation error message block.
        /// </summary>
        /// <remarks>
        /// The value is specified in pixels and applied to the visual element
        /// that displays the error message — typically a <see cref="TextBlock"/>
        /// inside the <see cref="Validation.ErrorTemplate"/>.
        ///
        /// <para>
        /// If not specified, the default value is <c>270</c>.
        /// </para>
        ///
        /// <para>
        /// This property is useful when different views require
        /// different error text widths — for example, narrower columns in add forms
        /// and wider ones in list views.
        /// </para>
        /// </remarks>
        public static readonly DependencyProperty MaxErrorWidthProperty =
            DependencyProperty.RegisterAttached(
                "MaxErrorWidth",
                typeof(double),
                typeof(ValidationPlacement),
                new PropertyMetadata(270.0));

        /// <summary>
        /// Sets the maximum width of the validation error message block
        /// for the specified control.
        /// </summary>
        /// <param name="element">The control to which the attached property is applied.</param>
        /// <param name="value">The maximum error block width (in pixels).</param>
        public static void SetMaxErrorWidth(DependencyObject element, double value)
            => element.SetValue(MaxErrorWidthProperty, value);

        /// <summary>
        /// Gets the maximum width of the validation error message block
        /// for the specified control.
        /// </summary>
        /// <param name="element">The control from which to read the property value.</param>
        /// <returns>The current maximum error block width (in pixels).</returns>
        public static double GetMaxErrorWidth(DependencyObject element)
            => (double)element.GetValue(MaxErrorWidthProperty);

        #endregion [ MaxErrorWidthProperty ]
    }

    /// <summary>
    /// Defines the possible placement options for displaying validation error messages.
    /// </summary>
    public enum PlacementMode
    {
        Right,
        Bottom,
        Left,
        Top
    }

}
