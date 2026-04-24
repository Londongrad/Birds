using System.Reflection;
using Birds.Application.Behaviors;
using Birds.Application.Commands.CreateBird;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Birds.Application;

public static class DependencyInjection
{
    public static void AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // MediatR executes pipeline behaviors in registration order. Keep exception handling outermost.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionHandlingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddMediatR(cfg => { cfg.RegisterServicesFromAssembly(typeof(CreateBirdCommandHandler).Assembly); });
    }
}
