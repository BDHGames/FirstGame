using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using odin.serialize.OdinSerializer;
using UnityEngine;
using Newtonsoft.Json;
using File = UnityEngine.Windows.File;
using fs = System.IO;
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
            List<string> reviewWords = new List<string>();
            Debug.Log("read");
            foreach (KeyValuePair<string, List<string>> pair in dictContents)
            {
                string word = string.Copy(pair.Key);
                FPART POS = FPART.NONE;
                List<string> parts = pair.Value;
                bool onlyBad = true;
                bool export = false;
                foreach (string part in parts)
                {
                    switch (part)
                    {
                        case "noun":
                            POS |= FPART.NOUN;
                            onlyBad = false;
                            break;
                        case "verb":
                            POS |= FPART.VERB;
                            onlyBad = false;
                            break;
                        case "adj":
                            POS |= FPART.ADJECTIVE;
                            onlyBad = false;
                            break;
                        case "adv":
                            POS |= FPART.ADVERB;
                            onlyBad = false;
                            break;
                        case "prep":
                            POS |= FPART.PREPOSITION;
                            onlyBad = false;
                            break;
                        case "pron":
                            POS |= FPART.PRONOUN;
                            onlyBad = false;
                            break;
                        case "conj":
                            POS |= FPART.CONJUNCTION;
                            onlyBad = false;
                            break;
                        case "proverb":
                        case "prep_phrase":
                        case "phrase":
                            break;
                        case "det": 
                        case "infix":
                        case "postp":
                        case "contraction":
                        case "prefix":
                        case "suffix":
                        case "punct":
                        case "character":
                        case "num":
                        case "symbol":    
                            export = true;
                            break;
                        default:
                            onlyBad = false;
                            break;
                    }
                }

                Debug.Log("parsed");
                if (!onlyBad)
                {
                    allWords.dict.Add(word, POS);
                }

                if (export)
                {
                    reviewWords.Add(word);
                }
            }

            byte[] outBytes = odin.serialize.OdinSerializer.SerializationUtility.SerializeValue(allWords, DataFormat.Binary);
            File.WriteAllBytes("/run/media/system/F/unityProjects/FirstGame/Assets/Data/odinDict", outBytes);

            StreamWriter reviewFile = fs.File.CreateText("/run/media/system/F/unityProjects/FirstGame/Assets/Data/review.txt");

            foreach (string reviewWord in reviewWords)
            {
                reviewFile.WriteLine(reviewWord);
            }
            Debug.Log("done");
        }
    }
}
