using UnityEngine;

/// <summary>
/// The full pool of flags shown in Flag Quiz.
/// </summary>
[CreateAssetMenu(fileName = "FlagDatabase", menuName = "BrainCitizen/Flag Database")]
public class FlagDatabase : ScriptableObject
{
    public FlagData[] flags;
}
