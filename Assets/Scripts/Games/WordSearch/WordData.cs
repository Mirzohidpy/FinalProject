using UnityEngine;

[CreateAssetMenu(fileName = "Word", menuName = "BrainCitizen/Word")]
public class WordData : ScriptableObject
{
    public string word;
    [TextArea] public string definition;
}
