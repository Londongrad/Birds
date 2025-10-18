using AutoMapper;
using Birds.Application.Common.Models;
using Birds.Application.Interfaces;
using Birds.Domain.Entities;
using MediatR;

namespace Birds.Application.Commands.UpdateBird
{
    public class UpdateBirdCommandHandler(
        IBirdRepository repository,
        IMapper mapper)
        : IRequestHandler<UpdateBirdCommand, Result>
    {
        public async Task<Result> Handle(UpdateBirdCommand request, CancellationToken cancellationToken)
        {
            if (request is null)
                return Result.Failure("Request cannot be null");

            var bird = mapper.Map<Bird>(request);

            await repository.UpdateAsync(bird, cancellationToken);

            return Result.Success();
        }
    }
}
