using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;
using System.Windows;

namespace Birds.UI.Services.Navigation
{
    /// <summary>
    /// Событие навигации, публикуемое через MediatR после открытия нового окна.
    /// <para>
    /// Содержит ссылку на <see cref="Window"/>, которое было открыто,
    /// и соответствующую <see cref="ObservableObject"/> ViewModel, назначенную этому окну.
    /// </para>
    /// Другие сервисы (например, сервис нотификаций) могут обработать это событие,
    /// чтобы привязаться к новому окну или выполнить дополнительную логику,
    /// связанную с переключением контекста.
    /// </summary>
    /// <param name="Window">Экземпляр окна, которое было открыто.</param>
    /// <param name="ViewModel">ViewModel, ставшая активной для данного окна.</param>
    public record NavigatedEvent(Window Window, ObservableObject ViewModel) : INotification;
}
