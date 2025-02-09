using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

public class LockFreeRingBuffer<T>
{
  private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
  private readonly int _capacity;

  public LockFreeRingBuffer(int capacity)
  {
    if (capacity <= 0)
    {
      throw new System.ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
    }
    _capacity = capacity;
  }

  public bool TryEnqueue(T item)
  {
    // Spin and remove elements until size is less than capacity.
    while (_queue.Count >= _capacity)
    {
      if (_queue.TryDequeue(out _)) continue;

      //If we are here, it means a different thread has already dequeued.
      //This is fine. Just loop again to make sure we are under the capacity.

    }
    _queue.Enqueue(item);
    return true;
  }

  public bool TryDequeue([MaybeNullWhen(false)] out T item)
  {
    return _queue.TryDequeue(out item);
  }
}