using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomCollections
{
	public class QueueDictionary<TKey, TValue> where TKey : notnull
	{
		private readonly object _lock = new();
		private readonly LinkedList<Tuple<TKey, TValue>> _queue = new();
		private readonly Dictionary<TKey, LinkedListNode<Tuple<TKey, TValue>>> _dictionary = new();

		public TValue Dequeue()
		{
			lock (_lock)
			{
				if (_queue.Count != 0)
				{
					var item = _queue.First();
					_queue.RemoveFirst();
					_dictionary.Remove(item.Item1);
					return item.Item2;
				}

				return default!;
			}
		}

		public TValue Dequeue(TKey key)
		{
			lock (_lock)
			{
				var node = _dictionary[key];
				_dictionary.Remove(key);
				_queue.Remove(node);
				return node.Value.Item2;
			}
		}

		public void Enqueue(TKey key, TValue value)
		{
			lock (_lock)
			{
				LinkedListNode<Tuple<TKey, TValue>> node =
					_queue.AddLast(new Tuple<TKey, TValue>(key, value));
				_dictionary.Add(key, node);
			}
		}

		public bool TryDequeue(out TValue data)
		{
			data = default!;
			lock (_lock)
			{
				if (_queue.Count == 0)
					return false;

				data = Dequeue()!;
				return true;
			}
		}

		public bool TryEnqueue(TKey key, TValue value)
		{
			lock(_lock)
			{
				if(_dictionary.ContainsKey(key))
					return false;
				
				Enqueue(key, value);
				return true;
			}
		}
	}
}
