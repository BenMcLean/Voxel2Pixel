using System.Collections;
using System.Collections.Generic;

namespace Voxel2Pixel.Model.BenVoxel
{
	public class LimitedKeyLengthDictionary<T> : IDictionary<string, T>
	{
		protected Dictionary<string, T> Dictionary = [];
		public T this[string key]
		{
			get => ((IDictionary<string, T>)Dictionary)[key];
			set
			{
				((IDictionary<string, T>)Dictionary)[key] = value;
			}
		}
		public ICollection<string> Keys => ((IDictionary<string, T>)Dictionary).Keys;
		public ICollection<T> Values => ((IDictionary<string, T>)Dictionary).Values;
		public int Count => ((ICollection<KeyValuePair<string, T>>)Dictionary).Count;
		public bool IsReadOnly => ((ICollection<KeyValuePair<string, T>>)Dictionary).IsReadOnly;
		public void Add(string key, T value)
		{
			((IDictionary<string, T>)Dictionary).Add(key, value);
		}
		public void Add(KeyValuePair<string, T> item)
		{
			((ICollection<KeyValuePair<string, T>>)Dictionary).Add(item);
		}
		public void Clear()
		{
			((ICollection<KeyValuePair<string, T>>)Dictionary).Clear();
		}
		public bool Contains(KeyValuePair<string, T> item)
		{
			return ((ICollection<KeyValuePair<string, T>>)Dictionary).Contains(item);
		}
		public bool ContainsKey(string key)
		{
			return ((IDictionary<string, T>)Dictionary).ContainsKey(key);
		}
		public void CopyTo(KeyValuePair<string, T>[] array, int arrayIndex)
		{
			((ICollection<KeyValuePair<string, T>>)Dictionary).CopyTo(array, arrayIndex);
		}
		public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
		{
			return ((IEnumerable<KeyValuePair<string, T>>)Dictionary).GetEnumerator();
		}
		public bool Remove(string key)
		{
			return ((IDictionary<string, T>)Dictionary).Remove(key);
		}
		public bool Remove(KeyValuePair<string, T> item)
		{
			return ((ICollection<KeyValuePair<string, T>>)Dictionary).Remove(item);
		}
		public bool TryGetValue(string key, out T value)
		{
			return ((IDictionary<string, T>)Dictionary).TryGetValue(key, out value);
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)Dictionary).GetEnumerator();
		}
	}
}
