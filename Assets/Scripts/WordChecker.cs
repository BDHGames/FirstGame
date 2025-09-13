using System.Collections.Generic;
using odin.serialize.OdinSerializer;
using UnityEngine;
using UnityEngine.Windows;


public class WordChecker : MonoBehaviour
{
    //the dictionary
    public SerializedDict _allWords;

    private bool happened = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    /*
     * Noun = 0b1000000
     * Verb = 0b0100000
     */
    void Start()
    {
        _allWords = ScriptableObject.CreateInstance<SerializedDict>();
        byte[] dictbytes = File.ReadAllBytes(Application.dataPath + "/Data/odinDict");
        _allWords = SerializationUtility.DeserializeValue<SerializedDict>(dictbytes, DataFormat.Binary);
    }

    //checks if the word is in the dict, if yes returns true, if no return false
    private bool CheckWord(string word, out FPART pOS)
    {
        //returns true if the word is in the dictionary, and puts the parts of speech in pOS
        //otherwise returns false
        return _allWords.dict.TryGetValue(word, out pOS);
    }
    
    // Update is called once per frame
    void Update()
    {
        //runs once to test dict, after start, so every script inits first. No lateStart sadly
        if (!happened)
        {
            happened = true;
            Debug.Log("check: test\n");
            FPART pOS;
            
            //each of these calls CheckWord to see if its in the dictionary
            if (CheckWord("test", out pOS))
            {
                //if in the dictionary, print the parts of speech
                Debug.Log("true: " + pOS.ToString());
            }
            else
            {
                //otherwise print "Not a Word"
                Debug.Log("Not a Word");
            }
            
            Debug.Log("check: run\n");
            if (CheckWord("run", out pOS))
            {
                Debug.Log("true: " + pOS.ToString());
            }
            else
            {
                Debug.Log("Not a Word");
            }
            
            Debug.Log("check: defenestrate\n");
            if (CheckWord("defenestrate", out pOS))
            {
                Debug.Log("true: " + pOS.ToString());
            }
            else
            {
                Debug.Log("Not a Word");
            }
            
            Debug.Log("check: hell\n");
            if (CheckWord("hell", out pOS))
            {
                Debug.Log("true: " + pOS.ToString());
            }
            else
            {
                Debug.Log("Not a Word");
            }

            happened = true;
        }
    }
}
