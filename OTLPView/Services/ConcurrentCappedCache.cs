namespace OTLPView.Services;

public sealed class ConcurrentCappedCache<T> : IDisposable, IEnumerable<T>
{
    private const int DEFAULT_MAX_COUNT = 1024;

    private readonly int _maxCount;

    private int _count;
    private int _wrapIndex = 0;
    private T[] _cache;

    public ConcurrentCappedCache(int maxCount)
    {
        _maxCount = maxCount;
        _cache = new T[_maxCount];
    }

    public ConcurrentCappedCache() : this(DEFAULT_MAX_COUNT) { }

    public void Add(T item)
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
                i = (_wrapIndex + index) % _count;
            }
            return _cache[i];
        }
        set
        {
            int i;
            lock (this)
            {
                i = (_wrapIndex + index) % _count;
            }
            _cache[i] = value;
        }
    }

    public int Count
    {
        get
        {
            lock (this)
            {
                return _count;
            }
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
        lock (this)
        {
            for (var i = 0; i < _count; i++)
            {
                if (this[i] is { } obj && obj.Equals(item))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public int IndexOf(T item)
    {
        lock (this)
        {
            for (var i = _wrapIndex; i < _count + _wrapIndex; i++)
            {
                if (this[i % _maxCount] is { } obj && obj.Equals(item))
                {
                    return i % _maxCount;
                }
            }
            return -1;
        }
    }

    public T First() => this[0];

    public T Last()
    {
        int i;
        lock (this)
        {
            if (Count is 0) { return default!; }
            i = (_wrapIndex + _count - 1) % _maxCount;
        }
        return _cache[i];
    }

    public T[] First(int count)
    {
        var result = new T[count];
        lock (this)
        {
            count = Math.Min(count, _count);

            for (var i = 0; i < count; i++) { result[i] = _cache[(_wrapIndex + i) % _maxCount]; }
        }
        return result;
    }
    public T[] Last(int count)
    {
        var result = new T[count];
        lock (this)
        {
            count = Math.Min(count, _count);

            for (var i = 0; i < count; i++) { result[i] = _cache[(_wrapIndex + _count - count + i) % _maxCount]; }
        }
        return result;
    }

    public IEnumerator<T> GetEnumerator() => new Enumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

    private sealed class Enumerator : IEnumerator<T>
    {
        private ConcurrentCappedCache<T> _cache;
        private int _index = -1;

        internal Enumerator(ConcurrentCappedCache<T> cache)
        {
            _cache = cache;
        }

        public T Current => _cache[_index];

        object IEnumerator.Current => Current!;

        public void Dispose()
        {
            _cache?.Dispose();
            _cache = null!;
        }

        public bool MoveNext()
        {
            _index++;
            return _index < (_cache?.Count ?? 0);
        }

        public void Reset() => _index = -1;
    }
}
