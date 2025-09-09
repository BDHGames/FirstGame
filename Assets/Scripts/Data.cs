﻿// Dictionary Data

using System;
using System.Collections.Generic;
using UnityEngine;

// Word Data

enum FPART : byte
{
	NONE			= 0b00000000,
	NOUN			= 0b00000001,
	VERB			= 0b00000010,
	ADJECTIVE		= 0b00000100,
	ADVERB			= 0b00001000,
	PREPOSITION		= 0b00010000, // include prep_phrase
	PRONOUN			= 0b00100000,
	CONJUNCTION		= 0b01000000,
	OTHER			= 0b10000000, // Not used in-game, only used when parsing to flag words for potential omission
}

// Board Data

public enum CELLK
{
	STANDARD,	// Can be filled
	LOCKED,		// Cannot be filled nor passed through
	VOID,		// Cannot be filled, can be passed through
	// Covered? Let tile fall through, can't be selected in this spot?
}

public enum SETTLEK
{
	[InspectorName(null)] // hides the NIL element in the inspector
	NIL = -1,   // Used for overrides
	[InspectorName("IN PLACE")]
	IN_PLACE = 0,
	FALL,
	RISE,
	[InspectorName("FROM LEFT")]
	FROM_LEFT,
	[InspectorName("FROM RIGHT")]
	FROM_RIGHT,
}

public class BoardLayout // this should probably be a scriptable object or otherwise freely swappable. Also make iterable?
{
	[Min(1)]
	public int _length = 1;

	[Min(1)]
	public int _height = 1;

	[SerializeField]
	private CELLK[,] _layout;

	public BoardLayout(int length, int height)
	{
		_length = length;
		_height = height;

		Debug.Assert(_length > 0 && _height > 0);

		_layout = new CELLK[_length, _height]; // column, row
	}

	public CELLK this[int col, int row]
	{
		get => _layout[col, row];
		set => _layout[col, row] = value; // should only be used if a boss has a board disruption
	}

	public CELLK this[Vector2Int coord]
	{
		get => _layout[coord.x, coord.y];
		set => _layout[coord.x, coord.y] = value; // should only be used if a boss has a board disruption
	}

	public Vector2Int Dims() => new Vector2Int(_length, _height);
	public Vector2Int BottomRight() => Dims() - Vector2Int.one;

	// should Layout have TopRow, BottomRow, LeftCol, and RightCol properties?
}

public class BoardState // the layout _can_ change mid battle due to enemy disruptions.
{
	public readonly BoardLayout		_layout;
	private char[,]					_chars;

	public BoardState(BoardLayout layout)
	{
		_layout = layout;
		_chars = new char[layout._length, layout._height];
	}

	public BoardState Clone()
	{
		BoardState clonedState = new BoardState(_layout);
		clonedState._chars = (char[,])_chars.Clone();
		return clonedState;
	}

	public char this[int col, int row]
	{
		get => _chars[col, row];
		set => _chars[col, row] = value;
	}

	public char this[Vector2Int coord]
	{
		get => _chars[coord.x, coord.y];
		set => _chars[coord.x, coord.y] = value;
	}


	public BoardState CloneSettled(SETTLEK settlek, out BoardDelta delta)
	{
		BoardState clonedState = Clone();
		clonedState.Settle(settlek, out delta);

		return clonedState;
	}

	// Should only call from within CloneSettled(SETTLEK)

	private void Settle(SETTLEK settlek, out BoardDelta delta)
	{
		switch (settlek)
		{
			case SETTLEK.IN_PLACE:
				SettleInPlace(out delta);
				return;

			case SETTLEK.FALL:
			case SETTLEK.RISE:
				SettleVertical(settlek, out delta);
				return;

			default:
				SettleHorizontal(settlek, out delta);
				return;
		}
	}

	private void SettleInPlace(out BoardDelta delta)
	{
		delta = new BoardDelta(_layout);
		BoardConfig config = BoardConfig.INSTANCE;

		for (int col = 0; col < _layout._length; col++)
		{
			for (int row = 0; row < _layout._length; row++)
			{
				if (_layout[col, row] == CELLK.STANDARD)
				{
					if (this[col, row] == ' ')
					{
						this[col, row] = config.Weights.RandomChar();
						delta.AddTile(new Vector2Int(col, row), this[col, row]);
					}
					else
					{
						// no-op delta
						delta[col, row] = new BoardDelta.TileDelta(this[col, row], new Vector2Int(col, row));
					}
				}
			}
		}
	}

	private void SettleVertical(SETTLEK settlek, out BoardDelta delta)
	{
		Debug.Assert(settlek == SETTLEK.FALL || settlek == SETTLEK.RISE);
		delta = new BoardDelta(_layout);
		BoardConfig config = BoardConfig.INSTANCE;

		foreach (int col in new IntIterator(0, _layout._length - 1))
		{
			// check for empty cells

			IntIterator rowIterFall = settlek == SETTLEK.FALL ?
				new IntIterator(_layout._height - 1, 0, -1) :	// bottom to top
				new IntIterator(0, _layout._height - 1, 1);		// top to bottom

			foreach (int row in rowIterFall)
			{
				if (_layout[col, row] != CELLK.STANDARD)
					continue;

				if (this[col, row] != ' ')
				{
					// save the delta
					delta[col, row] = new BoardDelta.TileDelta(this[col, row], new Vector2Int(col, row));
					continue;
				}

				if (row == ((settlek == SETTLEK.FALL) ? 0 : _layout._height - 1))
					continue;

				// check for non-empty cells that can fill the empty cell

				IntIterator rowIterScan = settlek == SETTLEK.FALL ?
					new IntIterator(row - 1, 0, -1) :					// all cells above the current
					new IntIterator(row + 1, _layout._height - 1, 1);	// all cells below the current

				foreach (int rowScan in rowIterScan)
				{
					// if the cell is locked, stop searching

					if (_layout[col, rowScan] == CELLK.LOCKED)
						break;

					// if the cell is void, skip this cell and continue searching

					if (_layout[col, rowScan] == CELLK.VOID)
						continue;

					// if the cell is standard and non-empty, move that to this cell and set that cell to empty

					if (this[col, rowScan] != ' ')
					{
						// store a delta that <col, rowScan> is moving to <col, row>
						delta[col, rowScan] = new BoardDelta.TileDelta(this[col, rowScan], new Vector2Int(col, row));

						this[col, row] = this[col, rowScan];
						this[col, rowScan] = ' ';

						break;
					}
				}
			}

			IntIterator rowIterPopulate = settlek == SETTLEK.FALL ?
				new IntIterator(0, _layout._height - 1, 1) :	// top to bottom
				new IntIterator(_layout._height - 1, 0, -1);	// bottom to top

			foreach (int row in rowIterPopulate)
			{
				// if the cell is locked, then neither this cell nor any after it can have new characters generate

				if (_layout[col, row] == CELLK.LOCKED)
					break;

				// if the cell is void, skip this cell and continue generating new characters

				if (_layout[col, row] == CELLK.VOID)
					continue;

				if (this[col, row] == ' ')
				{
					this[col, row] = config.Weights.RandomChar();
					delta.AddTile(new Vector2Int(col, row), this[col, row]);
				}
			}
		}
	}

	private void SettleHorizontal(SETTLEK settlek, out BoardDelta delta)
	{
		Debug.Assert(settlek == SETTLEK.FROM_LEFT || settlek == SETTLEK.FROM_RIGHT);

		delta = new BoardDelta(_layout);
		BoardConfig config = BoardConfig.INSTANCE;

		foreach (int row in new IntIterator(0, _layout._height - 1))
		{
			IntIterator colIteratorFall = settlek == SETTLEK.FROM_LEFT ?
				new IntIterator(_layout._length - 1, 0, -1) :	// right to left
				new IntIterator(0, _layout._length - 1, 1);	// left to right

			foreach (int col in colIteratorFall)
			{
				if (_layout[col, row] != CELLK.STANDARD)
					continue;

				if (this[col, row] != ' ')
				{
					// save the delta
					delta[col, row] = new BoardDelta.TileDelta(this[col, row], new Vector2Int(col, row));
					continue;
				}

				if (col == ((settlek == SETTLEK.FROM_LEFT) ? 0 : _layout._length - 1))
					continue;

				IntIterator colIteratorScan = settlek == SETTLEK.FROM_LEFT ?
					new IntIterator(col - 1, 0, -1) :					// all cells left of the current
					new IntIterator(col + 1, _layout._height - 1, 1);	// all cells right of the current

				foreach (int colScan in colIteratorScan)
				{
					// if the cell is locked, stop searching

					if (_layout[colScan, row] == CELLK.LOCKED)
						break;

					// if the cell is void, skip this cell and continue searching

					if (_layout[colScan, row] == CELLK.VOID)
						continue;

					// if the cell is standard and non-empty, move that to this cell and set that cell to empty

					if (this[colScan, row] != ' ')
					{
						// store a delta that <colScan, row> is moving to <col, row>
						delta[colScan, row] = new BoardDelta.TileDelta(this[colScan, row], new Vector2Int(col, row));

						this[col, row] = this[colScan, row];
						this[colScan, row] = ' ';
						break;
					}
				}
			}

			IntIterator colIteratorPopulate = settlek == SETTLEK.FROM_LEFT ?
				new IntIterator(0, _layout._length - 1, 1) :	// left to right
				new IntIterator(_layout._length - 1, 0, -1);	// right to left

			foreach (int col in colIteratorPopulate)
			{
				// if the cell is locked, then neither this cell nor any after it can have new characters generate

				if (_layout[col, row] == CELLK.LOCKED)
					break;

				// if the cell is void, skip this cell and continue generating new characters

				if (_layout[col, row] == CELLK.VOID)
					continue;

				if (this[col, row] == ' ')
				{
					this[col, row] = config.Weights.RandomChar();
					delta.AddTile(new Vector2Int(col, row), this[col, row]);
				}
			}
		}
	}
}

/// <summary>
/// Represents the steps required to get from one board state to the next. Used to tell tiles where to go.
/// If _vec2iEnd == <-1, -1> it means the tile in that spot was removed.
/// 
/// Still need to check the board state as that 
/// 
/// Storing _c may be unnecessary, we'll see.
/// </summary>
public class BoardDelta
{
	public struct TileDelta
	{
		public readonly char _char;
		public Vector2Int _destCoord;

		public TileDelta(char c, Vector2Int destCoord)
		{
			_char = c;
			_destCoord = destCoord;
		}

		public bool IsTileDeletion() => _destCoord == new Vector2Int(-1, -1);
	}

	public readonly TileDelta[,] _deltas;

	// We want to avoid duplicates here so a list would be inefficient

	public Dictionary<Vector2Int, char> _newTiles;

	public BoardDelta(BoardLayout layout)
	{
		_deltas = new TileDelta[layout._length, layout._height];

		foreach (Vector2Int startCoord in new Vector2IntIterator(layout.BottomRight()))
		{
			_deltas[startCoord.x, startCoord.y]._destCoord = new Vector2Int(-1, -1);
		}

		_newTiles = new Dictionary<Vector2Int, char>();
	}

	public TileDelta this[int col, int row]
	{
		get => _deltas[col, row];
		set => _deltas[col, row] = value;
	}

	public TileDelta this[Vector2Int coord]
	{
		get => _deltas[coord.x, coord.y];
		set => _deltas[coord.x, coord.y] = value;
	}

	public void AddTile(Vector2Int destCoord, char c)
	{
		Debug.Assert(!_newTiles.ContainsKey(destCoord));

		_newTiles[destCoord] = c;
	}
}

// Tile Data

/// <summary>
/// These are also flags. 0b01 = highlighted, 0b10 = selected
/// </summary>
public enum HIGHLIGHTS
{
	NORMAL,
	HIGHLIGHTED,
	SELECTED,
	SELECTED_AND_HIGHLIGHTED
}