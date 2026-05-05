using UnityEngine;

public enum EmotionDifficulty { Easy, Medium, Hard }

[CreateAssetMenu(fileName = "Emotion", menuName = "BrainCitizen/Emotion")]
public class EmotionData : ScriptableObject
{
    public string emotionName;
    public Sprite faceSprite;
    public EmotionDifficulty difficulty;
    [TextArea(2, 4)]
    public string mentalHealthTip;
}
