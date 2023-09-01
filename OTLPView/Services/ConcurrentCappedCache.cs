using System;
using System.Reflection.Metadata.Ecma335;

namespace OTLPView.DataModel;

public sealed class ConcurrentCappedCache<T> : IDisposable, IEnumerable<T>, IReadOnlyList<T>
{
    private const int DEFAULT_MAX_COUNT = 1024;

    private readonly int _maxCount;

    private int _count;
    private int _wrapIndex = 0;
    private T[] _cache;

    public ConcurrentCappedCache(int maxCount)
    {
        if (maxCount < 8) { throw new ArgumentOutOfRangeException(nameof(maxCount), "Must be at least 8"); }
        _maxCount = maxCount;
        _cache = new T[_maxCount];
    }

    public ConcurrentCappedCache() : this(DEFAULT_MAX_COUNT) { }

    public void Append(T item)
    {
        int index;
        lock (this)
        {
            index = _wrapIndex;
            _wrapIndex = (_wrapIndex + 1) % _maxCount;
            if (_count < _maxCount)
            {
                _count++;
            }
        }
        _cache[index] = item;
    }

    public T this[int index]
    {
        get
        {
            int i;
            lock (this)
            {
                i = (_wrapIndex + index) % _maxCount;
            }
            return _cache[i];
        }
        set
        {
            int i;
            lock (this)
            {
                i = (_wrapIndex + index) % _maxCount;
            }
            _cache[i] = value;
        }
    }

    public int Count
    {
        get
        {
            //lock (this)
            //{
            return _count;
            //}
        }
    }

    public bool IsReadOnly => throw new NotImplementedException();

    public void Dispose() => Clear();

    public void Clear()
    {
        lock (this)
        {
            _cache = new T[_maxCount];
            _wrapIndex = 0;
            _count = 0;
        }
    }

    public bool Contains(T item)
    {
        int count;
        lock (this)
        {
            count = _count;
        }

        for (var i = 0; i < count; i++)
        {
            if (this[i] is { } obj && obj.Equals(item))
            {
                return true;
            }
        }
        return false;
    }

    //public int IndexOf(T item)
    //{
    //    int wrapIndex, count, maxCount;

    //    lock (this)
    //    {
    //        wrapIndex = _wrapIndex;
    //        count = _count;
    //        maxCount = _maxCount;
    //    }
    //        for (var i = wrapIndex; i < count + wrapIndex; i++)
    //        {
    //            if (this[i % maxCount] is { } obj && obj.Equals(item))
    //            {
    //                return i % maxCount;
    //            }
    //        }
    //        return -1;
    //}

    public T Oldest() => this[0];

    public T Newest()
    {
        int i;
        if (Count is 0) { return default!; }
        lock (this)
        {
            i = (_wrapIndex + _count - 1) % _maxCount;
        }
        return _cache[i];
    }

    public T[] Oldest(int count)
    {
        var result = new T[count];
        int wrapIndex, totalCount, maxCount;

        lock (this)
        {
            wrapIndex = _wrapIndex;
            totalCount = _count;
            maxCount = _maxCount;
        }

        count = Math.Min(count, totalCount);
        for (var i = 0; i < count; i++) { result[i] = _cache[(wrapIndex + i) % maxCount]; }

        return result;
    }
    public T[] Newest(int count)
    {
        var result = new T[count];
        int wrapIndex, totalCount, maxCount;

        lock (this)
        {
            wrapIndex = _wrapIndex;
            totalCount = _count;
            maxCount = _maxCount;
        }

        count = Math.Min(count, totalCount);

        for (var i = 0; i < count; i++) { result[count - i - 1] = _cache[(wrapIndex + totalCount - count + i) % maxCount]; }

        return result;
    }

    public T[] ToArray()
    {
        lock (this)
        {
            var result = new T[_count];
            Array.Copy(_cache, _wrapIndex, result, 0, _count - _wrapIndex);
            Array.Copy(_cache, 0, result, _count - _wrapIndex, _wrapIndex);
            return result;
        }
    }

    public List<T> ToList()
    {
        lock (this)
        {
            var result = new List<T>(_count);
            result.AddRange(new Span<T>(_cache, _wrapIndex, _count - _wrapIndex));
            result.AddRange(new Span<T>(_cache, 0, _wrapIndex));
            return result;
        }
    }

    public T[] ToReverseArray()
    {
        lock (this)
        {
            var result = new T[_count];
            for (var i = 0; i < _count; i++)
            {
                result[_count - i - 1] = _cache[(_wrapIndex + i) % _maxCount];
            }
            return result;
        }
    }

    public IEnumerator<T> GetEnumerator() => new Enumerator<T>(this);
    public IEnumerable<T> GetReverseEnumerator() => ToReverseArray();

    IEnumerator IEnumerable.GetEnumerator() => ToArray().GetEnumerator();

    private sealed class Enumerator<T> : IEnumerator<T>, IEnumerator
    {
        private T[] _data;
        private int _index = -1;

        internal Enumerator(ConcurrentCappedCache<T> cache)
        {
            _data = cache.ToArray();
        }

        internal Enumerator(T[] cache)
        {
            _data = cache;
        }

        public T Current => (_index < 0 || _index >= _data.Length) ? default! : _data[_index];

        object IEnumerator.Current => Current!;

        public void Dispose()
        {
            _data = null;
        }

        public bool MoveNext()
        {
            _index++;
            return _index < (_data?.Length ?? 0);
        }

        public void Reset() => _index = -1;
    }
}
