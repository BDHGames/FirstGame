using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CharacterWeights))]
public class CharacterWeightsEditor : Editor
{
    public static bool __fFormat;

    public override void OnInspectorGUI()
    {
        var charweights = target as CharacterWeights;

        if (!charweights)
            return;

        if (charweights._weights.Length != 26)
        {
            charweights._weights = new float[26];
        }

        float gSum = charweights._weights.Sum();

        EditorGUILayout.LabelField("These are floats, not integers, so you can fine-tune these.");
        EditorGUILayout.LabelField("A higher number means that letter is more likely to appear.");
        EditorGUILayout.Separator();

        for (int i = 0; i < 26; i++)
        {
            char charLabel = (char)('A' + i);

            string strLabel = charLabel.ToString();

            if (gSum > 0)
            {
                if (__fFormat)
                {
                    float uChance = charweights._weights[i] / gSum;
                    float percentChance = uChance * 100;
                    strLabel += " (" + percentChance.ToString("0.00") + "% Chance)";
                }
                else
                {
                    strLabel += " (~1/" + Mathf.RoundToInt(gSum / charweights._weights[i]) + " Chance)";
                }
               
            }

            charweights._weights[i] = Mathf.Max(0.0f, EditorGUILayout.FloatField(strLabel, charweights._weights[i]));
        }

        EditorGUILayout.Separator();

        if (GUILayout.Button("Switch Chance Format"))
        {
            __fFormat = !__fFormat;
        }

        EditorGUILayout.Separator();

        GUI.enabled = false;
        EditorGUILayout.FloatField("Total Weight", gSum);
        GUI.enabled = true;
    }
}
