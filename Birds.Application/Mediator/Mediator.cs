using Microsoft.Extensions.DependencyInjection;

namespace Birds.Application.Mediator
{
    public class Mediator(IServiceProvider provider) : IMediator
    {
        private readonly IServiceProvider _provider = provider;

        public async Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
        {
            // 1. Определяем, какой тип хендлера нужен
            var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResponse));

            // 2. Достаём этот хендлер из DI-контейнера
            dynamic handler = _provider.GetRequiredService(handlerType);

            // 3. Запускаем Handle у этого хендлера
            return await handler.Handle((dynamic)query, cancellationToken);
        }

        public async Task Send(ICommand command, CancellationToken cancellationToken = default)
        {
            var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());
            dynamic handler = _provider.GetRequiredService(handlerType);
            await handler.Handle((dynamic)command, cancellationToken);
        }
    }
}
