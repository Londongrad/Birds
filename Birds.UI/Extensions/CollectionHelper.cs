using System.Collections;

namespace Birds.UI.Extensions;

/// <summary>
/// Набор методов расширения для индексированных коллекций (<see cref="IList{T}"/>),
/// обеспечивающих безопасную замену и добавление элементов.
/// </summary>
public static partial class CollectionHelper
{
    /// <summary>
    /// Заменяет первый элемент в коллекции, удовлетворяющий указанному условию.
    /// </summary>
    /// <typeparam name="T">Тип элементов коллекции.</typeparam>
    /// <param name="list">Индексированная коллекция, в которой выполняется поиск и замена.</param>
    /// <param name="predicate">
    /// Условие, определяющее элемент, который требуется заменить.
    /// Если элемент, удовлетворяющий условию, найден — он будет заменён на <paramref name="newItem"/>.
    /// </param>
    /// <param name="newItem">Элемент, который будет установлен вместо найденного элемента.</param>
    /// <returns>
    /// <see langword="true"/>, если элемент был найден и успешно заменён;
    /// в противном случае — <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Если коллекция реализует <see cref="ICollection"/> и поддерживает синхронизацию через свойство
    /// <see cref="ICollection.SyncRoot"/>, то операция выполняется под блокировкой, обеспечивая потокобезопасность.
    /// </remarks>
    public static bool Replace<T>(this IList<T> list, Predicate<T> predicate, T newItem)
    {
        if (list is ICollection coll)
        {
            lock (coll.SyncRoot)
                return list.PrivateReplace(predicate, newItem);
        }
        else
        {
            return list.PrivateReplace(predicate, newItem);
        }
    }

    /// <summary>
    /// Выполняет замену первого элемента, удовлетворяющего указанному условию, 
    /// либо добавляет новый элемент, если подходящий элемент не найден.
    /// </summary>
    /// <typeparam name="T">Тип элементов коллекции.</typeparam>
    /// <param name="list">Индексированная коллекция, в которой выполняется операция.</param>
    /// <param name="predicate">
    /// Условие, определяющее элемент, который требуется заменить.
    /// Если элемент найден — он будет заменён на <paramref name="newItem"/>;
    /// если не найден — новый элемент будет добавлен в конец коллекции.
    /// </param>
    /// <param name="newItem">Элемент, который будет добавлен или установлен вместо найденного элемента.</param>
    /// <returns>
    /// <see langword="true"/>, если элемент был найден и заменён;
    /// <see langword="false"/>, если элемент не найден и был добавлен в коллекцию.
    /// </returns>
    /// <remarks>
    /// Если коллекция реализует <see cref="ICollection"/> и поддерживает синхронизацию через свойство
    /// <see cref="ICollection.SyncRoot"/>, операция выполняется под блокировкой.
    /// Этот метод удобно использовать для обновления элементов в коллекции, когда известно, что элемент может как существовать, так и отсутствовать.
    /// </remarks>
    public static bool ReplaceOrAdd<T>(this IList<T> list, Predicate<T> predicate, T newItem)
    {
        if (list is ICollection coll)
        {
            lock (coll.SyncRoot)
                return list.PrivateReplaceOrAdd(predicate, newItem);
        }
        else
        {
            return list.PrivateReplaceOrAdd(predicate, newItem);
        }
    }

    // ------------------------------------------
    // Приватные внутренние версии без блокировок
    // ------------------------------------------

    private static bool PrivateReplace<T>(this IList<T> list, Predicate<T> predicate, T newItem)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (predicate(list[i]))
            {
                list[i] = newItem;
                return true;
            }
        }
        return false;
    }

    private static bool PrivateReplaceOrAdd<T>(this IList<T> list, Predicate<T> predicate, T newItem)
    {
        if (list.PrivateReplace(predicate, newItem))
            return true;

        list.Add(newItem);
        return false;
    }
}
