using System.Collections.Concurrent;

namespace SyncEngine
{
	public struct Change
	{
		public string RelativePath;
		public ChangeType Type;
	}

	public enum ChangeType : byte
	{
		Deleted = 0,
		Created = 1,
		Modified = 2,
		State = 3
	}

	public class ChangesQueue<T>
	{
		private readonly BlockingCollection<T> _queue = new();
		private readonly HashSet<T> _set = new();

		public bool Dequeue(out T? item, CancellationToken cancellationToken)
		{
			var takeResult = _queue.TryTake(out item, -1, cancellationToken);
			if (takeResult)
				_set.Remove(item!);
			return takeResult;
		}

		public void Enqueue(T item)
		{
			if(!_set.Add(item)) return;

			_queue.Add(item);
		}
	}
}
