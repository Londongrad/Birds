using Birds.Application.DTOs;
using MediatR;
using Birds.UI.ViewModels;

namespace Birds.UI.Services.Factories.BirdViewModelFactory
{
    /// <summary>
    /// A factory responsible for creating instances of <see cref="BirdViewModel"/>
    /// from data transfer objects (<see cref="BirdDTO"/>).
    /// </summary>
    /// <remarks>
    /// This class encapsulates the logic of creating view models, ensuring
    /// they are properly constructed with all required dependencies.  
    /// The <see cref="IMediator"/> is used to facilitate communication
    /// between the view models and other components of the application.
    /// </remarks>
    public class BirdViewModelFactory : IBirdViewModelFactory
    {
        private readonly IMediator _mediator;

        /// <summary>
        /// Initializes a new instance of the <see cref="BirdViewModelFactory"/> class.
        /// </summary>
        /// <param name="mediator">
        /// The mediator instance used to send requests and notifications
        /// between the view models and other parts of the application.
        /// </param>
        public BirdViewModelFactory(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <inheritdoc/>
        public BirdViewModel Create(BirdDTO dto) => new(dto, _mediator);
    }
}
