namespace Molten.Collections;

/// <summary>
/// A non-standard list that provides a ref-based indexer for accessing elements to avoid memory copies. 
/// Internally tracks free indices to avoid internal array resizes and copies where possible.
/// </summary>
/// <typeparam name="T">The type of element to store in the list.</typeparam>
public class ValueFreeRefList<T>
    where T : struct
{
    T[] _items;
    uint[] _free;
    uint _freeIndex;
    uint _capacity;
    uint _count;

    /// <summary>
    /// Creates a new instance of <see cref="ValueFreeRefList{T}"/>.
    /// </summary>
    /// <param name="initialCapacity"></param>
    public ValueFreeRefList(uint initialCapacity = 0)
    {
        _capacity = initialCapacity > 0 ? initialCapacity : 4;
        Clear(_capacity);
    }

    /// <summary>
    /// Ensures that the current <see cref="ValueFreeRefList{T}"/> has at least the specified capacity.
    /// </summary>
    /// <param name="capacity">The capacity to ensure.</param>
    public void EnsureCapacity(uint capacity)
    {
        if (capacity > _capacity)
        {
            _capacity = capacity;
            Array.Resize(ref _items, (int)capacity);
        }
    }

    /// <summary>
    /// Adds the provided value to the list and returns the index it was added at.
    /// </summary>
    /// <param name="item">The item to be added.</param>
    /// <returns>The index of the item within the list.</returns>
    public uint Add(ref T item)
    {
        uint index = 0;

        if (_freeIndex > 0)
        {
            index = _free[--_freeIndex];
        }
        else
        {
            if (_count == _capacity)
            {
                _capacity *= 2;
                Array.Resize(ref _items, (int)_capacity);
            }

            index = _count++;
        }

        _items[index] = item;
        return index;
    }

    /// <summary>
    /// Adds the provided value to the list and returns the index it was added at.
    /// </summary>
    /// <param name="item">The item to be added.</param>
    /// <returns>The index of the item within the list.</returns>
    public uint Add(T item)
    {
        return Add(ref item);
    }

    /// <summary>
    /// Adds the provided items to the list and returns the start index they were added at.
    /// </summary>
    /// <param name="items">The items to be added.</param>
    /// <returns>The start index of the range within the list.</returns>
    public uint AddRange(List<T> items)
    {
        uint count = (uint)items.Count;
        if (_capacity - _count < items.Count)
            EnsureCapacity(_capacity + count);

        uint startIndex = _count;
        items.CopyTo(_items, (int)startIndex);
        _count += count;

        return startIndex;
    }

    /// <summary>
    /// Adds the provided items to the list and returns the start index they were added at.
    /// </summary>
    /// <param name="items">The items to be added.</param>
    /// <returns>The start index of the range within the list.</returns>
    public uint AddRange(T[] items)
    {
        uint count = (uint)items.LongLength;
        if (_capacity - _count < items.LongLength)
            EnsureCapacity(_capacity + (uint)items.LongLength);

        uint startIndex = _count;
        Array.Copy(items, 0, _items, startIndex, items.Length);
        _count += count;

        return startIndex;
    }

    /// <summary>
    /// Removes an item at the specified index and adds it to an internal free stack.
    /// </summary>
    /// <param name="index"></param>
    /// <exception cref="IndexOutOfRangeException"></exception>
    public void RemoveAt(uint index)
    {
        if (index >= _count)
            throw new IndexOutOfRangeException();

        if (index == _count - 1)
        {
            _items[index] = default;
            _count--;
        }
        else
        {
            if (_freeIndex == _free.Length)
                Array.Resize(ref _free, _free.Length * 2);

            _free[_freeIndex++] = index;
        }
    }

    public void RemoveRange(uint startIndex, uint count)
    {
        uint end = startIndex + count;

        if (startIndex >= _count || end > _count)
            throw new IndexOutOfRangeException();

        if(end == _count)
        {
            for(uint i = startIndex; i < count; i++)
                _items[i] = default;

            _count -= count;
        }
        else
        {
            if(_freeIndex == _free.Length)
                Array.Resize(ref _free, (int)Math.Max(_free.Length * 2, _free.Length + count));

            for(uint i = startIndex; i < end; i++)
                _free[_freeIndex++] = i;
        }
    }

    /// <summary>
    /// Clears the current <see cref="ValueFreeRefList{T}"/>.
    /// </summary>
    /// <param name="newCapacity">If greater than zero, the current <see cref="ValueFreeRefList{T}"/> will be reset to the specified capacity.</param>
    public void Clear(uint newCapacity = 0)
    {
        for(uint i = 0; i < _count; i++)
            _items[i] = default;

        _count = 0;
        _freeIndex = 0;

        if(newCapacity > 0)
        {
            _capacity = newCapacity;
            _items = new T[_capacity];
            _free = new uint[Math.Max(4, _capacity / 4)];
        }
    }

    /// <summary>
    /// Gets the number of items in the list, including freed indices that may be inbetween.
    /// </summary>
    public uint Count => _count;

    /// <summary>
    /// Gets the number of free indices in the list.
    /// </summary>
    public uint FreeCount => _freeIndex;

    /// <summary>
    /// Gets the capacity of the current <see cref="ValueFreeRefList{T}"/>.
    /// </summary>
    public uint Capacity => _capacity;

    /// <summary>
    /// Gets the underlying array of items. Note: directly manipulating this array may cause unexpected behaviour.
    /// </summary>
    public T[] Items => _items;

    /// <summary>
    /// Gets or sets an item at the specified index by reference.
    /// </summary>
    /// <param name="index">The index of the item to be set or retrieved.</param>
    /// <returns></returns>
    public ref T this[uint index] => ref _items[index];

    /// <summary>
    /// Gets or sets an item at the specified index by reference.
    /// </summary>
    /// <param name="index">The index of the item to be set or retrieved.</param>
    /// <returns></returns>
    public ref T this[int index] => ref _items[index];
}
