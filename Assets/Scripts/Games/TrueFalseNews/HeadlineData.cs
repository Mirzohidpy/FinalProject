using UnityEngine;

/// <summary>
/// ScriptableObject representing a single True/False News headline.
/// Create assets via: Right-click in Project > Create > BrainCitizen > Headline
/// </summary>
[CreateAssetMenu(fileName = "New Headline", menuName = "BrainCitizen/Headline")]
public class HeadlineData : ScriptableObject
{
    [TextArea(2, 4)]
    public string headline;

    public bool isReal;

    [TextArea(2, 5)]
    public string explanation;

    public string sourceHint; // e.g. "Source: WHO, 2023" or "No credible source found"

    public HeadlineCategory category;
}

public enum HeadlineCategory
{
    Politics,
    Health,
    Science,
    WorldEvents
}
