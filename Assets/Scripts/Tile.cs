using TMPro;
using UnityEngine;

public class Tile: MonoBehaviour
{
	public TextMeshPro _tmpro;
	public char _letter;
	// should not be editor-accessible, should be accessed by code
	internal Vector2Int _coord;

	void Update()
	{
		if (_tmpro)
		{
			_tmpro.text = _letter.ToString();
		}
	}
}