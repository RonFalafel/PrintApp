using System.Collections.Concurrent;

namespace WebApp.Core
{
    public class FixedSizedQueue<T> : ConcurrentQueue<T>
    {
        private readonly object _lockObject = new object();
        private readonly int _limit;

        public FixedSizedQueue(int limit = 500)
        {
            _limit = limit;
        }

        public new void Enqueue(T obj) // Using new keyword to override function.
        {
            lock (_lockObject)
            {
                base.Enqueue(obj);
                while (Count > _limit && TryDequeue(out _)) { }
            }
        }
    }
}