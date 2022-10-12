using System;
using System.Collections;
using Unity.Collections;

[Serializable]
<<<<<<< HEAD
public struct NativeStack<T> : IDisposable where T : struct
=======
public struct NativeStack<T> : IDisposable, IEnumerable, IEnumerator, IEquatable<NativeStack<T>> where T : struct
>>>>>>> 9c31c7c3bd33c3d67b5c9a1a3d71d3be10f4427e
{
    private NativeArray<T> array;
    private int currIndex;
    public object Current => array[currIndex];
    public int Length => currIndex;

    public NativeStack(int length, Allocator allocator)
    {
        array = new NativeArray<T>(length, allocator);
        currIndex = 0;
    }

    public T Pop()
    {
        return array[--currIndex];
    }

    public void Push(T item)
    {
        array[currIndex++] = item;
    }

    public void Dispose()
    {
        array.Dispose();
    }

<<<<<<< HEAD
    public override int GetHashCode()
    {
        return array.GetHashCode();
=======
    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }

    public bool Equals(NativeStack<T> other)
    {
        return Equals(other);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public static bool operator ==(NativeStack<T> left, NativeStack<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(NativeStack<T> left, NativeStack<T> right)
    {
        return !left.Equals(right);
    }

    public IEnumerator GetEnumerator()
    {
        return (IEnumerator)this;
    }

    public bool MoveNext()
    {
        return currIndex > 0;
    }

    // any changes to array should update currentIndex
    public void Reset()
    {
>>>>>>> 9c31c7c3bd33c3d67b5c9a1a3d71d3be10f4427e
    }
}
