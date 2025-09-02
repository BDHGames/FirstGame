// Dictionary Data

using System;
using System.Collections.Generic;
using UnityEngine;

// Word Data

enum FPART : short
{
    NONE            = 0b000000000,
    NOUN            = 0b000000001,
    VERB            = 0b000000010,
    ADJECTIVE       = 0b000000100,
    ADVERB          = 0b000001000,
    PRONOUN         = 0b000010000,
    PREPOSITION     = 0b000100000,
    CONJUNCTION     = 0b001000000,
    INTERJECTION    = 0b010000000,
    ARTICLE         = 0b100000000,
}

struct SWord
{
    string  _str;   // the string itself
    FPART   _fpart; // parts of speech

    bool FHasPartOfSpeech(FPART fpart) => (_fpart & fpart) != FPART.NONE;
}



// Board Data

public enum CELLK
{
    STANDARD,   // Can be filled
    LOCKED,     // Cannot be filled nor passed through
    VOID,       // Cannot be filled, can be passed through
}

public enum SETTLEK
{
    IN_PLACE = 0,
    FALL,
    RISE,
    FROM_LEFT,
    FROM_RIGHT,
}

public class CBoardLayout // this should probably be a scriptable object or otherwise freely swappable
{
    [Min(1)]
    public int _length = 1;

    [Min(1)]
    public int _height = 1;

    [SerializeField]
    private CELLK[,] _layout;

    public CBoardLayout(int length, int height)
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

    public Vector2Int Vec2IDim() => new Vector2Int(_length, _height);
    public Vector2Int Vec2IDimMinusOne() => Vec2IDim() - Vector2Int.one;
}

public class CBoardState // tag = bstate
{
    public readonly CBoardLayout    _blayout;
    private char[,]                 _chars;

    public CBoardState(CBoardLayout blayout)
    {
        _blayout = blayout;
        _chars = new char[blayout._length, blayout._height];
    }

    public CBoardState Clone()
    {
        CBoardState bstateNew = new CBoardState(_blayout);
        bstateNew._chars = (char[,])_chars.Clone();
        return bstateNew;
    }

    public char this[int col, int row]
    {
        get => _chars[col, row];
        set => _chars[col, row] = value;
    }

    public CBoardState CloneSettled(SETTLEK settlek, out SBoardDelta delta)
    {
        CBoardState bStateNew = Clone();
        bStateNew.Settle(settlek, out delta);

        return bStateNew;
    }

    // Should only call after Clone or from within CloneSettled(SETTLEK). Not sure which we'll use
    public void Settle(SETTLEK settlek, out SBoardDelta delta)
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

    // all of these functions would be cleaner if we had pre-built iterators. Just select the iterator at the start of the function based on SETTLEK

    private void SettleInPlace(out SBoardDelta delta)
    {
        delta = new SBoardDelta(this);

        for (int col = 0; col < _blayout._length; col++)
        {
            for (int row = 0; row < _blayout._length; row++)
            {
                if (_blayout[col, row] == CELLK.STANDARD)
                {
                    if (this[col, row] == ' ')
                    {
                        this[col, row] = RandomChar();
                        delta.CreateTile(new Vector2Int(col, row), this[col, row]);
                    }
                    else
                    {
                        // no-op delta
                        delta[col, row] = new SBoardDelta.STileDelta(this[col, row], new Vector2Int(col, row));
                    }
                }
            }
        }
    }

    private void SettleVertical(SETTLEK settlek, out SBoardDelta delta)
    {
        Debug.Assert(settlek == SETTLEK.FALL || settlek == SETTLEK.RISE);
        delta = new SBoardDelta(this);

        foreach (int col in new IntIterator(0, _blayout._length - 1))
        {
            IntIterator rowIteratorFall = settlek == SETTLEK.FALL ?
                new IntIterator(_blayout._height - 1, 0, -1) :  // bottom to top
                new IntIterator(0, _blayout._height - 1, 1);    // top to bottom

            foreach (int row in rowIteratorFall)
            {
                if (_blayout[col, row] != CELLK.STANDARD)
                    continue;

                if (this[col, row] != ' ')
                {
                    // save the delta
                    delta[col, row] = new SBoardDelta.STileDelta(this[col, row], new Vector2Int(col, row));
                    continue;
                }

                if (row == ((settlek == SETTLEK.FALL) ? 0 : _blayout._height - 1))
                    continue;

                IntIterator rowIteratorScan = settlek == SETTLEK.FALL ?
                    new IntIterator(row - 1, 0, -1) :                   // all cells above the current
                    new IntIterator(row + 1, _blayout._height - 1, 1);  // all cells below the current

                foreach (int rowScan in rowIteratorScan)
                {
                    // if the cell is locked, stop searching

                    if (_blayout[col, rowScan] == CELLK.LOCKED)
                        break;

                    // if the cell is void, skip this cell and continue searching

                    if (_blayout[col, rowScan] == CELLK.VOID)
                        continue;

                    // if the cell is standard and non-empty, move that to this cell and set that cell to empty

                    if (this[col, rowScan] != ' ')
                    {
                        // store a delta that <col, rowScan> is moving to <col, row>
                        delta[col, rowScan] = new SBoardDelta.STileDelta(this[col, rowScan], new Vector2Int(col, row));

                        this[col, row] = this[col, rowScan];
                        this[col, rowScan] = ' ';

                        break;
                    }
                }
            }

            IntIterator rowIteratorPopulate = settlek == SETTLEK.FALL ?
                new IntIterator(0, _blayout._height - 1, 1) :   // top to bottom
                new IntIterator(_blayout._height - 1, 0, -1);   // bottom to top

            foreach (int row in rowIteratorPopulate)
            {
                // if the cell is locked, then neither this cell nor any after it can have new characters generate

                if (_blayout[col, row] == CELLK.LOCKED)
                    break;

                // if the cell is void, skip this cell and continue generating new characters

                if (_blayout[col, row] == CELLK.VOID)
                    continue;

                if (this[col, row] == ' ')
                {
                    this[col, row] = RandomChar();
                    delta.CreateTile(new Vector2Int(col, row), this[col, row]);
                }
            }
        }
    }

    private void SettleHorizontal(SETTLEK settlek, out SBoardDelta delta)
    {
        Debug.Assert(settlek == SETTLEK.FROM_LEFT || settlek == SETTLEK.FROM_RIGHT);

        delta = new SBoardDelta(this);

        foreach (int row in new IntIterator(0, _blayout._height - 1))
        {
            IntIterator colIteratorFall = settlek == SETTLEK.FROM_LEFT ?
                new IntIterator(_blayout._length - 1, 0, -1) :  // right to left
                new IntIterator(0, _blayout._length - 1, 1);    // left to right

            foreach (int col in colIteratorFall)
            {
                if (_blayout[col, row] != CELLK.STANDARD)
                    continue;

                if (this[col, row] != ' ')
                {
                    // save the delta
                    delta[col, row] = new SBoardDelta.STileDelta(this[col, row], new Vector2Int(col, row));
                    continue;
                }

                if (col == ((settlek == SETTLEK.FROM_LEFT) ? 0 : _blayout._length - 1))
                    continue;

                IntIterator colIteratorScan = settlek == SETTLEK.FROM_LEFT ?
                    new IntIterator(col - 1, 0, -1) :                   // all cells left of the current
                    new IntIterator(col + 1, _blayout._height - 1, 1);  // all cells right of the current

                foreach (int colScan in colIteratorScan)
                {
                    // if the cell is locked, stop searching

                    if (_blayout[colScan, row] == CELLK.LOCKED)
                        break;

                    // if the cell is void, skip this cell and continue searching

                    if (_blayout[colScan, row] == CELLK.VOID)
                        continue;

                    // if the cell is standard and non-empty, move that to this cell and set that cell to empty

                    if (this[colScan, row] != ' ')
                    {
                        // store a delta that <colScan, row> is moving to <col, row>
                        delta[colScan, row] = new SBoardDelta.STileDelta(this[colScan, row], new Vector2Int(col, row));

                        this[col, row] = this[colScan, row];
                        this[colScan, row] = ' ';
                        break;
                    }
                }
            }

            IntIterator colIteratorPopulate = settlek == SETTLEK.FROM_LEFT ?
                new IntIterator(0, _blayout._length - 1, 1) :   // left to right
                new IntIterator(_blayout._length - 1, 0, -1);   // right to left

            foreach (int col in colIteratorPopulate)
            {
                // if the cell is locked, then neither this cell nor any after it can have new characters generate

                if (_blayout[col, row] == CELLK.LOCKED)
                    break;

                // if the cell is void, skip this cell and continue generating new characters

                if (_blayout[col, row] == CELLK.VOID)
                    continue;

                if (this[col, row] == ' ')
                {
                    this[col, row] = RandomChar();
                    delta.CreateTile(new Vector2Int(col, row), this[col, row]);
                }
            }
        }
    }


    // Possibly move to different file. We want the weights of each character to be customizable

    private char RandomChar()
    {
        return 'A';
        throw new NotImplementedException();
        // return GameManager.CharacterWeights.GetChar(), or whatever path we use to get the current loaded character weights. Can weights be modified by gameplay?
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
public struct SBoardDelta // tag = bdelta
{
    public struct STileDelta
    {
        public readonly char _c;
        public Vector2Int _vec2iEnd;

        public STileDelta(char c, Vector2Int vec2iEnd)
        {
            _c = c;
            _vec2iEnd = vec2iEnd;
        }
    }

    public readonly CBoardState _bstateBase;
    public readonly STileDelta[,] _deltas;

    // We want to avoid duplicates here so a list would be inefficient

    public Dictionary<Vector2Int, char> _newTiles;

    public SBoardDelta(CBoardState bstateBase)
    {
        _bstateBase = bstateBase;
        _deltas = new STileDelta[_bstateBase._blayout._length, _bstateBase._blayout._height];

        foreach (int col in new IntIterator(0, _bstateBase._blayout._length - 1))
        {
            foreach (int row in new IntIterator(0, _bstateBase._blayout._height - 1))
            {
                _deltas[col, row]._vec2iEnd = -Vector2Int.one;
            }
        }

        _newTiles = new Dictionary<Vector2Int, char>();
    }

    public STileDelta this[int col, int row]
    {
        get => _deltas[col, row];
        set => _deltas[col, row] = value;
    }

    public STileDelta this[Vector2Int vec2i]
    {
        get => _deltas[vec2i.x, vec2i.y];
        set => _deltas[vec2i.x, vec2i.y] = value;
    }

    public void CreateTile(Vector2Int vec2iDest, char c)
    {
        Debug.Assert(!_newTiles.ContainsKey(vec2iDest));

        _newTiles[vec2iDest] = c;
    }
}
