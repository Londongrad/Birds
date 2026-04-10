using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using MediatR;

namespace Birds.Application.Commands.ImportBirds
{
    public sealed record ImportBirdsCommand(IReadOnlyList<BirdDTO> Birds)
        : IRequest<Result<BirdImportResultDTO>>;
}
