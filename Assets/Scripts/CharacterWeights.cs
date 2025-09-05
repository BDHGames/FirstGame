using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterWeights", menuName = "Scriptable Objects/CharacterWeights")]
public class CharacterWeights : ScriptableObject
{
	public float[] _weights;

	// do we want to fudge this a bit to ensure that we get vowels if it's been too long between them?

	public char RandomChar()
	{
		float sum = _weights.Sum();

		float rand = Random.Range(0.0f, 1.0f) * sum; // long-term we should have a centralized RNG so we can have consistent test cases.

		for (int charIter = 0; charIter < 26; charIter++)
		{
			if (rand < _weights[charIter])
				return (char)('A' + charIter);

			rand -= _weights[charIter];
		}

		return 'Z';
	}
}
