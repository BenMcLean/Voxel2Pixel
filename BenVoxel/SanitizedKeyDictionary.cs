using System;
using System.Collections;
using System.Collections.Generic;

namespace BenVoxel;

/// <summary>
/// Restricts keys to max length 255 with last-in-wins behavior.
/// </summary>
public sealed class SanitizedKeyDictionary<T> : IDictionary<string, T>
{
	private readonly Dictionary<string, T> Dictionary = [];
	public static string SanitizeKey(string key)
	{
		key = key.Trim();
		return key[..Math.Min(key.Length, 255)];
	}
	#region IDictionary
	public T this[string key]
	{
		get => Dictionary[SanitizeKey(key)];
		set => Dictionary[SanitizeKey(key)] = value;
	}
	public ICollection<string> Keys => Dictionary.Keys;
	public ICollection<T> Values => Dictionary.Values;
	public int Count => Dictionary.Count;
	public bool IsReadOnly => ((ICollection<KeyValuePair<string, T>>)Dictionary).IsReadOnly;
	public void Add(string key, T value) => this[key] = value;
	public void Add(KeyValuePair<string, T> item) => this[item.Key] = item.Value;
	public void Clear() => Dictionary.Clear();
	public bool Contains(KeyValuePair<string, T> item) =>
		Dictionary.TryGetValue(SanitizeKey(item.Key), out T value)
			&& EqualityComparer<T>.Default.Equals(value, item.Value);
	public bool ContainsKey(string key) => Dictionary.ContainsKey(SanitizeKey(key));
	public void CopyTo(KeyValuePair<string, T>[] array, int arrayIndex)
	{
		if (array == null)
			throw new ArgumentNullException(nameof(array));
		if (arrayIndex < 0)
			throw new ArgumentOutOfRangeException(nameof(arrayIndex));
		if (array.Length - arrayIndex < Count)
			throw new ArgumentException("Destination array is not large enough.");
		foreach (KeyValuePair<string, T> pair in Dictionary)
			array[arrayIndex++] = new KeyValuePair<string, T>(pair.Key, pair.Value);
	}
	public IEnumerator<KeyValuePair<string, T>> GetEnumerator() => Dictionary.GetEnumerator();
	public bool Remove(string key) => Dictionary.Remove(SanitizeKey(key));
	public bool Remove(KeyValuePair<string, T> item)
	{
		string sanitizedKey = SanitizeKey(item.Key);
		if (Dictionary.TryGetValue(sanitizedKey, out T value) &&
			EqualityComparer<T>.Default.Equals(value, item.Value))
			return Dictionary.Remove(sanitizedKey);
		return false;
	}
	public bool TryGetValue(string key, out T value) => Dictionary.TryGetValue(SanitizeKey(key), out value);
	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Dictionary).GetEnumerator();
	#endregion IDictionary
}
