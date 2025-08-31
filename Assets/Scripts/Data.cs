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

struct SCell
{
    enum CELLS // CELL Status
    {
        EMPTY,  // Can be filled
        LOCKED, // Cannot be filled nor passed through
        VOID,   // Cannot be filled, can be passed through
        FULL
    }

    CELLS _cells;

    Tile _tile; // might want to move? I like MVC pattern a lot here.
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

    private SCell[,] _cells;

    CBoardLayout(int length, int height)
    {
        _length = length;
        _height = height;

        _cells = new SCell[_length, _height]; // column, row
    }

    ref SCell CellAt(int col, int row)
    {
        return ref _cells[col, row];
    }
}