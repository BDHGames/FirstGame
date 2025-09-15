using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntIterator : IEnumerator<int>, IEnumerable<int>
{
	private readonly int _first;
	private readonly int _final;
	private readonly int _step;

	private int _lastReturned;
	private bool _hasReturned;

	public IntIterator(int first, int final, int step = 1)
	{
		_first = first;
		_lastReturned = first;
		_final = final;
		_step = step;
		Debug.Assert(step != 0 || _first == _final);
		if (_first != _final)
		{
			Debug.Assert((_first < _final) == (_step > 0));
		}
		_hasReturned = false;
	}

	public int Current
	{
		get
		{
			if (_hasReturned)
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

		if (_hasReturned && _first == _final)
			return false;

		if (!_hasReturned)
		{
			_lastReturned = _first;
			_hasReturned = true;
			return true;
		}

		_lastReturned += _step;
		return true;
	}

	public void Reset()
	{
		_hasReturned = false;
	}
}

public class Vector2IntIterator : IEnumerator<Vector2Int>, IEnumerable<Vector2Int>
{
	private readonly Vector2Int _first, _final;

	private Vector2Int _lastReturned;
	private bool _hasReturned;
	private bool _yFirst;

	private Vector2Int _dir;

	/// <summary>
	///
	/// </summary>
	/// <param name="first">Start point for the Vector2Int iterator</param>
	/// <param name="final">Final value of the Vector2Int iterator</param>
	/// <param name="yFirst">Force the iterator to increment Vector2Int.y before it increments Vector2.x</param>
	public Vector2IntIterator(Vector2Int first, Vector2Int final, bool yFirst = false)
	{
		_first = first;
		_lastReturned = first;
		_final = final;
		_yFirst = yFirst;

		_hasReturned = false;

		_dir = new Vector2Int(Math.Sign(final.x - first.x), Math.Sign(final.y - first.y));
	}

	public Vector2IntIterator(Vector2Int final, bool yFirst = false) : this(Vector2Int.zero, final, yFirst) { }

	public Vector2Int Current
	{
		get
		{
			if (_hasReturned)
				return _lastReturned;

			throw new InvalidOperationException("Must call MoveNext before getting Current!");
		}
	}

	object IEnumerator.Current => Current;

	public void Dispose() { }

	public IEnumerator<Vector2Int> GetEnumerator() => this;
	IEnumerator IEnumerable.GetEnumerator() => this;

	public bool MoveNext()
	{
		if (_first == _final)
			return false;

		if (_lastReturned == _final)
			return false;

		if (!_hasReturned)
		{
			_lastReturned = _first;
			_hasReturned = true;
			return true;
		}

		if (_yFirst)
		{
			if (_lastReturned.x == _final.x)
			{
				_lastReturned.y += _dir.y;
				_lastReturned.x = _first.x;
			}
			else
			{
				_lastReturned.x += _dir.x;
			}
		}
		else
		{
			if (_lastReturned.y == _final.y)
			{
				_lastReturned.x += _dir.x;
				_lastReturned.y = _first.y;
			}
			else
			{
				_lastReturned.y += _dir.y;
			}
		}

		return true;
	}

	public void Reset()
	{
		_hasReturned = false;
	}
}
