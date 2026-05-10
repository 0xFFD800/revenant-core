using System.Collections.Generic;

namespace RevenantCore.Util;

/// <summary>
/// Represents a dictionary which can have multiple values per key, for which the order of the values matters.
/// Although multiple values can exist for each key, key/value pairs must be individually unique.
/// </summary>
/// <typeparam name="K">The key type of the dictionary, which may identify multiple values.</typeparam>
/// <typeparam name="V">The value type of the dictionary, which will be stored in ordered lists.</typeparam>
public class OrderedDict<K, V> where V : notnull
{
    private readonly Dictionary<K, List<V>> dict = [];

    /// <summary>
    /// Adds a key/value pair to the dictionary. 
    /// If no keys yet exist with this value, a new value list will be added for this key.
    /// </summary>
    /// <param name="key">The key by which to identify this value.</param>
    /// <param name="value">The value to add to this dictionary.</param>
    /// <returns>Whether the key-value pair was unique and thus could be added to the dictionary.</returns>
    public bool Add(K key, V value)
    {
        if (!dict.TryGetValue(key, out List<V> values))
        {
            values = [];
            dict.Add(key, values);
        } else if (values.Contains(value))
            return false;

        values.Add(value);
        return true;
    }

    /// <summary>
    /// Gets all values for <paramref name="key"/> as an ordered array.
    /// </summary>
    /// <param name="key">The key to find values for.</param>
    /// <returns>All values for <paramref name="key"/> as an ordered array, or an empty array if none are found.</returns>
    public V[] Get(K key)
    {
        return [..dict.GetValueOrDefault(key, [])];
    }

    /// <summary>
    /// Returns whether the dictionary contains the specified key.
    /// </summary>
    /// <param name="key">The key to check the dictionary for.</param>
    /// <returns>Whether <paramref name="key"/> is present in the dictionary.</returns>
    public bool Has(K key)
    {
        return dict.ContainsKey(key);
    }

    /// <summary>
    /// Removes a key/value pair from the dictionary, if it exists.
    /// </summary>
    /// <param name="key">The key by which the value is identified.</param>
    /// <param name="value">The value to remove from the dictionary.</param>
    /// <returns>Whether the key-value pair existed in the dictionary and could be removed</returns>
    public bool Remove(K key, V value)
    {
        if (dict.TryGetValue(key, out List<V> values) && values.Remove(value))
        {
            if (values.Count == 0)
                dict.Remove(key);
            return true;
        } else
            return false;
    }

    /// <summary>
    /// Sorts the values in each underlying list using the specified comparer.
    /// </summary>
    /// <param name="comparer">The comparer with which to sort elements of the underlying lists.</param>
    public void Sort(IComparer<V> comparer)
    {
        foreach (List<V> values in dict.Values)
            values.Sort(comparer);
    }
}