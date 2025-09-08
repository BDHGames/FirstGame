using System.Collections.Generic;
using odin.serialize.OdinSerializer;
using UnityEngine;

[CreateAssetMenu(fileName = "SerializedDict", menuName = "Scriptable Objects/SerializedDict")]
public class SerializedDict : SerializedScriptableObject
{
    public Dictionary<string, FPART> dict = new Dictionary<string, FPART>();
}
