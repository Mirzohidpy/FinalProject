using UnityEngine;

public enum FlagDifficulty
{
    Easy,
    Medium,
    Hard
}

/// <summary>
/// One flag question: a country name, its flag sprite, and difficulty tier.
/// Created via Right-click > Create > BrainCitizen > Flag.
/// </summary>
[CreateAssetMenu(fileName = "Flag", menuName = "BrainCitizen/Flag")]
public class FlagData : ScriptableObject
{
    public string countryName;
    public Sprite flagSprite;
    public FlagDifficulty difficulty;
}
