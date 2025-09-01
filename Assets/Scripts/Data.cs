// Dictionary Data

using System;
using UnityEngine;

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

enum CELLK
{
    STANDARD,   // Can be filled
    LOCKED,     // Cannot be filled nor passed through
    VOID,       // Cannot be filled, can be passed through
}

enum SETTLEK
{
    IN_PLACE = 0,
    FALL,
    RISE,
    FROM_LEFT,
    FROM_RIGHT,
}

class CBoardLayout // this should probably be a scriptable object or otherwise freely swappable
{
    [Min(1)]
    public int _length = 1;

    [Min(1)]
    public int _height = 1;

    [SerializeField]
    private CELLK[,] _layout;

    CBoardLayout(int length, int height)
    {
        _length = length;
        _height = height;

        _layout = new CELLK[_length, _height]; // column, row
    }

    public CELLK this[int col, int row]
    {
        get => _layout[col, row];
        set => _layout[col, row] = value; // should only be used if a boss has a board disruption
    }
}

class CBoardState // tag = bstate
{
    private readonly CBoardLayout   _blayout;
    private char[,]                 _chars;

    CBoardState(CBoardLayout blayout)
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

    // Should only call after Clone or from within CloneSettled(SETTLEK). Not sure which we'll use
    public void Settle(SETTLEK settlek)
    {
        switch (settlek)
        {
            case SETTLEK.IN_PLACE:
                SettleInPlace();
                return;

            case SETTLEK.FALL:
            case SETTLEK.RISE:
                SettleVertical(settlek);
                return;

            case SETTLEK.FROM_LEFT:
            case SETTLEK.FROM_RIGHT:
                SettleHorizontal(settlek);
                return;
        }
    }

    // all of these functions would be cleaner if we had pre-built iterators. Just select the iterator at the start of the function based on SETTLEK

    private void SettleInPlace()
    {
        for (int col = 0; col < _blayout._length; col++)
        {
            for (int row = 0; row < _blayout._length; row++)
            {
                if (_blayout[col, row] == CELLK.STANDARD && this[col,row] == ' ')
                {
                    this[col, row] = RandomChar();
                }
            }
        }
    }

    private void SettleVertical(SETTLEK settlek)
    {
        Debug.Assert(settlek == SETTLEK.FALL || settlek == SETTLEK.RISE);

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
                    this[col, row] = RandomChar();
            }
        }
    }

    private void SettleHorizontal(SETTLEK settlek)
    {
        Debug.Assert(settlek == SETTLEK.FROM_LEFT || settlek == SETTLEK.FROM_RIGHT);

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
                    this[col, row] = RandomChar();
            }
        }
    }

    // Possibly move to different file. We want the weights of each character to be customizable

    private char RandomChar()
    {
        throw new NotImplementedException();
        // return GameManager.CharacterWeights.GetChar(), or whatever path we use to get the current loaded character weights. Can weights be modified by gameplay?
    }

}