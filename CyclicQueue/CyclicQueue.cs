using System.Collections.Generic;
using System.Linq;

namespace SleepHerd.DataStructures
{
    public class CyclicQueue<T>
    {
        private readonly LinkedList<T> _cyclicQueue = new();

        public T First => _cyclicQueue.Count > 0 ? _cyclicQueue.First.Value : default;
        public T Last => _cyclicQueue.Count > 0 ? _cyclicQueue.Last.Value : default;
        public int Count => _cyclicQueue.Count;
        
        public T CycleQueue()
        {
            if (_cyclicQueue.Count == 0)
            {
                return default;
            }

            var nextItem = _cyclicQueue.First.Value;
            _cyclicQueue.RemoveFirst();
            _cyclicQueue.AddLast(nextItem);
            return nextItem;
        }

        public void Remove(T item)
        {
            _cyclicQueue.Remove(item);
        }

        public void RemoveOldFromQueue(IEnumerable<T> itemList)
        {
            var oldItems = _cyclicQueue.Except(itemList).ToList();

            foreach (var item in oldItems)
            {
                _cyclicQueue.Remove(item);
            }
        }

        public void Add(T item)
        {
            _cyclicQueue.AddLast(item);
        }

        public void AddNewToQueue(IEnumerable<T> itemList)
        {
            var newItems = itemList.Except(_cyclicQueue);

            foreach (var item in newItems)
            {
                _cyclicQueue.AddLast(item);
            }
        }

        public void Clear()
        {
            _cyclicQueue.Clear();
        }
    }
}
