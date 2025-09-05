using System;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.UIElements;

public class GameBoard : MonoBehaviour
{
	public BoardLayout _layout = new BoardLayout(7,7);

	public BoardState _currentBoard, _nextBoard;

	private BoardDelta _currBoardDelta;

	RESOLVES _resolves = RESOLVES.Nil;

	public Tile _tilePrefab;

	public Tile[,] _playableBoard;

	public Tile[,] _stagingBoard; // for new tiles before they fall onto the screen

	enum RESOLVES // RESOLVE State
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

		_currentBoard = new BoardState(_layout);

		_currentBoard.Settle(SETTLEK.FALL, out _);

		_playableBoard = new Tile[_layout._length, _layout._height];
		_stagingBoard = new Tile[_layout._length, _layout._height];
		DisplayBoard();
	}

	private void ConstructTestBoard()
	{
		_layout[0, 0] = CELLK.STANDARD; _layout[1, 0] = CELLK.STANDARD; _layout[2, 0] = CELLK.VOID;	 _layout[3, 0] = CELLK.VOID;	 _layout[4, 0] = CELLK.VOID;	 _layout[5, 0] = CELLK.STANDARD; _layout[6, 0] = CELLK.STANDARD;
		_layout[0, 1] = CELLK.STANDARD; _layout[1, 1] = CELLK.STANDARD; _layout[2, 1] = CELLK.STANDARD; _layout[3, 1] = CELLK.VOID;	 _layout[4, 1] = CELLK.STANDARD; _layout[5, 1] = CELLK.STANDARD; _layout[6, 1] = CELLK.STANDARD;
		_layout[0, 2] = CELLK.VOID;	 _layout[1, 2] = CELLK.STANDARD; _layout[2, 2] = CELLK.STANDARD; _layout[3, 2] = CELLK.STANDARD; _layout[4, 2] = CELLK.STANDARD; _layout[5, 2] = CELLK.STANDARD; _layout[6, 2] = CELLK.VOID;
		_layout[0, 3] = CELLK.VOID;	 _layout[1, 3] = CELLK.VOID;	 _layout[2, 3] = CELLK.STANDARD; _layout[3, 3] = CELLK.STANDARD; _layout[4, 3] = CELLK.STANDARD; _layout[5, 3] = CELLK.VOID;	 _layout[6, 3] = CELLK.VOID;
		_layout[0, 4] = CELLK.VOID;	 _layout[1, 4] = CELLK.STANDARD; _layout[2, 4] = CELLK.STANDARD; _layout[3, 4] = CELLK.STANDARD; _layout[4, 4] = CELLK.STANDARD; _layout[5, 4] = CELLK.STANDARD; _layout[6, 4] = CELLK.VOID;
		_layout[0, 5] = CELLK.STANDARD; _layout[1, 5] = CELLK.STANDARD; _layout[2, 5] = CELLK.STANDARD; _layout[3, 5] = CELLK.VOID;	 _layout[4, 5] = CELLK.STANDARD; _layout[5, 5] = CELLK.STANDARD; _layout[6, 5] = CELLK.STANDARD;
		_layout[0, 6] = CELLK.STANDARD; _layout[1, 6] = CELLK.STANDARD; _layout[2, 6] = CELLK.VOID;	 _layout[3, 6] = CELLK.VOID;	 _layout[4, 6] = CELLK.VOID;	 _layout[5, 6] = CELLK.STANDARD; _layout[6, 6] = CELLK.STANDARD;
	}

	private void DisplayBoard()
	{
		foreach (Vector2Int coord in new Vector2IntIterator(Vector2Int.zero, _currentBoard._layout.BottomRight()))
		{
			if (_currentBoard._layout[coord.x, coord.y] == CELLK.STANDARD)
			{
				_playableBoard[coord.x, coord.y] = Instantiate(_tilePrefab);
				_playableBoard[coord.x, coord.y].transform.position = new Vector2(2 * coord.x, -2 * coord.y);
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
					_currentBoard[0,6] = ' ';
					_nextBoard = _currentBoard.CloneSettled(SETTLEK.FALL, out _currBoardDelta);
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
		foreach (Vector2Int coord in new Vector2IntIterator(_currentBoard._layout.BottomRight(), Vector2Int.zero))
		{
			BoardDelta.TileDelta tileDelta = _currBoardDelta[coord];

			if (tileDelta.IsTileDeletion() && _playableBoard[coord.x, coord.y])
			{
				Destroy(_playableBoard[coord.x, coord.y].gameObject);
			}
		}

		_resolves = RESOLVES.SpawnNew;
	}

	private void SpawnNewTiles()
	{
		foreach (var kvp in _currBoardDelta._newTiles)
		{
			_stagingBoard[kvp.Key.x, kvp.Key.y] = Instantiate(_tilePrefab);
			_stagingBoard[kvp.Key.x, kvp.Key.y].transform.position = new Vector2(kvp.Key.x * 2, (_layout._height - kvp.Key.y) * 2);

			_stagingBoard[kvp.Key.x, kvp.Key.y]._letter = kvp.Value;
		}

		// TODO mini settle+cleanup step

		_resolves = RESOLVES.Fall;
	}

	private void FallTiles()
	{
		float dT = Time.deltaTime;

		// iterate through present tiles

		bool movedTile = false;

		foreach (Vector2Int coord in new Vector2IntIterator(_currentBoard._layout.BottomRight()))
		{
			BoardDelta.TileDelta tDelta = _currBoardDelta[coord];

			// skip deleted tiles

			if (tDelta.IsTileDeletion())
				continue;

			Tile tileToMove = _playableBoard[coord.x, coord.y];

			if (!tileToMove)
				continue;

			Vector2 moveDir = _currBoardDelta[coord]._destCoord - coord;
			moveDir.Normalize();

			Vector3 distToMove = tileToMove.transform.position - new Vector3(2 * _currBoardDelta[coord]._destCoord.x, -2 * _currBoardDelta[coord]._destCoord.y, 0);
			if (distToMove.magnitude < 0.1)
			{
				tileToMove.transform.position = new Vector3(2 * _currBoardDelta[coord]._destCoord.x, -2 * _currBoardDelta[coord]._destCoord.y, 0);
			}
			else
			{
				tileToMove.transform.position += dT * new Vector3(3 * moveDir.x, -3 * moveDir.y);
				movedTile = true;
			}
		}

		// iterate through staged tiles

		foreach (Vector2Int coord in new Vector2IntIterator(_currentBoard._layout.BottomRight()))
		{
			Tile tileToMove = _stagingBoard[coord.x, coord.y];

			if (!tileToMove)
				continue;

			Vector3 distToMove = tileToMove.transform.position - new Vector3(2 * coord.x, -2 * coord.y, 0);
			if (distToMove.magnitude < 0.1)
			{
				tileToMove.transform.position = new Vector3(2 * coord.x, -2 * coord.y, 0);
			}
			else
			{
				// hardcoding SETTLEK.Fall for now
				tileToMove.transform.position += dT * new Vector3(0, -3);
				movedTile = true;
			}
		}

		// check if we're done

		if (!movedTile)
		{
			_resolves = RESOLVES.Cleanup;
		}
	}

	void FinishResolve()
	{
		// move tiles into their correct spot on the playing board. Currently only supports SETTLEK.Fall

		foreach (Vector2Int startCoord in new Vector2IntIterator(_layout.BottomRight(), Vector2Int.zero))
		{
			BoardDelta.TileDelta tDelta = _currBoardDelta[startCoord];

			if (tDelta.IsTileDeletion())
				continue;

			_playableBoard[tDelta._destCoord.x, tDelta._destCoord.y] = _playableBoard[startCoord.x, startCoord.y];
		}

		// move staged tiles

		foreach (Vector2Int coord in new Vector2IntIterator(_layout.BottomRight(), Vector2Int.zero))
		{
			if (_stagingBoard[coord.x, coord.y])
			{
				_playableBoard[coord.x, coord.y] = _stagingBoard[coord.x, coord.y];
				_stagingBoard[coord.x, coord.y] = null;
			}
		}

		_currBoardDelta = null;
		_resolves = RESOLVES.Nil;
	}
}
