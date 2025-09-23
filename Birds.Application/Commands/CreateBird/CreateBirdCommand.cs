using Birds.Application.DTOs;
using MediatR;

namespace Birds.Application.Commands.CreateBird
{
    public record CreateBirdCommand(CreateBirdDTO Dto) : IRequest<Guid>;
}
