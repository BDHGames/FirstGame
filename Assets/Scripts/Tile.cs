using TMPro;
using UnityEngine;

class Tile: MonoBehaviour
{
    public TextMeshPro _tmpro;
    public char _charLetter;

    void Update()
    {
        if (_tmpro)
        {
            _tmpro.text = _charLetter.ToString();
        }
    }
}