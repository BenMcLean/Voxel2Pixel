using System;
using System.Collections;
using System.Collections.Generic;
using System.Web;

namespace Voxel2Pixel.Model.BenVoxel
{
	public sealed class SanitizedKeyDictionary<T> : IDictionary<string, T>
	{
		private readonly Dictionary<string, T> Dictionary = [];
		public static string SanitizeKey(string key)
		{
			key = HttpUtility.UrlEncode(key.Trim());
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
		public void Add(string key, T value) => Dictionary.Add(SanitizeKey(key), value);
		public void Add(KeyValuePair<string, T> item) => Add(item.Key, item.Value);
		public void Clear() => Dictionary.Clear();
		public bool Contains(KeyValuePair<string, T> item) => ((ICollection<KeyValuePair<string, T>>)Dictionary).Contains(new(SanitizeKey(item.Key), item.Value));
		public bool ContainsKey(string key) => Dictionary.ContainsKey(SanitizeKey(key));
		public void CopyTo(KeyValuePair<string, T>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, T>>)Dictionary).CopyTo(array, arrayIndex);
		public IEnumerator<KeyValuePair<string, T>> GetEnumerator() => Dictionary.GetEnumerator();
		public bool Remove(string key) => Dictionary.Remove(SanitizeKey(key));
		public bool Remove(KeyValuePair<string, T> item) => ((ICollection<KeyValuePair<string, T>>)Dictionary).Remove(new(SanitizeKey(item.Key), item.Value));
		public bool TryGetValue(string key, out T value) => Dictionary.TryGetValue(SanitizeKey(key), out value);
		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Dictionary).GetEnumerator();
		#endregion IDictionary
	}
}
