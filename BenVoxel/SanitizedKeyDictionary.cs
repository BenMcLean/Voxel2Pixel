using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace BenVoxel;

/// <summary>
/// Restricts keys to max length 255 with last-in-wins behavior.
/// </summary>
public sealed class SanitizedKeyDictionary<T>() : IDictionary<string, T>
{
	private readonly Dictionary<string, T> _dictionary = [];
	[JsonInclude]
	public ReadOnlyDictionary<string, T> ReadOnlyDictionary
	{
		get => new(this);
		set
		{
			_dictionary.Clear();
			if (value is null) return;
			foreach (KeyValuePair<string, T> pair in value)
				Add(pair.Key, pair.Value);
		}
	}
	public static string SanitizeKey(string key)
	{
		key = key.Trim();
		return key[..Math.Min(key.Length, 255)];
	}
	#region IDictionary
	public T this[string key]
	{
		get => _dictionary[SanitizeKey(key)];
		set => _dictionary[SanitizeKey(key)] = value;
	}
	public ICollection<string> Keys => _dictionary.Keys;
	public ICollection<T> Values => _dictionary.Values;
	public int Count => _dictionary.Count;
	public bool IsReadOnly => ((ICollection<KeyValuePair<string, T>>)_dictionary).IsReadOnly;
	public void Add(string key, T value) => this[key] = value;
	public void Add(KeyValuePair<string, T> item) => this[item.Key] = item.Value;
	public void Clear() => _dictionary.Clear();
	public bool Contains(KeyValuePair<string, T> item) =>
		_dictionary.TryGetValue(SanitizeKey(item.Key), out T value)
			&& EqualityComparer<T>.Default.Equals(value, item.Value);
	public bool ContainsKey(string key) => _dictionary.ContainsKey(SanitizeKey(key));
	public void CopyTo(KeyValuePair<string, T>[] array, int arrayIndex)
	{
		if (array == null)
			throw new ArgumentNullException(nameof(array));
		if (arrayIndex < 0)
			throw new ArgumentOutOfRangeException(nameof(arrayIndex));
		if (array.Length - arrayIndex < Count)
			throw new ArgumentException("Destination array is not large enough.");
		foreach (KeyValuePair<string, T> pair in _dictionary)
			array[arrayIndex++] = new KeyValuePair<string, T>(pair.Key, pair.Value);
	}
	public IEnumerator<KeyValuePair<string, T>> GetEnumerator() => _dictionary.GetEnumerator();
	public bool Remove(string key) => _dictionary.Remove(SanitizeKey(key));
	public bool Remove(KeyValuePair<string, T> item)
	{
		string sanitizedKey = SanitizeKey(item.Key);
		if (_dictionary.TryGetValue(sanitizedKey, out T value) &&
			EqualityComparer<T>.Default.Equals(value, item.Value))
			return _dictionary.Remove(sanitizedKey);
		return false;
	}
	public bool TryGetValue(string key, out T value) => _dictionary.TryGetValue(SanitizeKey(key), out value);
	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_dictionary).GetEnumerator();
	#endregion IDictionary
}
