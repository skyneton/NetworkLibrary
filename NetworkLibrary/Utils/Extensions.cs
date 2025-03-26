using System.Collections.Concurrent;

namespace NetworkLibrary.Utils
{
    public static class Extensions
    {
        public static void Remove<T>(this ConcurrentBag<T> bag, T item)
        {
            var queue = new Queue<T>();
            while (!bag.IsEmpty)
            {
                if (bag.TryTake(out var i) && (i is null && item is null || (i?.Equals(item) ?? false)))
                    break;
                queue.Enqueue(i);
            }

            while (queue.Count > 0)
                bag.Add(queue.Dequeue());
        }
        public static bool Remove<T>(this ConcurrentQueue<T> queue, T item)
        {
            var temp = new Queue<T>();
            var contains = false;
            while (!queue.IsEmpty)
            {
                if (queue.TryDequeue(out var i) && (i is null && item is null || (i?.Equals(item) ?? false)))
                {
                    contains = true;
                    continue;
                }
                queue.Enqueue(i);
            }

            while (temp.Count > 0)
                queue.Enqueue(temp.Dequeue());
            return contains;
        }
    }
}
