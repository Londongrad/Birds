using Birds.Application.DTOs;
using Birds.Application.Queries.GetAllBirds;
using MediatR;
using Microsoft.Extensions.Hosting;

namespace Birds.UI.Services.Stores.BirdStore
{
    /// <summary>
    /// Сервис инициализации хранилища птиц при старте приложения.
    /// 
    /// Реализует <see cref="IHostedService"/>, поэтому вызывается автоматически
    /// в момент запуска WPF-приложения, когда поднимается Generic Host.
    /// <para>
    /// Основная задача — загрузить список птиц из базы данных с помощью <see cref="IMediator"/>
    /// и заполнить общую коллекцию <see cref="IBirdStore"/> для использования
    /// во всех ViewModel.
    /// </para>
    /// </summary>
    public class BirdStoreInitializer : IHostedService
    {
        private readonly IBirdStore _birdStore;
        private readonly IMediator _mediator;

        /// <summary>
        /// Создаёт новый экземпляр сервиса инициализации хранилища птиц.
        /// </summary>
        /// <param name="birdStore">Общее хранилище коллекции птиц.</param>
        /// <param name="mediator">MediatR-объект для выполнения запросов к базе данных.</param>
        public BirdStoreInitializer(IBirdStore birdStore, IMediator mediator)
        {
            _birdStore = birdStore;
            _mediator = mediator;
        }

        /// <summary>
        /// Метод, вызываемый при старте приложения.
        /// Загружает всех птиц из базы и добавляет их в <see cref="IBirdStore"/>.
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var birds = await _mediator.Send(new GetAllBirdsQuery(), cancellationToken);

            foreach (BirdDTO bird in birds)
                _birdStore.Birds.Add(bird);
        }

        /// <summary>
        /// Метод, вызываемый при завершении приложения.
        /// В данном случае не требуется никакой логики.
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
