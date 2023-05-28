using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace SyncEngine
{
	public struct Change
	{
		public string RelativePath;
		public ChangeType Type;
		public DateTime Time;
	}

	public enum ChangeType : short
	{
		Deleted = 0,
		Created = 1,
		Modified = 2,
		State = 3
	}

	public class ChangesQueue
	{
		private readonly object _lock = new();
		private readonly LinkedList<Change> _queue = new();
		private readonly Dictionary<string, List<LinkedListNode<Change>>> _dictionary = new();

		public Change Dequeue()
		{
			lock (_lock)
			{
				var item = _queue.First();
				_queue.Remove(item);
				if (_dictionary[item.RelativePath].Count == 1)
				{
					_dictionary.Remove(item.RelativePath);
				}
				else
				{
					_dictionary[item.RelativePath].RemoveAt(0);
				}
				return item;
			}
		}

		public void Enqueue(Change change)
		{
			lock (_lock)
			{
				if (_dictionary.ContainsKey(change.RelativePath))
				{
					if(change.Type == ChangeType.Deleted)
					{
						var nodes = _dictionary[change.RelativePath];
						foreach (var node in nodes)
						{
							_queue.Remove(node);
						}
						_dictionary[change.RelativePath].Clear();
					}
				}
				else
				{
					_dictionary.Add(change.RelativePath, new List<LinkedListNode<Change>>());
				}
				var newNode = _queue.AddLast(change);
				_dictionary[change.RelativePath].Add(newNode);
			}
		}

		public bool TryDequeue(out Change change)
		{
			change = default;
			lock ( _lock)
			{
				if (_queue.Count == 0) return false;

				change = Dequeue();
				return true;
			}
		}
	}
}
