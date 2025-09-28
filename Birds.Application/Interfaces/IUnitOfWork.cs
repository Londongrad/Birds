namespace Birds.Application.Interfaces
{
    public interface IUnitOfWork
    {
        /// <summary>Сохраняет все изменения, сделанные в текущем контексте.</summary>
        /// <returns>Количество изменённых записей в базе данных.</returns>
        /// <remarks>
        /// Метод фиксирует изменения, сделанные через репозитории.
        /// Вызывать его необходимо вручную после операций <c>AddAsync</c>, <c>Update</c> и <c>Remove</c>.
        /// </remarks>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}