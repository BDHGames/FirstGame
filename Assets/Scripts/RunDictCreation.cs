using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using odin.serialize.OdinSerializer;
using UnityEngine;
using Newtonsoft.Json;
using File = UnityEngine.Windows.File;

public class RunDictCreation : MonoBehaviour
{
    private bool hasRun = false;
    public SerializedDict allWords;
    private string fileContents;
    public string jsonPath;
    private Dictionary<String, List<String>> dictContents; 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        allWords = SerializedDict.CreateInstance<SerializedDict>();
        allWords.dict = new Dictionary<string, FPART>();
        dictContents = new Dictionary<string, List<string>>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!hasRun)
        {
            hasRun = true;
            //byte[] dictbytes = File.ReadAllBytes(jsonPath);
            StreamReader reader = new StreamReader(jsonPath);
            string dictString = reader.ReadToEnd();
            dictContents = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(dictString);
            //dictContents = odin.serialize.OdinSerializer.SerializationUtility.DeserializeValue<Dictionary<string, List<string>>>(dictbytes, DataFormat.JSON);
            foreach (KeyValuePair<string, List<string>> pair in dictContents)
            {
                string word = string.Copy(pair.Key);
                FPART POS = FPART.NONE;
                List<string> parts = pair.Value;
                foreach (string part in parts)
                {
                    switch (part)
                    {
                        case "noun":
                            POS |= FPART.NOUN;
                            break;
                        case "verb":
                            POS |= FPART.VERB;
                            break;
                        case "adj":
                            POS |= FPART.ADJECTIVE;
                            break;
                        case "adv":
                            POS |= FPART.ADVERB;
                            break;
                        case "prep":
                            POS |= FPART.PREPOSITION;
                            break;
                        case "pron":
                            POS |= FPART.PRONOUN;
                            break;
                        case "conj":
                            POS |= FPART.CONJUNCTION;
                            break;
                        default:
                            break;
                    }
                }
                allWords.dict.Add(word, POS);
            }

            byte[] outBytes = odin.serialize.OdinSerializer.SerializationUtility.SerializeValue(allWords, DataFormat.Binary);
            File.WriteAllBytes("/run/media/system/F/unityProjects/FirstGame/Assets/Data/odinDict", outBytes);
        }
    }
}
