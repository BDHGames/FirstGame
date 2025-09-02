using System.Collections.Generic;
using odin.serialize.OdinSerializer;
using UnityEngine;

[CreateAssetMenu(fileName = "SerializedDict", menuName = "Scriptable Objects/SerializedDict")]
public class SerializedDict : SerializedScriptableObject
{
    public Dictionary<string, byte> dict = new Dictionary<string, byte>();
}
