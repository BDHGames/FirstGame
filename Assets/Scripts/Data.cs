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
                SettleFall();
                return;

            case SETTLEK.RISE:
                SettleRise();
                return;

            case SETTLEK.FROM_LEFT:
                SettleFromLeft();
                return;

            case SETTLEK.FROM_RIGHT:
                SettleFromRight();
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
                    this[col, row] = CharRandom();
                }
            }
        }
    }

    // The two passes in these functions could probably be merged. Fall is bottom-to-top and populate is top-to-bottom

    private void SettleFall()
    {
        // requires two passes. First for the fall and second for filling in

        for (int col = 0; col < _blayout._length; col++)
        {
            for (int row = _blayout._height - 1; row >= 0; row--)
            {
                if (_blayout[col, row] != CELLK.STANDARD)
                    continue;

                if (this[col, row] != ' ')
                    continue;
                
                for (int rowAbove = row - 1; rowAbove >= 0; rowAbove--)
                {
                    // if the cell above is locked, stop searching

                    if (_blayout[col, rowAbove] == CELLK.LOCKED)
                        break;

                    // if the cell above is void, skip this cell and continue searching

                    if (_blayout[col, rowAbove] == CELLK.VOID)
                        continue;

                    // if the cell above is standard and non-empty, move that to this cell and set that cell to empty

                    if (this[col, rowAbove] != ' ')
                    {
                        this[col, row] = this[col, rowAbove];
                        this[col, rowAbove] = ' ';
                        break;
                    }
                }
            }
        }

        for (int col = 0; col < _blayout._length; col++)
        {
            for (int row = 0; row < _blayout._length; row++)
            {
                // if the cell is locked, then neither this cell nor any beneath it can have new characters generate

                if (_blayout[col, row] == CELLK.LOCKED)
                    break;

                // if the cell is void, skip this cell and continue generating new characters

                if (_blayout[col, row] == CELLK.VOID)
                    continue;

                if (this[col, row] == ' ')
                    this[col, row] = CharRandom();
            }
        }
    }

    private void SettleRise()
    {
        // requires two passes. First for the fall and second for filling in

        for (int col = 0; col < _blayout._length; col++)
        {
            for (int row = 0; row < _blayout._height; row++)
            {
                if (_blayout[col, row] != CELLK.STANDARD)
                    continue;

                if (this[col, row] != ' ')
                    continue;

                for (int rowBelow = row + 1; rowBelow < _blayout._height; rowBelow++)
                {
                    // if the cell below is locked, stop searching

                    if (_blayout[col, rowBelow] == CELLK.LOCKED)
                        break;

                    // if the cell below is void, skip this cell and continue searching

                    if (_blayout[col, rowBelow] == CELLK.VOID)
                        continue;

                    // if the cell below is standard and non-empty, move that to this cell and set that cell to empty

                    if (this[col, rowBelow] != ' ')
                    {
                        this[col, row] = this[col, rowBelow];
                        this[col, rowBelow] = ' ';
                        break;
                    }
                }
            }
        }

        for (int col = 0; col < _blayout._length; col++)
        {
            for (int row = _blayout._height - 1; row >= 0; row--)
            {
                // if the cell is locked, then neither this cell nor any above it can have new characters generate

                if (_blayout[col, row] == CELLK.LOCKED)
                    break;

                // if the cell is void, skip this cell and continue generating new characters

                if (_blayout[col, row] == CELLK.VOID)
                    continue;

                if (this[col, row] == ' ')
                    this[col, row] = CharRandom();
            }
        }
    }

    private void SettleFromLeft()
    {
        // requires two passes. First for the fall and second for filling in

        for (int row = 0; row < _blayout._height; row++)
        {
            for (int col = _blayout._length - 1; col >= 0; col--)
            {
                if (_blayout[col, row] != CELLK.STANDARD)
                    continue;

                if (this[col, row] != ' ')
                    continue;

                for (int colLeft = col - 1; colLeft >= 0; colLeft--)
                {
                    // if the cell to the left is locked, stop searching

                    if (_blayout[col, colLeft] == CELLK.LOCKED)
                        break;

                    // if the cell to the left is void, skip this cell and continue searching

                    if (_blayout[col, colLeft] == CELLK.VOID)
                        continue;

                    // if the cell to the left is standard and non-empty, move that to this cell and set that cell to empty

                    if (this[col, colLeft] != ' ')
                    {
                        this[col, row] = this[col, colLeft];
                        this[col, colLeft] = ' ';
                        break;
                    }
                }
            }
        }

        for (int row = 0; row < _blayout._height; row++)
        {
            for (int col = 0; col < _blayout._length; col++)
            {
                // if the cell is locked, then neither this cell nor any to the right it can have new characters generate

                if (_blayout[col, row] == CELLK.LOCKED)
                    break;

                // if the cell is void, skip this cell and continue generating new characters

                if (_blayout[col, row] == CELLK.VOID)
                    continue;

                if (this[col, row] == ' ')
                    this[col, row] = CharRandom();
            }
        }
    }

    private void SettleFromRight()
    {
        // requires two passes. First for the fall and second for filling in

        for (int row = 0; row < _blayout._height; row++)
        {
            for (int col = 0; col < _blayout._length; col++)
            {
                if (_blayout[col, row] != CELLK.STANDARD)
                    continue;

                if (this[col, row] != ' ')
                    continue;

                for (int colLeft = col - 1; colLeft >= 0; colLeft--)
                {
                    // if the cell to the left is locked, stop searching

                    if (_blayout[col, colLeft] == CELLK.LOCKED)
                        break;

                    // if the cell to the left is void, skip this cell and continue searching

                    if (_blayout[col, colLeft] == CELLK.VOID)
                        continue;

                    // if the cell to the left is standard and non-empty, move that to this cell and set that cell to empty

                    if (this[col, colLeft] != ' ')
                    {
                        this[col, row] = this[col, colLeft];
                        this[col, colLeft] = ' ';
                        break;
                    }
                }
            }
        }

        for (int row = 0; row < _blayout._height; row++)
        {
            for (int col = _blayout._length - 1; col >= 0; col--)
            {
                // if the cell is locked, then neither this cell nor any to the left can have new characters generate

                if (_blayout[col, row] == CELLK.LOCKED)
                    break;

                // if the cell is void, skip this cell and continue generating new characters

                if (_blayout[col, row] == CELLK.VOID)
                    continue;

                if (this[col, row] == ' ')
                    this[col, row] = CharRandom();
            }
        }
    }



    // Possibly move to different file. We want the weights of each character to be customizable
    private char CharRandom()
    {
        throw new NotImplementedException();
    }

}