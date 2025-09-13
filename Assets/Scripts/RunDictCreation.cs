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
        //reoplacement for the lack of lateStart()
        if (!hasRun)
        {
            hasRun = true;
            //byte[] dictbytes = File.ReadAllBytes(jsonPath);
            
            //reads in and deserializes the previous dictionary made in the utility into a Dictionary of the word, and a list of its parts of speech
            StreamReader reader = new StreamReader(jsonPath);
            string dictString = reader.ReadToEnd();
            dictContents = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(dictString);
            //dictContents = odin.serialize.OdinSerializer.SerializationUtility.DeserializeValue<Dictionary<string, List<string>>>(dictbytes, DataFormat.JSON);
            
            //a list for holding words for manual review
            List<string> reviewWords = new List<string>();
            Debug.Log("read");
            //loops through every entry in dictContents
            foreach (KeyValuePair<string, List<string>> pair in dictContents)
            {
                //holds the word itself
                string word = string.Copy(pair.Key);
                
                //the parts of speech we track as a flag value
                FPART POS = FPART.NONE;
                
                //holds the list of parts of speech
                List<string> parts = pair.Value;
                
                //holds whether we discard or export the word for review
                bool onlyBad = true;
                bool export = false;
                
                //loops through every part of speech attached to the word
                foreach (string part in parts)
                {
                    switch (part)
                    {
                        //each of these, if the part of speech is one we track, adds it to the flags, and says its not to be discarded
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
                        
                        //these ones are discarded, so we leave onlyBad as its current value
                        case "proverb":
                        case "prep_phrase":
                        case "phrase":
                            break;
                        //currently these words are excluded pending manual review
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
                        //if its a different but not excluded part of speech, it gets added with a blank flag
                        default:
                            onlyBad = false;
                            break;
                    }
                }

                Debug.Log("parsed");
                //adds words that have at least one included part of speech to the dictionary to be outputted
                if (!onlyBad)
                {
                    allWords.dict.Add(word, POS);
                }
                //adds words that have a part of speech in need of review to reviewWords for output to the review txt
                if (export)
                {
                    reviewWords.Add(word);
                }
            }
            
            //serializes the dictionary of words to a binary format and writes it out
            byte[] outBytes = odin.serialize.OdinSerializer.SerializationUtility.SerializeValue(allWords, DataFormat.Binary);
            File.WriteAllBytes("/run/media/system/F/unityProjects/FirstGame/Assets/Data/odinDict", outBytes);

            //creates the text file for manual review
            StreamWriter reviewFile = fs.File.CreateText("/run/media/system/F/unityProjects/FirstGame/Assets/Data/review.txt");
            //and writes out each word logged
            foreach (string reviewWord in reviewWords)
            {
                reviewFile.WriteLine(reviewWord);
            }
            Debug.Log("done");
        }
    }
}
