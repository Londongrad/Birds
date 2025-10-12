using System.Collections;

namespace Birds.UI.Extensions;

/// <summary>
/// A set of extension methods for indexed collections (<see cref="IList{T}"/>)
/// that provide safe element replacement and addition.
/// </summary>
public static partial class CollectionHelper
{
    /// <summary>
    /// Replaces the first element in the collection that matches the specified condition.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="list">The indexed collection where the search and replacement are performed.</param>
    /// <param name="predicate">
    /// A condition that determines the element to be replaced.
    /// If an element satisfying the condition is found, it will be replaced with <paramref name="newItem"/>.
    /// </param>
    /// <param name="newItem">The element that will replace the found element.</param>
    /// <returns>
    /// <see langword="true"/> if the element was found and successfully replaced;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// If the collection implements <see cref="ICollection"/> and supports synchronization
    /// via the <see cref="ICollection.SyncRoot"/> property, the operation is performed under a lock to ensure thread safety.
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
    /// Replaces the first element that matches the specified condition, 
    /// or adds a new element if no matching element is found.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="list">The indexed collection where the operation is performed.</param>
    /// <param name="predicate">
    /// A condition that determines the element to be replaced.
    /// If the element is found, it will be replaced with <paramref name="newItem"/>;
    /// if not found, the new element will be added to the end of the collection.
    /// </param>
    /// <param name="newItem">The element that will be added or used to replace the found element.</param>
    /// <returns>
    /// <see langword="true"/> if the element was found and replaced;
    /// <see langword="false"/> if the element was not found and was added to the collection.
    /// </returns>
    /// <remarks>
    /// If the collection implements <see cref="ICollection"/> and supports synchronization
    /// via the <see cref="ICollection.SyncRoot"/> property, the operation is performed under a lock.
    /// This method is useful for updating elements in a collection when the element may or may not already exist.
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
    // Private internal versions without locking
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
