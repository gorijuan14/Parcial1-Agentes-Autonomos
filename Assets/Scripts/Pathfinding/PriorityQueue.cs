using System;
using System.Collections.Generic;

public class PriorityQueue<T>
{
    private struct Entry
    {
        public T Item;
        public float Priority;
    }

    private readonly List<Entry> heap = new List<Entry>();
    public int Count => heap.Count;

    public void Enqueue(T item, float priority)
    {
        heap.Add(new Entry { Item = item, Priority = priority });
        int index = heap.Count - 1;
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (heap[parent].Priority <= heap[index].Priority)
                break;

            Swap(parent, index);
            index = parent;
        }
    }

    public T Dequeue()
    {
        if (heap.Count == 0)
            throw new InvalidOperationException("Cannot dequeue an empty priority queue.");

        T result = heap[0].Item;
        int lastIndex = heap.Count - 1;
        heap[0] = heap[lastIndex];
        heap.RemoveAt(lastIndex);

        int index = 0;
        while (index * 2 + 1 < heap.Count)
        {
            int child = index * 2 + 1;
            if (child + 1 < heap.Count && heap[child + 1].Priority < heap[child].Priority)
                child++;

            if (heap[index].Priority <= heap[child].Priority)
                break;

            Swap(index, child);
            index = child;
        }
        return result;
    }

    private void Swap(int a, int b)
    {
        Entry temporary = heap[a];
        heap[a] = heap[b];
        heap[b] = temporary;
    }
}
