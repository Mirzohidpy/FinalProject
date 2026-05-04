using UnityEngine;

public enum GameCategory
{
    CivicAwareness,
    MentalSkills
}

/// <summary>
/// Metadata for one mini-game in the Brain Citizen Academy hub.
/// One asset per game lives in Assets/Data/Hub/.
/// </summary>
[CreateAssetMenu(fileName = "GameInfo", menuName = "BrainCitizen/Game Info")]
public class GameInfo : ScriptableObject
{
    public int gameNumber;
    public string displayName;
    public string sceneName;

    [TextArea(2, 3)]
    public string tagline;

    public GameCategory category;
    public bool isImplemented;
}
