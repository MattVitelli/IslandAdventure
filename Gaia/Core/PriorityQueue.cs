using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gaia.Core
{
    public class PriorityQueue<P, T>
    {
        SortedDictionary<P, Queue<T>> dictionary = new SortedDictionary<P, Queue<T>>();
        int logSize = 0;

        public int Count
        {
            get { return logSize; }
        }

        public void Clear()
        {
            dictionary.Clear();
            logSize = 0;
        }

        public void Enqueue(T element, P priority)
        {
            logSize++;
            if (!dictionary.ContainsKey(priority))
            {
                dictionary.Add(priority, new Queue<T>());
            }
            dictionary[priority].Enqueue(element);
        }

        public T ExtractMin()
        {
            logSize--;
            var smallest = dictionary.First();
            T element = smallest.Value.Dequeue();
            if (smallest.Value.Count == 0)
                dictionary.Remove(smallest.Key);
            return element;
        }

        public T Peek()
        {
            var smallest = dictionary.First();
            return smallest.Value.Peek();
        }

        public T ExtractMax()
        {
            logSize--;
            var largest = dictionary.Last();
            T element = largest.Value.Dequeue();
            if (largest.Value.Count == 0)
                dictionary.Remove(largest.Key);
            return element;
        }

        //This is a really bad way to do this...
        public void RemoveAt(P priority, T element)
        {
            if(dictionary.ContainsKey(priority))
            {
                int dictCount = dictionary[priority].Count;
                for (int i = 0; i < dictCount; i++)
                {
                    T elem = dictionary[priority].Dequeue();
                    if (!elem.Equals(element))
                        dictionary[priority].Enqueue(elem);
                }
            }
        }
    }
}
