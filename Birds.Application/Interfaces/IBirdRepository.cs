using Birds.Application.Exceptions;
using Birds.Domain.Entities;

namespace Birds.Application.Interfaces
{
    /// <summary>Определяет контракт репозитория для работы с сущностями <see cref="Bird"/>.</summary>
    public interface IBirdRepository
    {
        /// <summary>Получает сущность <see cref="Bird"/> по её идентификатору.</summary>
        /// <returns>Экземпляр <see cref="Bird"/>.</returns>
        /// <exception cref="NotFoundException"></exception>
        Task<Bird> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>Возвращает неотслеживаемую коллекцию всех птиц.</summary>
        /// <returns>Коллекция <see cref="IReadOnlyList{Bird}"/>.</returns>
        Task<IReadOnlyList<Bird>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>Добавляет новую птицу в контекст отслеживания.</summary>
        /// <remarks>
        /// Метод <b>не выполняет сохранение в базе данных</b>.
        /// Для фиксации изменений необходимо вызвать <see cref="IUnitOfWork.SaveChangesAsync"/>.
        /// </remarks>
        Task<Guid> AddAsync(Bird bird, CancellationToken cancellationToken = default);

        /// <summary>Обновляет существующую птицу в контексте отслеживания.</summary>
        /// <remarks>
        /// Метод <b>не выполняет сохранение в базе данных</b>.
        /// Для фиксации изменений необходимо вызвать <see cref="IUnitOfWork.SaveChangesAsync"/>.
        /// </remarks>
        void Update(Bird bird);

        /// <summary>Удаляет птицу из контекста отслеживания.</summary>
        /// <remarks>
        /// Метод <b>не выполняет сохранение в базе данных</b>.
        /// Для фиксации изменений необходимо вызвать <see cref="IUnitOfWork.SaveChangesAsync"/>.
        /// </remarks>
        void Remove(Bird bird);
    }
}