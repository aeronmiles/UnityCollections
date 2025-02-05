using System;
using System.Threading;

public class LockFreeRingBuffer<T>
{
    private readonly T[] buffer;
    private int head;
    private int tail;
    private readonly int capacity;

    public LockFreeRingBuffer(int capacity)
    {
        this.capacity = capacity;
        buffer = new T[capacity];
        head = 0;
        tail = 0;
    }

    public bool TryEnqueue(T item)
    {
        int currentTail = tail;
        int nextTail = (currentTail + 1) % capacity;

        if (nextTail == head)
        {
          
            // Buffer is full
            return false;
        }

        buffer[currentTail] = item;
        Interlocked.Exchange(ref tail, nextTail);
        return true;
    }

    public bool TryDequeue(out T item)
    {
        int currentHead = head;

        if (currentHead == tail)
        {
            // Buffer is empty
            item = default(T);
            return false;
        }

        item = buffer[currentHead];
        Interlocked.Exchange(ref head, (currentHead + 1) % capacity);
        return true;
    }
}