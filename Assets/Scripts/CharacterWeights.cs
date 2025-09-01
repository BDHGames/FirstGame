using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterWeights", menuName = "Scriptable Objects/CharacterWeights")]
public class CharacterWeights : ScriptableObject
{
    public float[] _weights;

    // do we want to fudge this a bit to ensure that we get vowels if it's been too long between them?
    public char GetChar()
    {
        float sum = _weights.Sum();

        float uRand = Random.Range(0.0f, 1.0f); // long-term we should have a centralized RNG so we can have consistent test cases.

        float gRand = uRand * sum;

        for (int iChar = 0; iChar < 26; iChar++)
        {
            if (gRand < _weights[iChar])
                return (char)('A' + iChar);

            gRand -= _weights[iChar];
        }

        return 'Z';
    }
}
