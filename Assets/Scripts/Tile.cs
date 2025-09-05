using TMPro;
using UnityEngine;

public class Tile: MonoBehaviour
{
	public TextMeshPro _tmpro;
	public char _letter;

	void Update()
	{
		if (_tmpro)
		{
			_tmpro.text = _letter.ToString();
		}
	}
}