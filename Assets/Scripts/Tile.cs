using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Tile: MonoBehaviour
{
	public TextMeshPro _tmpro;
	public char _letter;
	// should not be editor-accessible, should be accessed by code
	internal Vector2Int _coord;

	[SerializeField]
	private SpriteRenderer _sprite;

	[SerializeField]
	private Color _normalColor, _highlightedColor, _selectedColor, _selectedAndHighlightedColor;

	public enum TILESELECTS
	{
		NORMAL,
		HIGHLIGHTED,
		SELECTED,
		SELECTED_AND_HIGHLIGHTED
	}

	private TILESELECTS _tileSelectState = TILESELECTS.NORMAL;
	public TILESELECTS TileSelectState => _tileSelectState;

	void Update()
	{
		if (_tmpro)
		{
			_tmpro.text = _letter.ToString();
		}
	}

	private void OnMouseEnter()
	{
		TileSelector.INSTANCE.MouseOverTile(this);
	}

	private void OnMouseExit()
	{
		TileSelector.INSTANCE.MouseLeaveTile(this);
	}

	private void OnMouseDown()
	{
		TileSelector.INSTANCE.ClickTile(this);
	}

	public void SetTileSelectState(TILESELECTS tileSelectState)
	{
		_tileSelectState = tileSelectState;

		if (_sprite)
		{
			switch (_tileSelectState)
			{
				case TILESELECTS.NORMAL:
					_sprite.color = _normalColor;
					return;
				case TILESELECTS.HIGHLIGHTED:
					_sprite.color = _highlightedColor;
					return;
				case TILESELECTS.SELECTED:
					_sprite.color = _selectedColor;
					return;
				case TILESELECTS.SELECTED_AND_HIGHLIGHTED:
					_sprite.color = _selectedAndHighlightedColor;
					return;
			}
		}
	}
}