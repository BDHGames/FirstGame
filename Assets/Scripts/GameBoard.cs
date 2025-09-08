using System;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.UIElements;

public class GameBoard : MonoBehaviour
{
	public BoardState _currState, _nextState;
	private BoardDelta _currDelta;

	RESOLVES _resolves = RESOLVES.Nil;

	public Tile[,] _playableBoard;

	public Tile[,] _stagingBoard; // for new tiles before they fall onto the screen

	BoardConfig _config;

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
		_config = BoardConfig.INSTANCE;

		_currState = new BoardState(_config.Layout);

		_playableBoard = new Tile[_config.Layout._length, _config.Layout._height];
		_stagingBoard = new Tile[_config.Layout._length, _config.Layout._height];

		GenerateBoard();
	}



	private void GenerateBoard()
	{
		foreach (Vector2Int coord in new Vector2IntIterator(Vector2Int.zero, _config.Layout.BottomRight()))
		{
			if (_config.Layout[coord.x, coord.y] == CELLK.STANDARD)
			{
				_playableBoard[coord.x, coord.y] = SpawnTile(coord);
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
					_currState[0, 6] = ' ';
					_nextState = _currState.CloneSettled(_config.SettleKind, out _currDelta);
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
		foreach (Vector2Int coord in new Vector2IntIterator(_config.Layout.BottomRight(), Vector2Int.zero))
		{
			BoardDelta.TileDelta tileDelta = _currDelta[coord];

			if (tileDelta.IsTileDeletion() && _playableBoard[coord.x, coord.y])
			{
				Destroy(_playableBoard[coord.x, coord.y].gameObject);
			}
		}

		_resolves = RESOLVES.SpawnNew;
	}

	private void SpawnNewTiles()
	{
		// Vector2s are pass-by-value so there's no point in creating and destroying a new one every iteration of the loop

		Vector2 spawnDir = _config.SettleKind == SETTLEK.IN_PLACE ? Vector2.up : -FallDirection();
		Vector2 layoutDims = _config.Layout.Dims();
		Vector2 tileSpacing = _config.TileSpacing;
		Vector2 spawnOffset = _config.SpawnOffset;

		// I'd like this to use SpawnTile but this allows for better data caching.

		foreach (var kvp in _currDelta._newTiles)
		{
			Tile tile = _stagingBoard[kvp.Key.x, kvp.Key.y] = Instantiate(_config.DefaultTilePrefab);
			tile.transform.position = spawnOffset + tileSpacing * spawnDir * (kvp.Key - layoutDims);
			tile._letter = kvp.Value;
			tile._coord = kvp.Key;
		}

		// TODO mini settle+cleanup step

		_resolves = RESOLVES.Fall;
	}

	private void FallTiles()
	{
		float dT = Time.deltaTime;

		// iterate through present tiles

		bool movedTile = false;

		Vector3 fallDir = FallDirection();
		Vector2 spawnOffset = _config.SpawnOffset;
		Vector2 tileSpacing = _config.TileSpacing;

		foreach (Vector2Int coord in new Vector2IntIterator(_config.Layout.BottomRight()))
		{
			BoardDelta.TileDelta tDelta = _currDelta[coord];

			// skip deleted tiles

			if (tDelta.IsTileDeletion())
				continue;

			Tile tileToMove = _playableBoard[coord.x, coord.y];

			if (!tileToMove)
				continue;

			Vector3 dest = spawnOffset + _currDelta[coord]._destCoord * tileSpacing;

			// We are at or past our destination. It's a better check than before but still not ideal. Would be good to stress test this

			if (Vector3.Dot(dest - tileToMove.transform.position, fallDir) <= 0)
			{
				tileToMove.transform.position = dest;
			}
			else
			{
				tileToMove.transform.position += _config.FallSpeed * dT * fallDir;
				movedTile = true;
			}
		}

		// iterate through staged tiles

		foreach (Vector2Int coord in new Vector2IntIterator(_config.Layout.BottomRight()))
		{
			Tile tileToMove = _stagingBoard[coord.x, coord.y];

			if (!tileToMove)
				continue;

			Vector3 dest = spawnOffset + coord * tileSpacing;

			if (Vector3.Dot(dest - tileToMove.transform.position, fallDir) <= 0)
			{
				tileToMove.transform.position = dest;
			}
			else
			{
				tileToMove.transform.position += _config.FallSpeed * dT * fallDir;
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

		foreach (Vector2Int startCoord in new Vector2IntIterator(_config.Layout.BottomRight(), Vector2Int.zero))
		{
			BoardDelta.TileDelta tDelta = _currDelta[startCoord];

			if (tDelta.IsTileDeletion())
				continue;

			_playableBoard[tDelta._destCoord.x, tDelta._destCoord.y] = _playableBoard[startCoord.x, startCoord.y];
			_playableBoard[tDelta._destCoord.x, tDelta._destCoord.y]._coord = tDelta._destCoord;
		}

		// move staged tiles

		foreach (Vector2Int coord in new Vector2IntIterator(_config.Layout.BottomRight(), Vector2Int.zero))
		{
			if (_stagingBoard[coord.x, coord.y])
			{
				_playableBoard[coord.x, coord.y] = _stagingBoard[coord.x, coord.y];
				_playableBoard[coord.x, coord.y]._coord = coord;
				_stagingBoard[coord.x, coord.y] = null;
			}
		}

		_currDelta = null;
		_resolves = RESOLVES.Nil;
	}

	private Tile SpawnTile(Vector2Int coord, Vector2 posOffset = default)
	{
		Tile tile = Instantiate(_config.DefaultTilePrefab, this.transform);
		tile.transform.position = _config.SpawnOffset + posOffset + (coord * _config.TileSpacing);
		tile._coord = coord;
		tile._letter = _config.Weights.RandomChar();
		return tile;
	}

	private Vector2 FallDirection()
	{
		switch (_config.SettleKind)
		{
			case SETTLEK.IN_PLACE:
				return Vector2.zero;
			case SETTLEK.FALL:
				return Vector2.down;
			case SETTLEK.RISE:
				return Vector2.up;
			case SETTLEK.FROM_LEFT:
				return Vector2.right;
			case SETTLEK.FROM_RIGHT:
				return Vector2.left;
			default:
				Debug.LogError($"Unexpected SETTLEK {_config.SettleKind} encountered");
				return new Vector2(float.NaN, float.NaN);
		}
	}
}
