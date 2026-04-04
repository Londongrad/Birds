namespace Birds.Infrastructure.Services
{
    public interface IDatabaseInitializer
    {
        Task InitializeAsync(CancellationToken cancellationToken);
    }
}
