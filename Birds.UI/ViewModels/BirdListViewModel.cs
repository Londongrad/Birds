using Birds.Application.DTOs;
using Birds.Application.Notifications;
using Birds.Application.Queries.GetAllBirds;
using Birds.Domain.Enums;
using Birds.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace Birds.UI.ViewModels
{
    public partial class BirdListViewModel : ObservableObject,
                                             INotificationHandler<BirdCreatedNotification>,
                                             IAsyncNavigatedTo
    {
        public BirdListViewModel(IMediator mediator)
        {
            _mediator = mediator;

            BirdsView = CollectionViewSource.GetDefaultView(Birds);
        }

        #region [ Fields ]

        private readonly IMediator _mediator;
        private bool _isLoaded; // Флаг: выполнена ли начальная загрузка (чтобы не грузить повторно)
        private int _isLoading; // Флаг: выполняется ли загрузка в данный момент (0 = нет, 1 = да)

        #endregion [ Fields ]

        #region [ Properties ]

        public ObservableCollection<BirdDTO> Birds { get; } = new();
        public static Array BirdNames => Enum.GetValues(typeof(BirdsName));
        public ICollectionView BirdsView { get; }

        #endregion [ Properties ]

        #region [ Methods ]

        /// <summary>
        /// Загружает всех птиц из БД.
        /// Используется защита от параллельных вызовов (Interlocked).
        /// </summary>
        private async Task LoadAsync()
        {
            // Interlocked предотвращает гонку условий при одновременных вызовах
            if (Interlocked.Exchange(ref _isLoading, 1) == 1) return;
            try
            {
                Birds.Clear();
                var result = await _mediator.Send(new GetAllBirdsQuery());

                //// DeferRefresh уменьшает количество обновлений UI при массовой загрузке
                //using (BirdsView.DeferRefresh())
                //{
                    foreach (var bird in result)
                        Birds.Add(bird);
                //}

                _isLoaded = true;
            }
            finally
            {
                Interlocked.Exchange(ref _isLoading, 0);
            }
        }

        public Task Handle(BirdCreatedNotification notification, CancellationToken cancellationToken)
        {
            var d = System.Windows.Application.Current.Dispatcher;
            if (d.CheckAccess())
                Birds.Add(notification.Bird); // Уже на UI-потоке — добавляем напрямую
            else
                d.BeginInvoke(() => Birds.Add(notification.Bird)); // Переключаемся на UI-поток
            return Task.CompletedTask;
        }

        /// <summary>
        /// Вызывает загрузку только один раз при первом переходе на эту ViewModel.
        /// </summary>
        public async Task OnNavigatedToAsync()
        {
            if (!_isLoaded)
            {
                await LoadAsync();
            }
        }

        #endregion [ Methods ]

        #region [ Commands ]



        #endregion [ Commands ]
    }
}
