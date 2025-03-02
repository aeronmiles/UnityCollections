using System;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

/// <summary>
/// High-performance lock-free ring buffer optimized for Oculus Quest and other mobile VR platforms.
/// Uses a fixed-size array with atomic operations to minimize GC pressure and CPU usage.
/// </summary>
/// <typeparam name="T">The type of elements stored in the buffer</typeparam>
public class LockFreeRingBuffer<T> where T : class
{
  private readonly T[] _buffer;
  private readonly int _capacityMask;

  // Using long for indices to avoid overflow issues in long-running applications
  private long _enqueuePosition;
  private long _dequeuePosition;

  // Separate volatile read variables to minimize thread contention
  private long _cachedDequeuePosition;
  private long _cachedEnqueuePosition;

  /// <summary>
  /// Gets the maximum number of items that can be stored in the buffer
  /// </summary>
  public int Capacity => _buffer.Length;

  /// <summary>
  /// Gets whether the buffer is currently empty
  /// </summary>
  public bool IsEmpty
  {
    get
    {
      // Get volatile reads once to minimize atomic operations
      var enqPos = Volatile.Read(ref _enqueuePosition);
      var deqPos = Volatile.Read(ref _dequeuePosition);
      return enqPos == deqPos;
    }
  }

  /// <summary>
  /// Gets the approximate number of items currently in the buffer.
  /// This is approximate due to concurrent operations.
  /// </summary>
  public int Count
  {
    get
    {
      // Get volatile reads once to minimize atomic operations
      var enqPos = Volatile.Read(ref _enqueuePosition);
      var deqPos = Volatile.Read(ref _dequeuePosition);
      return (int)Math.Min(_buffer.Length, enqPos - deqPos);
    }
  }

  /// <summary>
  /// Creates a new lock-free ring buffer with the specified capacity.
  /// The actual capacity will be rounded up to the next power of two.
  /// </summary>
  /// <param name="capacity">Minimum desired capacity. Must be positive.</param>
  public LockFreeRingBuffer(int capacity)
  {
    if (capacity <= 0)
      throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");

    // Round up to power of 2 for efficient masking
    int actualCapacity = RoundUpToPowerOfTwo(capacity);

    _buffer = new T[actualCapacity];
    _capacityMask = actualCapacity - 1;

    _enqueuePosition = 0;
    _dequeuePosition = 0;
    _cachedDequeuePosition = 0;
    _cachedEnqueuePosition = 0;
  }

  /// <summary>
  /// Attempts to add an item to the buffer.
  /// </summary>
  /// <param name="item">Item to add. Must not be null.</param>
  /// <param name="dropOldestWhenFull">
  /// If true, automatically drops the oldest item when the buffer is full.
  /// If false, returns false when the buffer is full without adding the item.
  /// </param>
  /// <returns>True if the item was added, false otherwise.</returns>
  public bool TryEnqueue(T item, bool dropOldestWhenFull = false)
  {
    if (item == null)
      throw new ArgumentNullException(nameof(item));

    // Refresh cache occasionally to reduce stale reads
    if (_enqueuePosition - _cachedDequeuePosition >= _buffer.Length)
    {
      _cachedDequeuePosition = Volatile.Read(ref _dequeuePosition);

      // If still full after refreshing cache
      if (_enqueuePosition - _cachedDequeuePosition >= _buffer.Length)
      {
        if (!dropOldestWhenFull)
          return false; // Apply back-pressure instead of dropping

        // Advance dequeue position to make room (drop oldest item)
        // Note: We might "race" with another thread here, but it's acceptable
        // as it just means another thread already made room for us
        Interlocked.CompareExchange(
            ref _dequeuePosition,
            _cachedDequeuePosition + 1,
            _cachedDequeuePosition);

        // Refresh after attempting to drop
        _cachedDequeuePosition = Volatile.Read(ref _dequeuePosition);
      }
    }

    // Reserve a slot by incrementing the enqueue position
    long position = Interlocked.Increment(ref _enqueuePosition) - 1;
    int index = (int)(position & _capacityMask);

    // Store the item
    Volatile.Write(ref _buffer[index], item);

    return true;
  }

  /// <summary>
  /// Attempts to remove and return an item from the buffer.
  /// </summary>
  /// <param name="item">The removed item, or null if the buffer was empty.</param>
  /// <returns>True if an item was removed, false if the buffer was empty.</returns>
  public bool TryDequeue([MaybeNullWhen(false)] out T item)
  {
    // Refresh cache occasionally to reduce stale reads
    if (_cachedEnqueuePosition <= _dequeuePosition)
    {
      _cachedEnqueuePosition = Volatile.Read(ref _enqueuePosition);
      if (_cachedEnqueuePosition <= _dequeuePosition)
      {
        item = default;
        return false;
      }
    }

    // Reserve a slot for dequeuing by incrementing the dequeue position
    long position = Interlocked.Increment(ref _dequeuePosition) - 1;
    int index = (int)(position & _capacityMask);

    // Get the item and clear the slot (important for GC if T is a reference type)
    item = Interlocked.Exchange(ref _buffer[index], null);

    // If we got null, it means we hit a race condition with another thread
    // This shouldn't happen in a correct implementation, but we handle it gracefully
    if (item == null)
    {
      // Try again by recursively calling ourselves
      // This is rare, so the recursive call is acceptable
      return TryDequeue(out item);
    }

    return true;
  }

  /// <summary>
  /// Attempts to remove multiple items from the buffer at once.
  /// </summary>
  /// <param name="items">Array to store dequeued items</param>
  /// <param name="startIndex">Start index in the array to begin storing items</param>
  /// <param name="count">Maximum number of items to dequeue</param>
  /// <returns>The number of items actually dequeued</returns>
  public int TryDequeueBatch(T[] items, int startIndex, int count)
  {
    if (items == null)
      throw new ArgumentNullException(nameof(items));

    if (startIndex < 0 || startIndex >= items.Length)
      throw new ArgumentOutOfRangeException(nameof(startIndex));

    if (count <= 0 || startIndex + count > items.Length)
      throw new ArgumentOutOfRangeException(nameof(count));

    if (IsEmpty)
      return 0;

    int dequeued = 0;

    // Optimize for the common case with a batch reservation
    long currentDequeue = Volatile.Read(ref _dequeuePosition);
    long currentEnqueue = Volatile.Read(ref _enqueuePosition);
    long available = currentEnqueue - currentDequeue;

    if (available <= 0)
      return 0;

    // Don't try to dequeue more than is available or requested
    int toDequeue = (int)Math.Min(available, count);

    // Try to reserve toDequeue items at once
    long newDequeuePosition = currentDequeue + toDequeue;
    if (Interlocked.CompareExchange(ref _dequeuePosition, newDequeuePosition, currentDequeue) != currentDequeue)
    {
      // If batch reservation failed, fall back to one-by-one dequeuing
      for (int i = 0; i < count; i++)
      {
        if (!TryDequeue(out var item))
          break;

        items[startIndex + i] = item;
        dequeued++;
      }

      return dequeued;
    }

    // Batch reservation succeeded, now retrieve the items
    for (int i = 0; i < toDequeue; i++)
    {
      int index = (int)((currentDequeue + i) & _capacityMask);
      items[startIndex + i] = Interlocked.Exchange(ref _buffer[index], null);
      dequeued++;
    }

    return dequeued;
  }

  /// <summary>
  /// Clears all items from the buffer.
  /// Note: This is not an atomic operation and should not be used while other threads
  /// are accessing the buffer.
  /// </summary>
  public void Clear()
  {
    // Clear all buffer slots
    for (int i = 0; i < _buffer.Length; i++)
    {
      _buffer[i] = null;
    }

    // Reset positions
    Volatile.Write(ref _dequeuePosition, 0);
    Volatile.Write(ref _enqueuePosition, 0);
    _cachedDequeuePosition = 0;
    _cachedEnqueuePosition = 0;
  }

  /// <summary>
  /// Rounds up a number to the next power of two.
  /// </summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static int RoundUpToPowerOfTwo(int v)
  {
    v--;
    v |= v >> 1;
    v |= v >> 2;
    v |= v >> 4;
    v |= v >> 8;
    v |= v >> 16;
    v++;
    return v;
  }
}