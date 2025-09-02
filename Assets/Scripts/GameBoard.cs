using System;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.UIElements;

public class GameBoard : MonoBehaviour
{
    public CBoardLayout _layout = new CBoardLayout(7,7);

    public CBoardState _currentBoard, _nextBoard;

    private SBoardDelta _currDelta;

    RESOLVES _resolves = RESOLVES.Nil;

    public Tile _tilePrefab;

    public Tile[,] _playableBoard;

    public Tile[,] _stagingBoard; // for new tiles before they fall onto the screen

    enum RESOLVES
    {
        Nil = -1,
        DeleteSelected = 0,
        SpawnNew = 1,
        Fall = 2,
        Cleanup = 3
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ConstructTestBoard();

        _currentBoard = new CBoardState(_layout);

        _currentBoard.Settle(SETTLEK.FALL, out _);

        _playableBoard = new Tile[_layout._length, _layout._height];
        _stagingBoard = new Tile[_layout._length, _layout._height];
        DisplayBoard();
    }

    private void ConstructTestBoard()
    {
        _layout[0, 0] = CELLK.STANDARD; _layout[1, 0] = CELLK.STANDARD; _layout[2, 0] = CELLK.VOID;     _layout[3, 0] = CELLK.VOID;     _layout[4, 0] = CELLK.VOID;     _layout[5, 0] = CELLK.STANDARD; _layout[6, 0] = CELLK.STANDARD;
        _layout[0, 1] = CELLK.STANDARD; _layout[1, 1] = CELLK.STANDARD; _layout[2, 1] = CELLK.STANDARD; _layout[3, 1] = CELLK.VOID;     _layout[4, 1] = CELLK.STANDARD; _layout[5, 1] = CELLK.STANDARD; _layout[6, 1] = CELLK.STANDARD;
        _layout[0, 2] = CELLK.VOID;     _layout[1, 2] = CELLK.STANDARD; _layout[2, 2] = CELLK.STANDARD; _layout[3, 2] = CELLK.STANDARD; _layout[4, 2] = CELLK.STANDARD; _layout[5, 2] = CELLK.STANDARD; _layout[6, 2] = CELLK.VOID;
        _layout[0, 3] = CELLK.VOID;     _layout[1, 3] = CELLK.VOID;     _layout[2, 3] = CELLK.STANDARD; _layout[3, 3] = CELLK.STANDARD; _layout[4, 3] = CELLK.STANDARD; _layout[5, 3] = CELLK.VOID;     _layout[6, 3] = CELLK.VOID;
        _layout[0, 4] = CELLK.VOID;     _layout[1, 4] = CELLK.STANDARD; _layout[2, 4] = CELLK.STANDARD; _layout[3, 4] = CELLK.STANDARD; _layout[4, 4] = CELLK.STANDARD; _layout[5, 4] = CELLK.STANDARD; _layout[6, 4] = CELLK.VOID;
        _layout[0, 5] = CELLK.STANDARD; _layout[1, 5] = CELLK.STANDARD; _layout[2, 5] = CELLK.STANDARD; _layout[3, 5] = CELLK.VOID;     _layout[4, 5] = CELLK.STANDARD; _layout[5, 5] = CELLK.STANDARD; _layout[6, 5] = CELLK.STANDARD;
        _layout[0, 6] = CELLK.STANDARD; _layout[1, 6] = CELLK.STANDARD; _layout[2, 6] = CELLK.VOID;     _layout[3, 6] = CELLK.VOID;     _layout[4, 6] = CELLK.VOID;     _layout[5, 6] = CELLK.STANDARD; _layout[6, 6] = CELLK.STANDARD;
    }

    private void DisplayBoard()
    {
        foreach (Vector2Int vec2iCoord in new Vector2IntIterator(Vector2Int.zero, new Vector2Int(_currentBoard._blayout._length - 1, _currentBoard._blayout._height - 1), false))
        {
            if (_currentBoard._blayout[vec2iCoord.x, vec2iCoord.y] == CELLK.STANDARD)
            {
                _playableBoard[vec2iCoord.x, vec2iCoord.y] = Instantiate(_tilePrefab);
                _playableBoard[vec2iCoord.x, vec2iCoord.y].transform.position = new Vector2(2 * vec2iCoord.x, -2 * vec2iCoord.y);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        switch (_resolves)
        {
            case RESOLVES.Nil:
                if (Input.GetMouseButtonDown((int)MouseButton.LeftMouse))
                {
                    _currentBoard[0, 6] = ' ';
                    _nextBoard = _currentBoard.CloneSettled(SETTLEK.FALL, out _currDelta);
                    _resolves = RESOLVES.DeleteSelected;
                }
                return;

            case RESOLVES.DeleteSelected:
                DeleteSelectedTiles();
                return;

            case RESOLVES.SpawnNew:
                SpawnNewTiles();
                return;

            case RESOLVES.Fall:
                FallTiles();
                return;

            case RESOLVES.Cleanup:
                FinishResolve();
                return;
        }
    }

    private void DeleteSelectedTiles()
    {
        foreach (Vector2Int vec2iCoord in new Vector2IntIterator(_currentBoard._blayout.Vec2IDimMinusOne(), Vector2Int.zero))
        {
            SBoardDelta.STileDelta tDelta = _currDelta[vec2iCoord];

            if (tDelta._vec2iEnd == -Vector2Int.one)
            {
                if (_playableBoard[vec2iCoord.x, vec2iCoord.y])
                {
                    Destroy(_playableBoard[vec2iCoord.x, vec2iCoord.y].gameObject);
                }
            }
        }

        _resolves = RESOLVES.SpawnNew;
    }

    private void SpawnNewTiles()
    {
        foreach (var kvp in _currDelta._newTiles)
        {
            _stagingBoard[kvp.Key.x, kvp.Key.y] = Instantiate(_tilePrefab);
            _stagingBoard[kvp.Key.x, kvp.Key.y].transform.position = new Vector2(kvp.Key.x * 2, (_layout._height - kvp.Key.y) * 2);

            _stagingBoard[kvp.Key.x, kvp.Key.y]._charLetter = kvp.Value;
        }

        _resolves = RESOLVES.Fall;
    }

    private void FallTiles()
    {
        float dT = Time.deltaTime;

        // iterate through present tiles

        bool fMovedTile = false;

        foreach (Vector2Int vec2iCoord in new Vector2IntIterator(_currentBoard._blayout.Vec2IDimMinusOne()))
        {
            SBoardDelta.STileDelta tDelta = _currDelta[vec2iCoord];

            // skip deleted tiles

            if (tDelta._vec2iEnd == -Vector2Int.one)
                continue;

            Tile tileToMove = _playableBoard[vec2iCoord.x, vec2iCoord.y];

            if (!tileToMove)
                continue;

            Vector2 dSMove = _currDelta[vec2iCoord]._vec2iEnd - vec2iCoord;
            dSMove.Normalize();

            Vector3 distToMove = tileToMove.transform.position - new Vector3(2 * _currDelta[vec2iCoord]._vec2iEnd.x, -2 * _currDelta[vec2iCoord]._vec2iEnd.y, 0);
            if (distToMove.magnitude < 0.1)
            {
                tileToMove.transform.position = new Vector3(2 * _currDelta[vec2iCoord]._vec2iEnd.x, -2 * _currDelta[vec2iCoord]._vec2iEnd.y, 0);
            }
            else
            {
                tileToMove.transform.position += dT * new Vector3(3 * dSMove.x, -3 * dSMove.y);
                fMovedTile = true;
            }
            
        }

        // iterate through staged tiles

        foreach (Vector2Int vec2iCoord in new Vector2IntIterator(_currentBoard._blayout.Vec2IDimMinusOne()))
        {
            Tile tileToMove = _stagingBoard[vec2iCoord.x, vec2iCoord.y];

            if (!tileToMove)
                continue;

            Vector3 distToMove = tileToMove.transform.position - new Vector3(2 * vec2iCoord.x, -2 * vec2iCoord.y, 0);
            if (distToMove.magnitude < 0.1)
            {
                tileToMove.transform.position = new Vector3(2 * vec2iCoord.x, -2 * vec2iCoord.y, 0);
            }
            else
            {
                // hardcoding SETTLEK.Fall for now
                tileToMove.transform.position += dT * new Vector3(0, -3);
                fMovedTile = true;
            }
        }

        // check if we're done

        if (!fMovedTile)
        {
            _resolves = RESOLVES.Cleanup;
        }
    }

    void FinishResolve()
    {
        // move tiles into their correct spot on the playing board. Currently only supports SETTLEK.Fall

        foreach (Vector2Int vec2iCoord in new Vector2IntIterator(_currentBoard._blayout.Vec2IDimMinusOne(), Vector2Int.zero))
        {
            SBoardDelta.STileDelta tDelta = _currDelta[vec2iCoord];

            if (tDelta._vec2iEnd == -Vector2Int.one)
                continue;

            _playableBoard[tDelta._vec2iEnd.x, tDelta._vec2iEnd.y] = _playableBoard[vec2iCoord.x, vec2iCoord.y];
        }

        // move staged tiles

        foreach (Vector2Int vec2iCoord in new Vector2IntIterator(_currentBoard._blayout.Vec2IDimMinusOne(), Vector2Int.zero))
        {
            if (_stagingBoard[vec2iCoord.x, vec2iCoord.y])
            {
                _playableBoard[vec2iCoord.x, vec2iCoord.y] = _stagingBoard[vec2iCoord.x, vec2iCoord.y];
            }
        }

        _resolves = RESOLVES.Nil;
    }
}
