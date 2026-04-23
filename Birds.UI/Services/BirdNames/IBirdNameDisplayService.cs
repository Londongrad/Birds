using Birds.Domain.Enums;

namespace Birds.UI.Services.BirdNames;

public interface IBirdNameDisplayService
{
    string GetDisplayName(BirdSpecies bird);
}
