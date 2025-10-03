using Birds.Application.DTOs;
using MediatR;
using Birds.UI.ViewModels;

namespace Birds.UI.Services.Factories.BirdViewModelFactory
{
    /// <summary>
    /// Фабрика, отвечающая за создание экземпляров <see cref="BirdViewModel"/>
    /// из объектов передачи данных <see cref="BirdDTO"/>.
    /// </summary>
    /// <remarks>
    /// Класс инкапсулирует логику создания view-model, гарантируя,
    /// что они будут корректно сконструированы со всеми необходимыми зависимостями.  
    /// Для взаимодействия между view-model и другими компонентами приложения
    /// используется <see cref="IMediator"/>.
    /// </remarks>
    public class BirdViewModelFactory : IBirdViewModelFactory
    {
        private readonly IMediator _mediator;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="BirdViewModelFactory"/>.
        /// </summary>
        /// <param name="mediator">
        /// Экземпляр медиатора, используемый для отправки запросов и уведомлений
        /// между view-model и остальными частями приложения.
        /// </param>
        public BirdViewModelFactory(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <inheritdoc/>
        public BirdViewModel Create(BirdDTO dto) => new(dto, _mediator);
    }
}
