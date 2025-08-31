using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

public class IntIterator : IEnumerator<int>, IEnumerable<int>
{
    private readonly int _first;
    private readonly int _final;
    private readonly int _step;

    private int _lastReturned;
    private bool _fHasReturned;

    public IntIterator(int first, int final, int step = 1)
    {
        _first = first;
        _lastReturned = first;
        _final = final;
        _step = step;
        Debug.Assert((_first < _final) == (_step > 0));
        Debug.Assert(step != 0 && _first != _final);
        _fHasReturned = false;
    }

    public int Current
    {
        get
        {
            if (_fHasReturned)
                return _lastReturned;

            throw new InvalidOperationException("Must call MoveNext before getting Current!");
        }
    }

    object IEnumerator.Current => Current;

    public void Dispose() { }

    public IEnumerator<int> GetEnumerator() => this;
    IEnumerator IEnumerable.GetEnumerator() => this;

    public bool MoveNext()
    {
        if (_first < _final)
        {
            if (_lastReturned + _step > _final)
                return false;
        }

        if (_first > _final)
        {
            if (_lastReturned + _step < _final)
                return false;
        }

        if (!_fHasReturned)
        {
            _lastReturned = _first;
            _fHasReturned = true;
            return true;
        }

        _lastReturned += _step;
        return true;
    }

    public void Reset()
    {
        _fHasReturned = false;
    }
}
