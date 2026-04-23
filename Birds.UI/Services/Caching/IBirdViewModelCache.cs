using Birds.Application.DTOs;
using Birds.UI.ViewModels;

namespace Birds.UI.Services.Caching;

public interface IBirdViewModelCache : IDisposable
{
    int Count { get; }

    BirdViewModel GetOrCreate(BirdDTO dto);

    void Refresh(BirdDTO dto);

    void Remove(Guid birdId);

    void Clear();
}
