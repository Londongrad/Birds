using Birds.Application.Common.Models;
using Birds.Application.DTOs;

namespace Birds.UI.Services.Import.Interfaces;

public interface IImportService
{
    Task<Result<IReadOnlyList<BirdDTO>>> ImportAsync(string path, CancellationToken cancellationToken);
}