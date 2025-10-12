namespace Birds.Application.Interfaces
{
    public interface IUnitOfWork
    {
        /// <summary>Saves all changes made in the current context.</summary>
        /// <returns>The number of records modified in the database.</returns>
        /// <remarks>
        /// This method commits the changes made through the repositories.
        /// It must be called manually after performing <c>AddAsync</c>, <c>Update</c>, or <c>Remove</c> operations.
        /// </remarks>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}