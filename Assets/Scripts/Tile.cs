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

	private HIGHLIGHTS _highlightState = HIGHLIGHTS.NORMAL;
	public HIGHLIGHTS HighlightState { get => _highlightState; set => SetHighlightState(value); }

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

	private void SetHighlightState(HIGHLIGHTS tileSelectState)
	{
		_highlightState = tileSelectState;

		if (_sprite)
		{
			switch (_highlightState)
			{
				case HIGHLIGHTS.NORMAL:
					_sprite.color = _normalColor;
					return;
				case HIGHLIGHTS.HIGHLIGHTED:
					_sprite.color = _highlightedColor;
					return;
				case HIGHLIGHTS.SELECTED:
					_sprite.color = _selectedColor;
					return;
				case HIGHLIGHTS.SELECTED_AND_HIGHLIGHTED:
					_sprite.color = _selectedAndHighlightedColor;
					return;
			}
		}
	}
}