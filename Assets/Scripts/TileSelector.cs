using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class TileSelector : MonoBehaviour
{
	public enum TILESELECTK
	{
		[InspectorName("Click and Drag")]
		CLICK_AND_DRAG,
		[InspectorName("Click to Start and Submit")]
		CLICK_AND_MOVE,
		[InspectorName("Click Each Letter")]
		CLICK_EACH_LETTER,
		[InspectorName("Keyboard Movement")]
		KEYBOARD_MOVE,
		[InspectorName("Type")]
		TYPE
	}

	[SerializeField]
	private TILESELECTK _selectionKind = TILESELECTK.CLICK_AND_DRAG;

	List<Tile> _selectedTiles = new List<Tile>();
	Tile _currentHighlightedTile;
	string _word = "";

	bool _isMouseDown = false;
	public bool _isSelectingEnabled = true;


	public static TileSelector INSTANCE;

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

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
		if (_isSelectingEnabled)
		{
			UpdateTileSelection();
		}

    }

	void UpdateTileSelection()
	{
		switch (_selectionKind)
		{
			case TILESELECTK.CLICK_AND_DRAG:
				UpdateClickDrag();
				return;
			case TILESELECTK.CLICK_AND_MOVE:
				UpdateClickMove();
				return;
			case TILESELECTK.CLICK_EACH_LETTER:
				UpdateClickLetter();
				return;
			case TILESELECTK.KEYBOARD_MOVE:
				UpdateKeyboardMove();
				return;
			case TILESELECTK.TYPE:
				UpdateType();
				return;
		}
	}

	private void UpdateClickDrag()
	{
		if (!_isMouseDown)
		{
			if (Input.GetMouseButtonDown((int)MouseButton.Left))
			{
				_isMouseDown = true;
				Debug.Log("Selecting Started");
				if (_currentHighlightedTile) // we probably shouldn't allow clicking when you're not hovering over a tile but meh
				{
					_selectedTiles.Add(_currentHighlightedTile);
					_currentHighlightedTile.SetTileSelectState(Tile.TILESELECTS.SELECTED_AND_HIGHLIGHTED);
					_word += _currentHighlightedTile._letter;
				}
			}
		}
		if (_isMouseDown)
		{
			if (!Input.GetMouseButton((int)MouseButton.Left))
			{
				_isMouseDown = false;
				Debug.Log("Selecting Ended");

				if (_word != "")
				{
					// this is where we would confirm that this is a real word

					GameBoard.INSTANCE.SubmitWord(_selectedTiles);

					_word = "";

					foreach (Tile t in _selectedTiles)
					{
						if (t == _currentHighlightedTile)
						{
							t.SetTileSelectState(Tile.TILESELECTS.HIGHLIGHTED);
						}
						else
						{
							t.SetTileSelectState(Tile.TILESELECTS.NORMAL);
						}
					}
					_selectedTiles.Clear();
				}
				else
				{

				}
			}
		}
	}

	private void UpdateType()
	{
		throw new NotImplementedException();
	}

	private void UpdateKeyboardMove()
	{
		throw new NotImplementedException();
	}

	private void UpdateClickLetter()
	{
		throw new NotImplementedException();
	}

	private void UpdateClickMove()
	{
		throw new NotImplementedException();
	}

	internal void MouseOverTile(Tile tile)
	{
		_currentHighlightedTile = tile;

		if (_selectionKind == TILESELECTK.CLICK_AND_DRAG && _isMouseDown)
		{
			if (_selectedTiles.Count == 0)
			{
				_selectedTiles.Add(tile);
				tile.SetTileSelectState(Tile.TILESELECTS.SELECTED_AND_HIGHLIGHTED);
				_word += tile._letter;
			}
			else
			{
				// if the tile is the second-to-last element in the list, remove the last element

				if (_selectedTiles.Count >= 2 && _selectedTiles[^2] == tile)
				{
					Tile lastTile = _selectedTiles[^1];
					lastTile.SetTileSelectState(Tile.TILESELECTS.NORMAL);
					_selectedTiles.Remove(_selectedTiles[^1]);
					_word = _word[..^1];
					tile.SetTileSelectState(Tile.TILESELECTS.SELECTED_AND_HIGHLIGHTED);
				}
				else
				{
					if (_selectedTiles.Contains(tile))
					{
						// do nothing
					}
					else
					{
						Vector2Int gridDist = _selectedTiles[^1]._coord - tile._coord;

						if (Mathf.Abs(gridDist.x) <= 1 && Mathf.Abs(gridDist.y) <= 1 && !_selectedTiles.Contains(tile))
						{
							_selectedTiles.Add(tile);
							tile.SetTileSelectState(Tile.TILESELECTS.SELECTED_AND_HIGHLIGHTED);
							_word += tile._letter;
						}
						else
						{
							tile.SetTileSelectState(Tile.TILESELECTS.HIGHLIGHTED);
						}
					}
				}
			}
		}
		else
		{
			tile.SetTileSelectState(Tile.TILESELECTS.HIGHLIGHTED);
		}
	}

	internal void MouseLeaveTile(Tile tile)
	{
		if (tile.TileSelectState == Tile.TILESELECTS.HIGHLIGHTED)
			tile.SetTileSelectState(Tile.TILESELECTS.NORMAL);
		else if (tile.TileSelectState == Tile.TILESELECTS.SELECTED_AND_HIGHLIGHTED)
			tile.SetTileSelectState(Tile.TILESELECTS.SELECTED);

		if (_currentHighlightedTile == tile)
		{
			_currentHighlightedTile = null;
		}
	}


	internal void ClickTile(Tile tile)
	{

	}
}
