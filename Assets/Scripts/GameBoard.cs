using System;
using System.Collections.Generic;
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

	public static GameBoard INSTANCE;

	private void Awake()
	{
		// set up singleton

		if (INSTANCE != null && INSTANCE != this)
		{
			Destroy(gameObject);
			return;
		}

		INSTANCE = this;
	}

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
		if (_resolves == RESOLVES.Nil)
		{
			//if (Input.GetMouseButtonDown((int)MouseButton.LeftMouse))
			//{
			//	_currState[0, 6] = ' ';
			//	_nextState = _currState.CloneSettled(_config.SettleKind, out _currDelta);
			//	_resolves = RESOLVES.DeleteSelected;
			//}
		}

		UpdateResolveState();
	}

	private void UpdateResolveState()
	{
		switch (_resolves)
		{
			case RESOLVES.Nil:
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

		Vector2 spawnDir = (_config.SettleKind == SETTLEK.IN_PLACE) ? Vector2.up : -FallDirection();
		Vector2 layoutDims = _config.Layout.Dims();
		Vector2 tileSpacing = _config.TileSpacing;
		Vector2 spawnOffset = _config.SpawnOffset;
		Vector2 stagingTopLeft = spawnOffset + tileSpacing * spawnDir * layoutDims;

		// I'd like this to use SpawnTile but this allows for better data caching.

		foreach (var kvp in _currDelta._newTiles)
		{
			Tile tile = _stagingBoard[kvp.Key.x, kvp.Key.y] = Instantiate<Tile>(_config.DefaultTilePrefab, this.transform);
			tile.transform.position = stagingTopLeft + (kvp.Key * tileSpacing * new Vector2(1, -1));
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

			Vector3 dest = spawnOffset + _currDelta[coord]._destCoord * tileSpacing * new Vector2(1, -1);

			// We are at or past our destination. It's a better check than before but still not ideal. Would be good to stress test this

			if (Vector3.Dot(dest - tileToMove.transform.position, fallDir) <= 0 || _config.SettleKind == SETTLEK.IN_PLACE)
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

			Vector3 dest = spawnOffset + coord * tileSpacing * new Vector2(1, -1);

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
		Vector2IntIterator coordIterator;

		switch (_config.SettleKind)
		{
			case SETTLEK.IN_PLACE:
			case SETTLEK.FALL:
			case SETTLEK.FROM_LEFT:
				coordIterator = new Vector2IntIterator(_config.Layout.BottomRight(), Vector2Int.zero); // y first doesn't really matter here
				break;
			default:
				coordIterator = new Vector2IntIterator(Vector2Int.zero, _config.Layout.BottomRight());
				break;
		}

		foreach (Vector2Int startCoord in coordIterator)
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
		_currState = _nextState;
		_nextState = null;
		TileSelector.INSTANCE._isSelectingEnabled = true;
	}

	private Tile SpawnTile(Vector2Int coord, Vector2 posOffset = default)
	{
		Tile tile = Instantiate<Tile>(_config.DefaultTilePrefab, this.transform);
		tile.transform.position = _config.SpawnOffset + posOffset + (coord * _config.TileSpacing * new Vector2(1, -1));
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

	internal void SubmitWord(List<Tile> selectedTiles)
	{
		foreach (Tile tile in selectedTiles)
		{
			_currState[tile._coord] = ' ';
		}

		_nextState = _currState.CloneSettled(_config.SettleKind, out _currDelta);
		_resolves = RESOLVES.DeleteSelected;
		TileSelector.INSTANCE._isSelectingEnabled = false;
	}
}
