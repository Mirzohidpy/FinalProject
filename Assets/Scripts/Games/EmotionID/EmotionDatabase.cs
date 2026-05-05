using UnityEngine;

[CreateAssetMenu(fileName = "EmotionDatabase", menuName = "BrainCitizen/Emotion Database")]
public class EmotionDatabase : ScriptableObject
{
    public EmotionData[] emotions;
}
