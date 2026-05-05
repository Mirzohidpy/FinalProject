using UnityEngine;

[CreateAssetMenu(fileName = "WordDatabase", menuName = "BrainCitizen/Word Database")]
public class WordDatabase : ScriptableObject
{
    public WordData[] words;
}
