namespace Molten.Collections;

/// <summary>
/// A non-standard list that provides a ref-based indexer for accessing elements to avoid memory copies. 
/// Internally tracks free indices to avoid internal array resizes and copies where possible.
/// </summary>
/// <typeparam name="T">The type of element to store in the list.</typeparam>
public class ValueFreeRefList<T>
    where T : unmanaged
{
    T[] _items;
    uint[] _free;
    int _freeIndex;
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

    public void Add(ref T item)
    {
        if (_freeIndex > 0)
        {
            uint index = _free[--_freeIndex];
            _items[index] = item;
            return;
        }
        else
        {
            if (_count == _capacity)
            {
                _capacity *= 2;
                Array.Resize(ref _items, (int)_capacity);
            }

            _items[_count++] = item;
        }
    }

    public void Add(T item)
    {
        Add(ref item);
    }

    public void AddRange(IEnumerable<T> values)
    {
        switch (values)
        {
            case List<T> list:
                if(_capacity - _count < list.Count)
                    EnsureCapacity(_capacity + (uint)list.Count);

                list.CopyTo(_items, (int)_count);
                break;

            case T[] array:
                if (_capacity - _count < array.LongLength)
                    EnsureCapacity(_capacity + (uint)array.LongLength);

                array.CopyTo(_items, _count);
                break;

            default:
                foreach (T value in values)
                    Add(value);
                break;
        }
    }

    public unsafe void RemoveAt(uint index)
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
    /// Gets the capacity of the current <see cref="ValueFreeRefList{T}"/>.
    /// </summary>
    public uint Capacity => _capacity;

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
