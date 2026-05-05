using UnityEngine;

public enum MathOperation
{
    Add,
    Subtract,
    Multiply,
    MixedAddSub,
    MixedAll
}

[CreateAssetMenu(fileName = "MathLevel", menuName = "BrainCitizen/Math Level")]
public class MathLevelData : ScriptableObject
{
    public string levelName;
    public MathOperation operation;
    public int minOperand = 1;
    public int maxOperand = 10;
    public int questionsInLevel = 5;
    public float secondsPerQuestion = 7f;
    public bool suddenDeath = false;
}
