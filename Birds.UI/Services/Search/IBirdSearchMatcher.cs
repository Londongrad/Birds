using Birds.Application.DTOs;

namespace Birds.UI.Services.Search;

public interface IBirdSearchMatcher
{
    string NormalizeQuery(string? query);

    bool Matches(BirdDTO bird, string normalizedQuery);
}
